// Multi-colour palettes
const palette = ['#3b82f6', '#06b6d4', '#ef4444', '#f59e0b', '#10b981', '#8b5cf6', '#f97316'];
const bluePalette = ['#3b82f6', '#60a5fa', '#1d4ed8'];

// Placeholder sample data
const sample = {
    beneficiaries: [
        { id: 1, sex: "male", dob: "1990-01-01", mission: "mission_a", program: "health", registered: "2025-02-02" },
        { id: 2, sex: "female", dob: "1992-05-01", mission: "mission_b", program: "cash", registered: "2025-03-02" },
        { id: 3, sex: "female", dob: "2015-02-01", mission: "mission_a", program: "health", registered: "2025-04-02" }
    ],
    surveys: [
        { id: 1, title: "Needs Assessment", responses: 40, mission: "mission_a", program: "health", created: "2025-01-01" },
        { id: 2, title: "Shelter Check", responses: 27, mission: "mission_b", program: "cash", created: "2025-02-15" }
    ],
    assistances: [
        { id: 1, demographic: "female-headed", mission: "mission_a", program: "health", date: "2025-01-10" },
        { id: 2, demographic: "elderly", mission: "mission_b", program: "cash", date: "2025-02-20" }
    ],
    assessments: [
        { status: "approved", mission: "mission_a", program: "health", created: "2025-02-01" },
        { status: "pending", mission: "mission_b", program: "cash", created: "2025-02-10" },
        { status: "rejected", mission: "mission_a", program: "health", created: "2025-03-10" },
        { status: "created", mission: "mission_a", program: "health", created: "2025-04-01" }
    ]
};

// Chart handles
let chartRegistrations, chartResponses, chartAssistances, chartAssessmentsStatus;

// Helpers
function isChild(dob) {
    if (!dob) return false;
    const age = (new Date() - new Date(dob)) / (365.25 * 24 * 60 * 60 * 1000);
    return age < 18;
}
function ctx(id) {
    const c = document.getElementById(id);
    return c ? c.getContext("2d") : null;
}

// Build charts
function buildCharts() {
    try {
        // Registrations
        const ctxReg = ctx("chartRegistrations");
        if (ctxReg) {
            chartRegistrations = new Chart(ctxReg, {
                type: "bar",
                data: {
                    labels: ["Male", "Female", "Children(Under 18)"],
                    datasets: [{
                        data: [0, 0, 0],
                        backgroundColor: bluePalette,
                        borderColor: bluePalette,
                        borderWidth: 1,
                        borderRadius: 6
                    }]
                },
                options: { responsive: true, maintainAspectRatio: false }
            });
        }

        // Responses
        const ctxResp = ctx("chartResponses");
        if (ctxResp) {
            chartResponses = new Chart(ctxResp, {
                type: "bar",
                data: {
                    labels: [],
                    datasets: [{
                        label: "Responses",
                        data: [],
                        backgroundColor: palette,
                        borderColor: palette,
                        borderWidth: 1,
                        borderRadius: 6
                    }]
                },
                options: { responsive: true, maintainAspectRatio: false }
            });
        }

        // Pie (assistances)
        const ctxAssist = ctx("chartAssistances");
        if (ctxAssist) {
            chartAssistances = new Chart(ctxAssist, {
                type: "pie",
                data: { labels: [], datasets: [{ data: [], backgroundColor: palette }] },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    aspectRatio: 1.2,
                    plugins: { legend: { position: "bottom" } }
                }
            });
        }

        // Doughnut (assessment status)
        const ctxAssess = ctx("chartAssessmentsStatus");
        if (ctxAssess) {
            chartAssessmentsStatus = new Chart(ctxAssess, {
                type: "doughnut",
                data: { labels: [], datasets: [{ data: [], backgroundColor: palette.slice(0, 4) }] },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    aspectRatio: 1.2,
                    plugins: { legend: { position: "bottom" } }
                }
            });
        }
    } catch (err) {
        console.error('buildCharts error:', err);
    }
}

