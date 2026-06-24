/**
 * calculated-field.js (vanilla JS) 
 */

(function () {
    'use strict';

    // Config
    const SAVE_HANDLER = '?handler=SaveCalculatedField'; // querystring handler base (we'll append questionId & surveyId)
    const MODAL_ID = 'calculatedFieldModal';

    // Simple logging helpers
    function log(...args) { console.debug('calculated-field:', ...args); }
    function warn(...args) { console.warn('calculated-field:', ...args); }
    function error(...args) { console.error('calculated-field:', ...args); }

    // Sanity checks
    if (typeof bootstrap === 'undefined' || typeof bootstrap.Modal === 'undefined') {
        error('bootstrap.Modal not found. Ensure bootstrap.bundle.js is loaded before this script.');
        return;
    }

    const modalEl = document.getElementById(MODAL_ID);
    if (!modalEl) {
        error(`#${MODAL_ID} not found. Include the _CalculatedFieldModal partial in the page.`);
        return;
    }

    // Modal instance (reuse)
    let modalInstance;
    try { modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl); }
    catch (e) { modalInstance = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false }); }

    // DOM helpers
    function qs(selector, root = document) { return root.querySelector(selector); }
    function qsa(selector, root = document) { return Array.from(root.querySelectorAll(selector)); }
    function create(tag, attrs = {}, children = []) {
        const el = document.createElement(tag);
        for (const k in attrs) {
            if (k === 'class') el.className = attrs[k];
            else if (k === 'text') el.textContent = attrs[k];
            else el.setAttribute(k, attrs[k]);
        }
        children.forEach(c => {
            if (typeof c === 'string') el.appendChild(document.createTextNode(c));
            else el.appendChild(c);
        });
        return el;
    }

    // Read antiforgery token from hidden form (#antiforgeryForm) if available
    function getAntiForgeryToken() {
        const el = document.querySelector('#antiforgeryForm input[name="_bravePortalCsrf"]');
        return el ? el.value : null;
    }

    // Normalize numeric questions from window.__numericQuestions
    function getNumericQuestions() {
        const raw = Array.isArray(window.__numericQuestions) ? window.__numericQuestions : [];
        return raw.map(q => ({
            questionId: q.questionId ?? q.id ?? q.QuestionId ?? null,
            label: q.label ?? q.Label ?? q.text ?? q.QuestionText ?? String(q.questionId ?? q.id ?? ''),
            value: (q.value !== undefined ? q.value : (q.Value !== undefined ? q.Value : null))
        })).filter(x => x.questionId != null);
    }

    // Create an operand row element. operand = { type: 'constant'|'question', value, questionId }
    function makeOperandRow(index, operand = { type: 'constant', value: null }) {
        const row = create('div', { class: 'd-flex operand-row mb-2', 'data-operand-index': String(index) });

        // type select
        const typeSelect = create('select', { class: 'form-select form-select-sm operand-type', style: 'width:140px;flex:0 0 140px;' });
        typeSelect.innerHTML = '<option value="constant">Constant</option><option value="question">Question Value</option>';
        row.appendChild(typeSelect);

        // constant input
        const constantInput = create('input', { class: 'form-control form-control-sm operand-constant', type: 'number', placeholder: 'Constant', style: 'flex:1;' });
        row.appendChild(constantInput);

        // question select
        const qSelect = create('select', { class: 'form-select form-select-sm operand-question', style: 'flex:1; display:none;' });
        qSelect.innerHTML = '<option value="">-- select question --</option>';
        row.appendChild(qSelect);

        // preview button
        const previewBtn = create('button', { class: 'btn btn-sm btn-outline-secondary btn-preview', type: 'button', title: 'Preview referenced question value' });
        previewBtn.innerHTML = '<i class="fa fa-eye"></i>';
        row.appendChild(previewBtn);

        // populate questions
        const numericQs = getNumericQuestions();
        numericQs.forEach(q => {
            const opt = create('option', { value: String(q.questionId) });
            opt.textContent = q.label || String(q.questionId);
            if (q.value !== undefined && q.value !== null) opt.dataset.value = String(q.value);
            qSelect.appendChild(opt);
        });

        // initialize
        typeSelect.value = operand.type || 'constant';
        if ((operand.type || 'constant') === 'question') {
            constantInput.style.display = 'none';
            qSelect.style.display = '';
            if (operand.questionId) qSelect.value = String(operand.questionId);
        } else {
            qSelect.style.display = 'none';
            constantInput.style.display = '';
            if (operand.value != null) constantInput.value = operand.value;
        }

        return row;
    }

    // Read operand rows from DOM -> [{type, value, questionId}, ...]
    function readOperandsFromDom() {
        const ops = [];
        const rows = qsa('#calc-operands .operand-row', modalEl);
        rows.forEach(r => {
            const type = r.querySelector('.operand-type').value;
            if (type === 'question') {
                const qid = r.querySelector('.operand-question').value || null;
                ops.push({ type: 'question', questionId: qid, value: null });
            } else {
                const raw = r.querySelector('.operand-constant').value;
                ops.push({ type: 'constant', value: raw === '' ? null : Number(raw), questionId: null });
            }
        });
        return ops;
    }

    // Resolve operand numeric values from constants or window.__numericQuestions values
    function resolveOperandValues(ops) {
        const numericQs = getNumericQuestions();
        return ops.map(op => {
            if (op.type === 'question') {
                const q = numericQs.find(x => String(x.questionId) === String(op.questionId));
                return q ? (q.value === undefined ? null : Number(q.value)) : null;
            }
            return op.value === null || op.value === undefined ? null : Number(op.value);
        });
    }

    // Compute result according to operation
    function computeResult(operation, resolvedValues) {
        if (!Array.isArray(resolvedValues) || resolvedValues.length === 0) return null;
        if (resolvedValues.some(v => v === null || v === undefined || isNaN(Number(v)))) return null;

        let acc = Number(resolvedValues[0]);
        for (let i = 1; i < resolvedValues.length; i++) {
            const b = Number(resolvedValues[i]);
            switch (operation) {
                case 'sum': acc = acc + b; break;
                case 'subtract': acc = acc - b; break;
                case 'multiply': acc = acc * b; break;
                case 'divide':
                    if (b === 0) return 'DIV_BY_ZERO';
                    acc = acc / b; break;
                default: return null;
            }
        }
        return acc;
    }

    // Update preview UI and build JSON preview (payload does NOT include questionId or override)
    function updatePreviewAndJson() {
        const operationEl = qs('#calc-operation', modalEl);
        const operation = operationEl ? operationEl.value : 'sum';
        const ops = readOperandsFromDom();
        const resolved = resolveOperandValues(ops);
        const result = computeResult(operation, resolved);

        const resultPreviewEl = qs('#calc-result-preview', modalEl);
        const errorEl = qs('#calc-error', modalEl);
        const jsonEl = qs('#json-preview', modalEl);

        if (result === 'DIV_BY_ZERO') {
            if (errorEl) { errorEl.style.display = ''; errorEl.textContent = 'Cannot divide by zero.'; }
            if (resultPreviewEl) resultPreviewEl.value = '';
        } else if (result === null) {
            if (errorEl) { errorEl.style.display = 'none'; errorEl.textContent = ''; }
            if (resultPreviewEl) resultPreviewEl.value = '';
        } else {
            if (errorEl) { errorEl.style.display = 'none'; errorEl.textContent = ''; }
            if (resultPreviewEl) resultPreviewEl.value = Number.isInteger(result) ? result : result;
        }

        // Build payload WITHOUT questionId and WITHOUT override
        const payload = {
            operation: operation,
            operands: ops.map(op => op.type === 'question' ? { type: 'question', questionId: op.questionId } : { type: 'constant', value: op.value }),
            resultPreview: (result === 'DIV_BY_ZERO' ? null : (result === null ? null : result))
        };

        if (jsonEl) jsonEl.textContent = JSON.stringify(payload, null, 2);
    }

    /* -------------------- EVENT HANDLERS -------------------- */

    // ---------- Robust "open modal" handler with prepopulate from saved resultExpression ----------
    document.addEventListener('click', function (ev) {
        const btn = ev.target.closest && ev.target.closest('.open-calculated-btn');
        if (!btn) return;
        ev.preventDefault();

        try {
            // Try many possible attribute/dataset names
            let qId =
                btn.getAttribute('data-question-id') ||
                btn.getAttribute('data-source-id') ||
                btn.getAttribute('data-qid') ||
                btn.getAttribute('data-questionid') ||
                btn.dataset?.questionId ||
                btn.dataset?.sourceId ||
                btn.dataset?.qid ||
                btn.dataset?.questionid ||
                '';

            // If still empty, try the card-level data attributes
            if (!qId) {
                const card = btn.closest('.qb-card');
                if (card) {
                    qId =
                        card.getAttribute('data-source-id') ||
                        card.getAttribute('data-qid') ||
                        card.getAttribute('data-id') ||
                        card.dataset?.sourceId ||
                        card.dataset?.qid ||
                        card.dataset?.id ||
                        '';
                }
            }

            // question label fallback
            const qLabel =
                btn.getAttribute('data-question-label') ||
                btn.getAttribute('data-label') ||
                btn.dataset?.questionLabel ||
                btn.dataset?.label ||
                (btn.textContent || '').trim() ||
                '';

            // ensure hidden input exists and set it
            let qidEl = qs('#calc-question-id', modalEl);
            if (!qidEl) {
                qidEl = document.createElement('input');
                qidEl.type = 'hidden';
                qidEl.id = 'calc-question-id';
                modalEl.querySelector('.modal-body')?.appendChild(qidEl);
            }
            qidEl.value = qId || '';

            // show label for UX
            const qlabelEl = qs('#calc-question-label', modalEl);
            if (qlabelEl) qlabelEl.textContent = qLabel || (qId ? `#${qId}` : 'Calculated Field');

            // reset error & preview UI
            const errEl = qs('#calc-error', modalEl);
            if (errEl) { errEl.style.display = 'none'; errEl.textContent = ''; }
            const resultEl = qs('#calc-result-preview', modalEl);
            if (resultEl) resultEl.value = '';
            const jsonEl = qs('#json-preview', modalEl);
            if (jsonEl) jsonEl.textContent = '';

            // try to find the numeric question in window.__numericQuestions to read saved resultExpression
            const numericQs = Array.isArray(window.__numericQuestions) ? window.__numericQuestions : [];
            const matched = numericQs.find(x => String(x.questionId) === String(qId));

            // default operation
            const opEl = qs('#calc-operation', modalEl);
            if (opEl) opEl.value = 'sum';

            // function to populate operands from parsed payload
            function populateOperandsFromPayload(payload) {
                const opContainer = qs('#calc-operands', modalEl);
                if (!opContainer) return;
                opContainer.innerHTML = ''; // clear

                // payload.operands expected: [{type:'constant', value:...} | {type:'question', questionId:...}, ...]
                const ops = Array.isArray(payload && payload.operands) ? payload.operands : [];

                if (ops.length === 0) {
                    // fallback: two empty constants
                    opContainer.appendChild(makeOperandRow(1, { type: 'constant', value: null }));
                    opContainer.appendChild(makeOperandRow(2, { type: 'constant', value: null }));
                    return;
                }

                ops.forEach((op, idx) => {
                    const row = makeOperandRow(idx + 1, { type: op.type === 'question' ? 'question' : 'constant', value: op.value ?? null, questionId: op.questionId ?? null });
                    // After creating row, we must ensure the question select contains the numeric question list (makeOperandRow populates from current getNumericQuestions)
                    opContainer.appendChild(row);
                    // If operand is question, set selected value
                    if (op.type === 'question' && op.questionId) {
                        const sel = row.querySelector('.operand-question');
                        if (sel) sel.value = String(op.questionId);
                    }
                    // If operand is constant, set constant input value
                    if (op.type !== 'question' && op.value !== undefined && op.value !== null) {
                        const cin = row.querySelector('.operand-constant');
                        if (cin) cin.value = op.value;
                    }
                });
            }

            // If matched question has a saved resultExpression, try to parse and prepopulate
            let parsed = null;
            if (matched && matched.resultExpression) {
                try {
                    // resultExpression may already be a JSON string or object
                    if (typeof matched.resultExpression === 'string') {
                        parsed = JSON.parse(matched.resultExpression);
                    } else {
                        parsed = matched.resultExpression;
                    }
                } catch (err) {
                    // parse failed — ignore and allow default UI
                    parsed = null;
                    if (errEl) {
                        errEl.style.display = '';
                        errEl.textContent = 'Saved calculated-field JSON is malformed and could not be loaded.';
                    }
                    console.warn('calculated-field: failed to parse saved resultExpression for question', qId, err);
                }
            }

            // Populate UI: operation, operands, result preview and json preview
            if (parsed && typeof parsed === 'object') {
                if (opEl && parsed.operation) opEl.value = parsed.operation;
                populateOperandsFromPayload(parsed);
                // compute preview using current logic (updatePreviewAndJson will compute based on DOM inputs)
                setTimeout(() => {
                    updatePreviewAndJson();
                    // override preview value with saved resultPreview if present
                    if (parsed.resultPreview !== undefined && parsed.resultPreview !== null) {
                        const rp = qs('#calc-result-preview', modalEl);
                        if (rp) rp.value = parsed.resultPreview;
                    }
                    // show the saved JSON in preview (canonicalize)
                    const jsonEl2 = qs('#json-preview', modalEl);
                    if (jsonEl2) {
                        try { jsonEl2.textContent = JSON.stringify(parsed, null, 2); } catch (e) { jsonEl2.textContent = String(parsed); }
                    }
                }, 60);
            } else {
                // no parsed saved payload -> default two empty constants
                const opContainer = qs('#calc-operands', modalEl);
                if (opContainer) {
                    opContainer.innerHTML = '';
                    opContainer.appendChild(makeOperandRow(1, { type: 'constant', value: null }));
                    opContainer.appendChild(makeOperandRow(2, { type: 'constant', value: null }));
                }
                // compute initial preview
                setTimeout(updatePreviewAndJson, 50);
            }

            // Show modal
            modalInstance.show();

            // friendly warning if we couldn't find a question id
            if (!qId) {
                if (errEl) {
                    errEl.style.display = '';
                    errEl.textContent = 'Warning: question id not found on button — saved JSON cannot be mapped to a question until you open this modal from a persisted question.';
                }
            }

            log('opened calculated-field modal for questionId:', qId, 'prepopulated:', !!parsed);
        } catch (err) {
            error('open-modal error', err);
            alert('Failed to open calculated-field modal (see console).');
        }
    });



    // Modal input listener: toggles operand type visibility and updates preview
    modalEl.addEventListener('input', function (ev) {
        const typeEl = ev.target.closest && ev.target.closest('.operand-type');
        if (typeEl) {
            const row = typeEl.closest('.operand-row');
            if (!row) return;
            const t = typeEl.value;
            const constInput = row.querySelector('.operand-constant');
            const qSelect = row.querySelector('.operand-question');
            if (t === 'question') {
                if (constInput) constInput.style.display = 'none';
                if (qSelect) qSelect.style.display = '';
            } else {
                if (constInput) constInput.style.display = '';
                if (qSelect) qSelect.style.display = 'none';
            }
            updatePreviewAndJson();
            return;
        }

        if (ev.target.matches('.operand-constant, .operand-question, #calc-operation')) {
            updatePreviewAndJson();
        }
    });

    // Add / remove operand and preview button (delegated)
    document.addEventListener('click', function (ev) {
        const addBtn = ev.target.closest && ev.target.closest('#add-operand-btn');
        if (addBtn) {
            ev.preventDefault();
            const container = qs('#calc-operands', modalEl);
            const next = (container ? container.querySelectorAll('.operand-row').length + 1 : 1);
            if (container) container.appendChild(makeOperandRow(next, { type: 'constant', value: null }));
            updatePreviewAndJson();
            return;
        }

        const remBtn = ev.target.closest && ev.target.closest('#remove-operand-btn');
        if (remBtn) {
            ev.preventDefault();
            const container = qs('#calc-operands', modalEl);
            const rows = container ? container.querySelectorAll('.operand-row') : [];
            if (rows.length <= 1) return;
            rows[rows.length - 1].remove();
            updatePreviewAndJson();
            return;
        }

        const pbtn = ev.target.closest && ev.target.closest('.btn-preview');
        if (pbtn && modalEl.contains(pbtn)) {
            ev.preventDefault();
            const row = pbtn.closest('.operand-row');
            if (!row) return;
            const qid = row.querySelector('.operand-question').value;
            if (!qid) {
                alert('Select a question first to preview its value.');
                return;
            }
            const nq = getNumericQuestions().find(x => String(x.questionId) === String(qid));
            alert(`Value for ${nq ? nq.label : qid}: ${nq && (nq.value !== undefined) ? nq.value : 'n/a'}`);
            return;
        }
    });

    // Save handler: POST payload WITHOUT questionId and WITHOUT override,
    // but send questionId & surveyId in query string so server can update correct question.
    document.addEventListener('click', function (ev) {
        const saveBtn = ev.target.closest && ev.target.closest('#calc-save-btn');
        if (!saveBtn) return;
        ev.preventDefault();

        try {
            const jsonTextEl = qs('#json-preview', modalEl);
            const jsonText = jsonTextEl ? jsonTextEl.textContent : null;
            if (!jsonText) {
                const errEl = qs('#calc-error', modalEl);
                if (errEl) { errEl.style.display = ''; errEl.textContent = 'Nothing to save.'; }
                return;
            }

            const payload = JSON.parse(jsonText);
            if (payload.resultPreview === null || payload.resultPreview === undefined) {
                const errEl = qs('#calc-error', modalEl);
                if (errEl) { errEl.style.display = ''; errEl.textContent = 'No valid result to save.'; }
                return;
            }

            // read question id from hidden input (set on modal open)
            const qidEl = qs('#calc-question-id', modalEl);
            const questionId = qidEl ? (qidEl.value || '') : '';
            if (!questionId) {
                const errEl = qs('#calc-error', modalEl);
                if (errEl) { errEl.style.display = ''; errEl.textContent = 'Missing question id.'; }
                return;
            }

            // ensure surveyId is available in page scope (your page defines const surveyId = @Model.SurveyId;)
            if (typeof surveyId === 'undefined') {
                warn('surveyId is not defined on page. Include const surveyId = @Model.SurveyId; in page script.');
            }

            const url = `${SAVE_HANDLER}&questionId=${encodeURIComponent(questionId)}&surveyId=${encodeURIComponent(typeof surveyId !== 'undefined' ? surveyId : '')}`;
            const headers = { 'Content-Type': 'application/json' };
            const token = getAntiForgeryToken();
            if (token) headers['X-BRaVe-Portal-CSRF'] = token;

            fetch(url, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(payload)
            }).then(async res => {
                let json = null;
                try { json = await res.json(); } catch (e) { /* ignore JSON parse */ }

                if (res.ok && json && json.success) {
                    // follow your existing pattern: if server asks to reload, do so to show TempData
                    if (json.reload === true) {
                        window.location.reload();
                        return;
                    }
                    // otherwise just close modal
                    modalInstance.hide();
                } else {
                    const msg = (json && json.message) || `Save failed: ${res.status} ${res.statusText}`;
                    const errEl = qs('#calc-error', modalEl);
                    if (errEl) { errEl.style.display = ''; errEl.textContent = msg; }
                }
            }).catch(err => {
                error('save fetch error', err);
                const errEl = qs('#calc-error', modalEl);
                if (errEl) { errEl.style.display = ''; errEl.textContent = 'An error occurred saving the calculated field.'; }
            });

        } catch (err) {
            error('save handler error', err);
            const errEl = qs('#calc-error', modalEl);
            if (errEl) { errEl.style.display = ''; errEl.textContent = 'An unexpected error occurred.'; }
        }
    });

    // Expose tiny debug API
    window.__calculatedField = window.__calculatedField || {};
    window.__calculatedField.updatePreviewAndJson = updatePreviewAndJson;
    window.__calculatedField.getNumericQuestions = getNumericQuestions;

    log('calculated-field.js loaded — modal id:', MODAL_ID, 'save handler base:', SAVE_HANDLER);
})();
