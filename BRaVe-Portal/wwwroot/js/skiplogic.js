/**
 * skiplogic.js (patched)
 *
 * Expression builder used by the QuestionBuilder page.
 *
 */

document.addEventListener("DOMContentLoaded", () => {
    // try to resolve elements either in page or inside #sharedModal
    const modalRoot = document.getElementById("sharedModal") || null;
    const resolve = (ids) => {
        for (const id of ids) {
            const el =
                document.getElementById(id) ||
                (modalRoot && modalRoot.querySelector(`#${id}`));
            if (el) return el;
        }
        return null;
    };

    const questionsListEl = resolve(["modal-questions-list", "questions-list"]);
    const questionsCountEl = resolve(["modal-question-count", "question-count"]);
    const searchInput = resolve(["modal-search-questions", "search-questions"]);
    const builderRoot = resolve(["modal-builder-root", "builder-root"]);
    const jsonOutput = resolve(["modal-json-output", "json-output"]);
    const operatorsContainer = resolve(["modal-operators-list", "operators-list"]);
    const modalSaveBtn = resolve(["modal-save", "btn-save"]);
    const modalClearBtn = resolve(["modal-clear", "btn-clear"]);
    const modalConfirmApplyBtn = resolve(["modal-confirm-save"]);
    const modalTitleEl = resolve(["sharedModalLabel"]);

    // bootstrap modal (best-effort)
    let bsModal = null;
    if (modalRoot && modalRoot.classList && modalRoot.classList.contains("modal") && window.bootstrap) {
        try { bsModal = new bootstrap.Modal(modalRoot); } catch (e) { }
    }

    // small helpers
    const generateId = () => Math.random().toString(36).slice(2, 10);
    const escapeHtml = (s) =>
        s == null
            ? ""
            : String(s).replace(/[&<>\"'`=\/]/g, (ch) => {
                return {
                    "&": "&amp;",
                    "<": "&lt;",
                    ">": "&gt;",
                    '"': "&quot;",
                    "'": "&#39;",
                    "/": "&#47;",
                    "`": "&#96;",
                    "=": "&#61;"
                }[ch];
            });


    // --- Answer type lookup normalization (new) ---
    // window.BRAVE_ANSWER_TYPES is expected to be an array of LookupItemDto from server
    const ANSWER_TYPE_LOOKUPS = (Array.isArray(window.BRAVE_ANSWER_TYPES) ? window.BRAVE_ANSWER_TYPES : []).map(l => ({
        id: l?.Id ?? null,
        code: (l?.Code !== undefined && l?.Code !== null) ? String(l.Code) : String(l?.Id ?? ''),
        text: l?.Text ?? l?.text ?? String(l?.Id ?? '')
    }));

    function getAnswerTypeName(rawType) {
        if (rawType == null) return '';
        const rt = String(rawType);
        const found = ANSWER_TYPE_LOOKUPS.find(x => String(x.id) === rt || String(x.code) === rt);
        return found ? found.text : rt;
    }
    // --- end answer type lookup ---

    // currently-open target question (when adding rules for a specific question)
    let CURRENT_TARGET_QUESTION_ID = null;

    // QUESTIONS: runtime array populated from API or server-rendered payload
    let QUESTIONS = [];
    let LEFT_PANEL_QUESTIONS = [];
    let ACTIVE_RULE_ID = null;



    // root expression tree (every node must have an id)
    let state = {
        root: {
            /*id: generateId(),*/
            type: "group",
            op: "AND",
            expressions: []
        }
    };

    function ensureNodeIds(node) {
        if (!node) return node;
       if (!node.id) node.id = generateId();
        if (node.type === "group") {
            if (!Array.isArray(node.expressions)) node.expressions = [];
            node.expressions.forEach((ch) => ensureNodeIds(ch));
        }
        return node;
    }

    // discover SurveyId from multiple places so no Razor change required
    function discoverSurveyId() {
        if (typeof window.BRAVE_SURVEY_ID !== "undefined" && window.BRAVE_SURVEY_ID) return window.BRAVE_SURVEY_ID;
        try {
            if (modalRoot && modalRoot.dataset && modalRoot.dataset.surveyId) return modalRoot.dataset.surveyId;
        } catch (e) { }
        try {
            const params = new URLSearchParams(window.location.search);
            if (params.has("surveyId")) return params.get("surveyId");
            if (params.has("SurveyId")) return params.get("SurveyId");
            const m = window.location.pathname.match(/QuestionBuilder\/(\d+)/i);
            if (m && m[1]) return m[1];
        } catch (e) { }
        return null;
    }

    // Helper to build canonical mapping for a question item:
    function mapQuestionItem(q) {
        const id = q.SourceId ?? q.Id ?? generateId();
        const text = q.QuestionText ?? q.Text ?? q.text ?? "";
        // rawType is either numeric id or string code (preserve for logic)
        const rawType = (q.AnswerType !== undefined && q.AnswerType !== null)
            ? String(q.AnswerType)
            : ((q.Type && q.Type.toString()) || q.type || "");
        // Prefer server-provided friendly name (TypeName), then client lookup, then rawType
        const friendlyType = (q.TypeName && String(q.TypeName).trim() !== "")
            ? String(q.TypeName)
            : (getAnswerTypeName(rawType) || rawType || "");
        const options = q.Options ?? q.options ?? q.Choices ?? q.choices ?? undefined;

        return {
            id,
            text,
            // preserve raw code/id for logic checks
            typeCode: rawType,
            // friendly display name
            typeName: friendlyType,
            // backwards-compatible field used in other parts - keep but not for display
            type: rawType,
            options,
            DatasetId: q.DatasetId ?? q.Raw?.DatasetId ?? null,
            SourceId: q.SourceId ?? q.SourceID ?? null,
            Raw: q
        };
    }


    // load questions from server-provided initial payload (if present) OR from the page handler
    async function loadQuestionsFromApi() {
        // Prefer server-side injected questions if available (fast and avoids route mismatches)
        if (Array.isArray(window.BRAVE_INITIAL_QUESTIONS)) {
            try {
                QUESTIONS = window.BRAVE_INITIAL_QUESTIONS.map(mapQuestionItem);
                return;
            } catch (ex) {
                console.warn('skiplogic: failed to map BRAVE_INITIAL_QUESTIONS', ex);
                QUESTIONS = [];
            }
        }

        const surveyId = discoverSurveyId();
        if (!surveyId) {
            QUESTIONS = [];
            return;
        }

        // Use current path so route parameters (SurveyId/QuestionId/surveyCode) are preserved
        const base = window.location.pathname; // e.g. /surveys/Questionnaire/QuestionBuilder/5/0/ABC
        const url = `${base}?handler=questions&surveyId=${encodeURIComponent(surveyId)}`;
        // console log is intentional for troubleshooting in dev tools
        console.log("skiplogic: fetching questions from", url);

        try {
            const resp = await fetch(url, { method: "GET", headers: { "Accept": "application/json" } });
            console.log("skiplogic: fetch status", resp.status);
            if (!resp.ok) {
                console.warn("skiplogic: questions API returned", resp.status);
                QUESTIONS = [];
                return;
            }
            const json = await resp.json();
            QUESTIONS = (Array.isArray(json) ? json : []).map(mapQuestionItem);
        } catch (err) {
            console.warn("skiplogic: failed to fetch questions", err);
            QUESTIONS = [];
        }
    }

    // UI: operators and questions list
    function renderOperators() {
        if (!operatorsContainer) return;

        operatorsContainer.innerHTML = "";

        // fallback visual operators (UI only — does NOT affect logic)
        const fallback = OPERATORS_BY_TYPE.text || [];

        fallback.forEach(op => {
            const b = document.createElement("div");
            b.className = "badge bg-light border text-secondary p-2";
            b.textContent = op.label;
            b.title = op.id;
            operatorsContainer.appendChild(b);
        });
    }

    function renderQuestions(filter = "") {
        if (!questionsListEl) return;
        questionsListEl.innerHTML = "";
        const lowered = (filter || "").toLowerCase();

        // Work on a copy
        let pool = Array.isArray(LEFT_PANEL_QUESTIONS)
            ? LEFT_PANEL_QUESTIONS.slice()
            : [];


        // If a target question is set, only show questions that appear *before* it in the QUESTIONS array,
        // and explicitly exclude the target itself.
        if (CURRENT_TARGET_QUESTION_ID) {
            const idx = pool.findIndex(m => {
                if (!m) return false;
                const a = (m.SourceId !== undefined && m.SourceId !== null) ? String(m.SourceId) : String(m.id ?? "");
                return String(a) === String(CURRENT_TARGET_QUESTION_ID);
            });

            if (idx >= 0) {
                // slice to keep items before target; this excludes the target itself
                pool = pool.slice(0, idx);
            } else {
                // if not found — fall back to full list (no slicing)
                pool = pool;
            }
        }

        const filtered = pool.filter((q) =>
            (q && q.text ? String(q.text).toLowerCase() : "").includes(lowered)
        );

        if (questionsCountEl) questionsCountEl.textContent = filtered.length;

        if (!filtered.length) {
            const p = document.createElement("div");
            p.className = "p-3 text-muted small";
            p.textContent = "No questions available for this survey.";
            questionsListEl.appendChild(p);
            return;
        } 

        filtered.forEach((q) => {
            const div = document.createElement("div");
            div.className = "card mb-2 p-2 draggable-question";
            div.draggable = true;

            // Show question text and friendly type name (typeName). fallback to typeCode if missing.
            const displayType = (q && (q.typeName || q.typeCode || q.type)) || "";

            div.innerHTML = `
            <div class="d-flex align-items-center justify-content-between">
                <div>
                  <strong>${escapeHtml(q.text || '')}</strong>
                  <div class="small text-muted">${escapeHtml(displayType)}</div>
                </div>
                <div class="text-muted"><i class="bi bi-grip-vertical"></i></div>
            </div>`;

            div.addEventListener("dragstart", (ev) =>
                ev.dataTransfer.setData(
                    "application/json",
                    JSON.stringify({ type: "question", data: q })
                )
            );

            div.addEventListener("click", () => {
                if (!ACTIVE_RULE_ID) return;

                const targetRule = findNode(state.root, ACTIVE_RULE_ID);
                if (!targetRule || targetRule.type !== "rule") return;

                targetRule.q = q.SourceId ?? q.id;
                targetRule.value = "";
                renderTree();
            });


            questionsListEl.appendChild(div);
        });
    }


    // tree rendering
    function renderTree() {
        if (!builderRoot) return;
        builderRoot.innerHTML = "";
        builderRoot.appendChild(renderGroup(state.root));
        updateOutput();
    }

    function renderGroup(node) {
        ensureNodeIds(node);
        const w = document.createElement("div");

        const header = document.createElement("div");
        header.className = "d-flex align-items-center gap-2 mb-2";

        const andBtn = document.createElement("button");
        andBtn.className = "btn btn-sm " + (node.op === "AND" ? "btn-primary" : "btn-outline-secondary");
        andBtn.textContent = "AND";
        andBtn.onclick = () => { node.op = "AND"; renderTree(); };

        const orBtn = document.createElement("button");
        orBtn.className = "btn btn-sm " + (node.op === "OR" ? "btn-warning text-white" : "btn-outline-secondary");
        orBtn.textContent = "OR";
        orBtn.onclick = () => { node.op = "OR"; renderTree(); };

        header.appendChild(andBtn);
        header.appendChild(orBtn);
        w.appendChild(header);

        const children = document.createElement("div");
        children.className = "ps-2";

        node.expressions.forEach((child) =>
            children.appendChild(child.type === "group" ? renderGroup(child) : renderRule(child))
        );

        w.appendChild(children);

        const footer = document.createElement("div");
        footer.className = "mt-2";

        const addRuleBtn = document.createElement("button");
        addRuleBtn.className = "btn btn-sm btn-link";
        addRuleBtn.innerHTML = '<i class="bi bi-plus"></i> Rule';
        addRuleBtn.onclick = () => addRuleToGroup(node.id);

        const addGroupBtn = document.createElement("button");
        addGroupBtn.className = "btn btn-sm btn-link";
        addGroupBtn.innerHTML = '<i class="bi bi-folder-plus"></i> Group';
        addGroupBtn.onclick = () => addGroupToGroup(node.id);

        footer.appendChild(addRuleBtn);
        footer.appendChild(addGroupBtn);

        w.appendChild(footer);

        const container = document.createElement("div");
        container.className = "mb-3 p-3 border rounded bg-white";
        container.appendChild(w);
        return container;
    }
    const OPERATORS_BY_TYPE = {

        number: [
            { id: "EQ", label: "=" },
            { id: "NEQ", label: "≠" },
            { id: "GT", label: ">" },
            { id: "LT", label: "<" },
            { id: "GTE", label: "≥" },
            { id: "LTE", label: "≤" }
        ],

        text: [
            { id: "EQ", label: "=" },
            { id: "NEQ", label: "≠" },
            { id: "CONTAINS", label: "Contains" },
            { id: "STARTS", label: "Starts With" },
            { id: "ENDS", label: "Ends With" },
            { id: "EMPTY", label: "Empty" },
            { id: "NOT_EMPTY", label: "Not Empty" }
        ],

        date: [
            { id: "EQ", label: "=" },
            { id: "NEQ", label: "≠" },
            { id: "GT", label: "After" },
            { id: "LT", label: "Before" },
            { id: "GTE", label: "≥" },
            { id: "LTE", label: "≤" }
        ],

        boolean: [
            { id: "EQ", label: "=" },
            { id: "NEQ", label: "≠" }
        ],

        select_one: [
            { id: "EQ", label: "=" },
            { id: "NEQ", label: "≠" },
            { id: "IN", label: "In" }
        ],

        select_many: [
            { id: "CONTAINS", label: "Contains" },
            { id: "NOT_CONTAINS", label: "Not Contains" },
            { id: "ANY", label: "Any Of" },
            { id: "ALL", label: "All Of" }
        ],

        dataset: [
            { id: "EQ", label: "=" },
            { id: "NEQ", label: "≠" },
            { id: "IN", label: "In" }
        ],
        admin_level: [
            { id: "HAS_VALUE", label: "Has value" },
            { id: "NO_VALUE", label: "Does not have value" }
        ],

        gps: [
            { id: "WITHIN", label: "Within Radius" },
            { id: "OUTSIDE", label: "Outside Radius" }
        ]
    };

    function getOperatorsForType(type) {
        return OPERATORS_BY_TYPE[type] || OPERATORS_BY_TYPE.text;
    }
    function renderRule(node) {
        ensureNodeIds(node);
        const q = (QUESTIONS || []).find((m) => {
            if (!m) return false;
            if (node.q == null) return false;
            return m.SourceId === node.q || m.id == node.q || m.SourceId == node.q;
        });

        const container = document.createElement("div");
        container.className = "d-flex align-items-center gap-2 mb-2 flex-wrap";
        container.addEventListener("click", () => {
            ACTIVE_RULE_ID = node.id;
        });

        const drag = document.createElement("div");
        drag.innerHTML = '<i class="bi bi-grip-vertical"></i>';
        drag.className = "text-muted";
        container.appendChild(drag);

        const question = document.createElement("div");
        question.style.minWidth = "180px";
        question.style.padding = "6px 10px";
        question.style.border = "1px dashed #e9ecef";
        question.style.borderRadius = "6px";
        question.style.cursor = "pointer";

        const displayType = q ? (q.typeName || q.typeCode || "") : "";
        question.innerHTML = q
            ? `<strong>${escapeHtml(q.text)}</strong><div class="small text-muted">${escapeHtml(displayType)}</div>`
            : `<span class="text-muted small">Drag question here</span>`;

        question.addEventListener("dragover", (e) => { e.preventDefault(); question.style.background = "#eef"; });
        question.addEventListener("dragleave", () => (question.style.background = ""));
        question.addEventListener("drop", (e) => {
            e.preventDefault();
            question.style.background = "";
            try {
                const payload = JSON.parse(e.dataTransfer.getData("application/json"));
                if (payload.type === "question") {
                    node.q = payload.data.SourceId ?? payload.data.id;
                    node.value = "";
                    renderTree();
                }
            } catch (ex) { console.warn("drop parse error", ex); }
        });

        container.appendChild(question);


       
     

        const valWrap = document.createElement("div");
        valWrap.style.minWidth = "140px";

        // Determine the canonical type string we'll use for rendering.
        // typeCode may be a numeric id or a code string. If it looks numeric or ambiguous,
        // prefer the friendly name (q.typeName) or other Raw fields.
        let typeCode = q ? (q.typeCode || q.type || "") : "";

        // If typeCode is numeric (e.g. "1") or otherwise not descriptive, try to fall back
        // to friendly names from the question payload.
        const looksLikeNumeric = String(typeCode).trim().match(/^[0-9]+$/);
        if (looksLikeNumeric || !String(typeCode).trim()) {
            typeCode = q && (q.typeName || q.Raw && (q.Raw.TypeName || q.Raw.Type || q.Raw.AnswerType)) || typeCode;
        }

        // --- NORMALIZE TYPE CODE ---
        const t = String(typeCode || "").trim().toUpperCase();

        /*
            Final normalized types:
            INT, NUMERIC  → number
            TEXT          → text
            DATE          → date
            BOOLEAN       → boolean
            SELECT ONE    → select_one
            SELECT MULTIPLE → select_many
            GPS COORDINATES → gps
            PHOTO, DOCUMENT, NOTE, COMPUTED, DATASET → unsupported (free text fallback)
        */

        let normalizedType = "text";

        if (t === "INT" || t === "INTEGER" || t === "NUMERIC" || t === "NUMBER") normalizedType = "number";
        else if (t === "TEXT" || t === "STRING") normalizedType = "text";
        else if (t === "DATE") normalizedType = "date";
        else if (t === "BOOLEAN" || t === "BOOL") normalizedType = "boolean";

        else if (t === "SELECT ONE" || t === "SELECT_ONE" || t === "SELECTONE" || t === "SELECT-ONE" || t === "SELECTONE") normalizedType = "select_one";
        else if (q && q.DatasetId) normalizedType = "dataset";
        // --- ADMINISTRATIVE LEVEL (NEW) ---
        else if (
            t === "ADMINISTRATIVE LEVEL" ||
            t === "ADMIN LEVEL" ||
            t === "ADMIN_LEVEL" ||
            t === "ADMINISTRATIVE_LEVEL"
        ) {
            normalizedType = "admin_level";
        }
        else if (
            t === "SELECT MULTIPLE" ||
            t === "SELECT_MULTIPLE" ||
            t === "SELECTMULTIPLE" ||
            t === "SELECT-MULTIPLE" ||
            t === "SELECT_MANY" ||
            t === "SELECT MANY"
        ) normalizedType = "select_many";

        else if (t === "GPS COORDINATES" || t === "GPS" || t === "COORDINATES") normalizedType = "gps";
        else if (t === "PHOTO" || t === "DOCUMENT" || t === "NOTE" || t === "COMPUTED") normalizedType = "text";



        const sel = document.createElement("select");
        sel.className = "form-select form-select-sm";
        sel.style.width = "140px";

        // get operators dynamically
        const operators = getOperatorsForType(normalizedType);

        // if current operator invalid for new type → reset
        if (!operators.find(o => o.id === node.op)) {
            node.op = operators[0].id;
        }

        operators.forEach(op => {
            const o = document.createElement("option");
            o.value = op.id;
            o.textContent = op.label;
            if (node.op === op.id) o.selected = true;
            sel.appendChild(o);
        });

        sel.onchange = () => {
            node.op = sel.value;
            updateOutput();
        };

        container.appendChild(sel);

        if (!q) {
            // No question selected
            const i = document.createElement("input");
            i.className = "form-control form-control-sm";
            i.disabled = true;
            i.placeholder = "Select question";
            valWrap.appendChild(i);
        }
        // --- ADMIN LEVEL: no value input ---
        else if (normalizedType === "admin_level") {

            // presence-only rule → no value field needed
            node.value = null;

            const info = document.createElement("span");
            info.className = "text-muted small";
            info.textContent = "—";

            valWrap.appendChild(info);
        }
        else if (
            normalizedType === "select_one" ||
            normalizedType === "select_many" ||
            normalizedType === "dataset"
        ) {
            const s = document.createElement("select");
            s.className = "form-select form-select-sm";

            const empty = document.createElement("option");
            empty.value = "";
            empty.textContent = "Select...";
            s.appendChild(empty);

            const lookupId = q.LookupId ?? q.Raw?.LookupId;
            if (lookupId) {
                fetch(`?handler=LookupValues&lookupId=${encodeURIComponent(lookupId)}`)
                    .then(r => r.ok ? r.json() : [])
                    .then(data => {
                        (Array.isArray(data) ? data : []).forEach(opt => {
                            const o = document.createElement("option");
                            o.value = opt.id;
                            o.textContent = opt.text;

                            // restore previous value by TEXT
                            if (node.value != null && String(node.value) === String(opt.id)) {
                                o.selected = true;
                            }

                            s.appendChild(o);
                        });

                        // 🔒 force re-select after async load
                        if (node.value) {
                            const match = [...s.options].find(x =>
                                String(x.value) === String(node.value)
                            );
                            if (match) s.value = match.value;
                        }
                    })
                    .catch(() => { /* silent fail */ });
            }

            s.onchange = () => {
                const selected = s.options[s.selectedIndex];
                if (!selected) return;

                // store lookup ID, not text
                node.value = selected.value || null;

                updateOutput();
            };

            valWrap.appendChild(s);
        }


        else if (normalizedType === "boolean") {

            const s = document.createElement("select");
            s.className = "form-select form-select-sm";
            s.innerHTML = `
        <option value="">Select...</option>
        <option value="true">True</option>
        <option value="false">False</option>
    `;
            s.onchange = () => { node.value = s.value === "true"; updateOutput(); };
            valWrap.appendChild(s);
        }
        else if (normalizedType === "date") {

            const d = document.createElement("input");
            d.type = "date";
            d.className = "form-control form-control-sm";
            d.value = node.value || "";
            d.onchange = () => { node.value = d.value; updateOutput(); };
            valWrap.appendChild(d);

        }
        else if (normalizedType === "number") {

            const n = document.createElement("input");
            n.type = "number";
            n.className = "form-control form-control-sm";
            n.value = node.value || "";
            n.oninput = () => { node.value = n.value; updateOutput(); };
            valWrap.appendChild(n);

        }
        else {
            // Fallback for types like NOTE, DOCUMENT, PHOTO, GPS, COMPUTED
            const i = document.createElement("input");
            i.type = "text";
            i.className = "form-control form-control-sm";
            i.value = node.value || "";
            i.placeholder = normalizedType === "gps" ? "lat,long" : "";
            i.oninput = () => { node.value = i.value; updateOutput(); };
            valWrap.appendChild(i);
        }


        container.appendChild(valWrap);

        const del = document.createElement("button");
        del.className = "btn btn-sm btn-light text-danger";
        del.innerHTML = '<i class="bi bi-trash"></i>';
        del.onclick = () => deleteNode(node.id);
        container.appendChild(del);

        return container;
    }

    // tree helpers
    function findNode(root, id) {
        if (!root) return null;
        if (root.id === id) return root;
        if (root.type === "group") {
            for (const ch of root.expressions) {
                const found = findNode(ch, id);
                if (found) return found;
            }
        }
        return null;
    }

    function findParent(root, id) {
        if (!root || root.type !== "group") return null;
        for (const ch of root.expressions) {
            if (ch.id === id) return root;
            if (ch.type === "group") {
                const f = findParent(ch, id);
                if (f) return f;
            }
        }
        return null;
    }

    function addRuleToGroup(groupId, prefill = null) {
        // If invoked with a prefill question, treat that question as the "target".
        // IMPORTANT: do NOT clear CURRENT_TARGET_QUESTION_ID when no prefill is supplied.
        if (prefill) {
            CURRENT_TARGET_QUESTION_ID = String(prefill.SourceId ?? prefill.id ?? "");
        }
        // otherwise: keep the previously-set target (do not null it out)

        const g = findNode(state.root, groupId);
        if (!g || g.type !== "group") return;

        const newRule = {
            id: generateId(),
            type: "rule",
            op: "EQ",
            q: prefill ? (prefill.SourceId ?? prefill.id) : null,
            value: ""
        };
        g.expressions.push(newRule);

        // re-render questions and tree
        renderQuestions();
        renderTree();
    }



    function addGroupToGroup(groupId) {
        const g = findNode(state.root, groupId);
        if (!g || g.type !== "group") return;

        const newGroup = {
            id: generateId(),
            type: "group",
            op: "AND",
            expressions: [
                { id: generateId(), type: "rule", op: "EQ", q: null, value: "" }
            ]
        };
        g.expressions.push(newGroup);
        renderTree();
    }

    function deleteNode(id) {
        const p = findParent(state.root, id);
        if (!p) return;

        p.expressions = p.expressions.filter((c) => c.id !== id);

        if (ACTIVE_RULE_ID === id) {
            ACTIVE_RULE_ID = null; // clear stale reference
        }

        if (!state.root.expressions.length) {
            renderQuestions(); // restore original filtered list
        }

        renderTree();
    }



    // output transform
    function transformOutput(node) {
        if (!node) return null;
        if (node.type === "rule") {
            if (!node.q && node.q !== 0) return null;
            return { op: node.op, q: node.q, value: node.value };
        }
        const children = node.expressions.map(transformOutput).filter(Boolean);
        return children.length ? { op: node.op, expressions: children } : null;
    }

    function updateOutput() {
        const out = transformOutput(state.root);
        const t = out ? JSON.stringify(out, null, 2) : "{}";
        if (jsonOutput) jsonOutput.textContent = t;
    }

    // public API
    window.renderQuestions = renderQuestions;
    window.renderOperators = renderOperators;
    window.renderTree = renderTree;
    window.exportBuilderState = () => {
        const clone = JSON.parse(JSON.stringify(state.root));
        removeIds(clone);
        return clone;
    };

    function removeIds(node) {
        if (!node) return;
        delete node.id;
        if (node.expressions) node.expressions.forEach(removeIds);
    }


    window.loadBuilderState = (rootState) => {
        if (!rootState) return;
        const candidate = rootState.type === "group" ? rootState : rootState.root || rootState;
        if (candidate && candidate.type === "group") {
            ensureNodeIds(candidate);
            state.root = JSON.parse(JSON.stringify(candidate));
        } else {
            console.warn("Invalid expression tree structure passed to loadBuilderState");
        }
        renderTree();
    };

    window.clearBuilder = function () {
        state.root = { type: "group", op: "AND", expressions: [] };
        renderQuestions();   // re-render using ORIGINAL filter
        renderTree();
    };



    window.addRuleToGroup = addRuleToGroup;

    window.openBuilderModal = function (options = {}) {
        const { target = null, initial = null, onSave, onCancel } = options;
        if (modalTitleEl) modalTitleEl.textContent = target ? `Edit ${target}` : "Expression Builder";
        if (initial) {
            try {
                const parsed = typeof initial === "string" ? JSON.parse(initial) : initial;
                window.loadBuilderState(parsed);
            } catch (e) { console.warn("Invalid initial JSON", e); }
        } else {
            renderTree();
        }
        if (bsModal) bsModal.show();
        let saved = false;
        function localSave() {
            saved = true;
            const out = window.exportBuilderState();
            if (typeof onSave === "function") onSave(out);
        }
        if (modalSaveBtn) modalSaveBtn.addEventListener("click", localSave, { once: true });
        if (modalConfirmApplyBtn)
            modalConfirmApplyBtn.addEventListener("click", async () => {

                try {
                    showLoader();              //  start loader

                    if (!saved)
                        await localSave();     // wait for skiplogic save

                    if (bsModal)
                        bsModal.hide();
                }
                finally {
                    hideLoader();              // stop loader
                }

            }, { once: true });
        if (modalRoot && bsModal) modalRoot.addEventListener("hidden.bs.modal", () => { if (!saved && typeof onCancel === "function") onCancel(); }, { once: true });
    };

    // initialization
    async function initBuilder() { 
    // read server-provided target id (string) if present
    try {
        if (typeof window.BRAVE_TARGET_QUESTION_ID !== "undefined" && window.BRAVE_TARGET_QUESTION_ID) {
            CURRENT_TARGET_QUESTION_ID = String(window.BRAVE_TARGET_QUESTION_ID);
        } else {
            CURRENT_TARGET_QUESTION_ID = null;
        }
    } catch (e) {
        CURRENT_TARGET_QUESTION_ID = null;
    }

        if (searchInput) {
            searchInput.addEventListener("input", (ev) => renderQuestions(ev.target.value));
        }

        if (modalClearBtn) {
            modalClearBtn.addEventListener("click", () => {
                try { window.clearBuilder(); } catch (e) { console.error("clear error", e); }
            });
        }

        await loadQuestionsFromApi();
        // Freeze left panel questions exactly once
        LEFT_PANEL_QUESTIONS = QUESTIONS.filter(q => {
            const t = String(q.typeName || q.typeCode || "").toUpperCase();
            return t !== "DATASET";
        });



        // load injected builder state (if server provided JSON string/object)
        if (typeof window.BRAVE_INITIAL_BUILDER_STATE !== 'undefined' && window.BRAVE_INITIAL_BUILDER_STATE) {
            try {
                const parsed = (typeof window.BRAVE_INITIAL_BUILDER_STATE === 'string')
                    ? JSON.parse(window.BRAVE_INITIAL_BUILDER_STATE)
                    : window.BRAVE_INITIAL_BUILDER_STATE;
                if (parsed && typeof window.loadBuilderState === 'function') {
                    window.loadBuilderState(parsed);
                }
            } catch (e) {
                console.warn('Failed parsing BRAVE_INITIAL_BUILDER_STATE', e);
            }
        }


        ensureNodeIds(state.root);

        renderOperators();
        renderTree();
        renderQuestions();

        setTimeout(updateOutput, 250);
        document.addEventListener("click", updateOutput);
    }

    initBuilder().catch((e) => {
        console.error("skiplogic initialization failed", e);
        // leave QUESTIONS empty and render so the user sees the "No questions..." message
        QUESTIONS = [];
        renderOperators();
        renderTree();
        renderQuestions();
        updateOutput();
    });
});