function applyFilters() {
    try {
        const b = sample.beneficiaries || [];

        const males = b.filter(x => x.sex === "male").length;
        const females = b.filter(x => x.sex === "female").length;
        const children = b.filter(x => isChild(x.dob)).length;

        if (chartRegistrations) {
            chartRegistrations.data.datasets[0].data = [males, females, children];
            chartRegistrations.update();
        }

        const s = sample.surveys || [];
        if (chartResponses) {
            chartResponses.data.labels = s.map(x => x.title);
            chartResponses.data.datasets[0].data = s.map(x => x.responses);
            chartResponses.update();
        }

        const a = sample.assistances || [];
        const groups = {};
        a.forEach(x => groups[x.demographic] = (groups[x.demographic] || 0) + 1);
        if (chartAssistances) {
            chartAssistances.data.labels = Object.keys(groups);
            chartAssistances.data.datasets[0].data = Object.values(groups);
            chartAssistances.update();
        }

        const st = { approved: 0, rejected: 0, pending: 0, created: 0 };
        (sample.assessments || []).forEach(x => { if (x && x.status) st[x.status] = (st[x.status] || 0) + 1; });
        if (chartAssessmentsStatus) {
            chartAssessmentsStatus.data.labels = Object.keys(st);
            chartAssessmentsStatus.data.datasets[0].data = Object.values(st);
            chartAssessmentsStatus.update();
        }

        const lu = document.getElementById("lastUpdated");
        if (lu) lu.innerText = new Date().toLocaleString();
    } catch (err) {
        console.error('applyFilters error:', err);
    }
}

/* ---------- Pending tasks static section (Dashboard) ---------- */
/* Sample task shape and data (replace with real fetch if available) */
const pendingTasksSample = [
    {
        id: 'T-1234',
        title: 'Verify beneficiary documents',
        mission: 'mission_a',
        program: 'health',
        dueDate: '2025-12-12',
        assignedTo: 'Jane Doe',
        priority: 'High',
        description: 'Verify scanned ID + proof of residence for beneficiary #1234.',
        status: 'pending'
    }
];

// safety escape
function escapeHtml(s) {
    if (s === null || s === undefined) return '';
    return String(s).replace(/[&<>"']/g, function (m) { return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[m]; });
}

function renderTaskDetailHtml(task) {
    return `
    <div>
      <h5>${escapeHtml(task.title)}</h5>
      <p class="text-muted small mb-2">Task ID: ${escapeHtml(task.id)} · Mission: ${escapeHtml(task.mission)} · Program: ${escapeHtml(task.program)}</p>
      <p>${escapeHtml(task.description || 'No description provided.')}</p>
      <ul class="list-inline small text-muted">
        <li class="list-inline-item">Assigned to: <strong>${escapeHtml(task.assignedTo || 'Unassigned')}</strong></li>
        <li class="list-inline-item">Priority: <strong>${escapeHtml(task.priority || 'Normal')}</strong></li>
        <li class="list-inline-item">Due: <strong>${escapeHtml(formatDateStr(task.dueDate))}</strong></li>
      </ul>
      <div class="mt-3 d-flex gap-2">
        <button class="btn btn-sm btn-primary">Take action</button>
        <button class="btn btn-sm btn-outline-secondary btn-back-to-list">Back to list</button>
      </div>
    </div>
  `;
}

function formatDateStr(d) {
    if (!d) return '-';
    try {
        const dt = new Date(d);
        if (isNaN(dt)) return d;
        return dt.toLocaleDateString();
    } catch (e) { return d; }
}

function showPendingTasksModal(pendingTasks = []) {
    const modalEl = document.getElementById('pendingTaskModal');
    const contentEl = document.getElementById('pendingTaskContent');
    if (!modalEl || !contentEl) return;

    const html = (pendingTasks.length === 1)
        ? renderTaskDetailHtml(pendingTasks[0])
        : `<div class="list-group">${pendingTasks.map(t => `
         <button type="button" class="list-group-item list-group-item-action pending-task-item" data-task-id="${escapeHtml(t.id)}">
           <div class="d-flex w-100 justify-content-between">
             <h6 class="mb-1">${escapeHtml(t.title)}</h6>
             <small class="text-muted">${escapeHtml(formatDateStr(t.dueDate))}</small>
           </div>
           <p class="mb-1 text-muted">${escapeHtml(t.mission)} · ${escapeHtml(t.program)}</p>
           <small class="text-muted">Assigned to: ${escapeHtml(t.assignedTo || 'Unassigned')} · Priority: ${escapeHtml(t.priority || 'Normal')}</small>
         </button>`).join('')}</div>
      `;

    contentEl.innerHTML = html;

    if (pendingTasks.length > 1) {
        contentEl.querySelectorAll('.pending-task-item').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = btn.dataset.taskId;
                const task = pendingTasks.find(t => String(t.id) === String(id));
                if (task) {
                    contentEl.innerHTML = renderTaskDetailHtml(task);
                    const backBtn = contentEl.querySelector('.btn-back-to-list');
                    if (backBtn) backBtn.addEventListener('click', () => showPendingTasksModal(pendingTasks));
                }
            });
        });
    }

    const bs = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false });
    bs.show();
}

function renderPendingTasks(tasks = []) {
    const wrap = document.getElementById('pendingTasksWrap');
    if (!wrap) {
        // nothing to render — likely markup missing
        console.warn('#pendingTasksWrap not found in DOM');
        return;
    }

    const pending = (tasks || []).filter(t => (t.status || '').toLowerCase() === 'pending');

    if (pending.length > 0) {
        wrap.innerHTML = `
      <div class="static-notification warning" role="status" aria-live="polite">
        <div class="left d-flex gap-3 align-items-center">
          <div class="icon text-warning"><i class="fa-solid fa-triangle-exclamation"></i></div>
          <div>
            <div><strong>${pending.length} pending task${pending.length > 1 ? 's' : ''}</strong></div>
            <div class="meta">You have pending task${pending.length > 1 ? 's' : ''}. Please review them.</div>
          </div>
        </div>
        <div class="right">
          <div class="d-flex gap-2">
            <button id="viewPendingBtn" class="btn btn-sm btn-outline-dark btn-view">View</button>
          </div>
        </div>
      </div>
    `;
        const viewBtn = document.getElementById('viewPendingBtn');
        if (viewBtn) viewBtn.addEventListener('click', () => showPendingTasksModal(pending));
    } else {
        wrap.innerHTML = `
      <div class="static-notification success" role="status" aria-live="polite">
        <div class="left d-flex gap-3 align-items-center">
          <div class="icon text-success"><i class="fa-solid fa-circle-check"></i></div>
          <div>
            <div><strong>No pending tasks</strong></div>
            <div class="meta">You have no pending tasks at the moment.</div>
          </div>
        </div>
        <div class="right">
          <div class="d-flex gap-2">
            <a href="#" class="btn btn-sm btn-outline-success btn-view" onclick="return false;">OK</a>
          </div>
        </div>
      </div>
    `;
    }
}

// Init (single DOMContentLoaded handler)
document.addEventListener("DOMContentLoaded", () => {
    // Build charts and show sample data
    buildCharts();
    applyFilters();

    // Bind apply button if present
    const applyBtn = document.getElementById("applyBtn");
    if (applyBtn) applyBtn.addEventListener('click', applyFilters);

    // Raw data button (open Raw Data page)
    const rawBtn = document.getElementById("rawDataBtn");
    if (rawBtn) rawBtn.addEventListener('click', () => {
        // change path if your raw data route differs (e.g., /RawData)
        window.location.href = '/RawData';
    });

    // Initialize pending tasks (replace pendingTasksSample with fetch if you have an API)
    try {
        renderPendingTasks(typeof pendingTasks !== 'undefined' ? pendingTasks : pendingTasksSample);
    } catch (err) {
        console.error('pending tasks render error', err);
    }
});
