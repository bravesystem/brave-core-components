let currentPage = 1;
let pageSize = 10;
let rows = [];
let filtered = [];

document.addEventListener('DOMContentLoaded', () => {
    rows = Array.from(document.querySelectorAll('#rawTbody tr'));
    filtered = rows.slice();

    bindEvents();
    render(); // ✅ initial render
});

function bindEvents() {
    document.getElementById('rdApply')?.addEventListener('click', applyFilters);

    document.getElementById('pageSize')?.addEventListener('change', () => {
        currentPage = 1;
        render();
    });

    document.querySelectorAll('#rawTableHead th.sortable').forEach(th => {
        th.addEventListener('click', () => sortTable(th.dataset.key));
    });

    document.getElementById('btnExportCsv')?.addEventListener('click', exportCSV);
}

function render() {
    const tbody = document.getElementById('rawTbody');
    tbody.innerHTML = '';

    pageSize = parseInt(document.getElementById('pageSize').value, 10);
    const start = (currentPage - 1) * pageSize;
    const pageItems = filtered.slice(start, start + pageSize);

    pageItems.forEach(r => tbody.appendChild(r));

    document.getElementById('showingRange').textContent =
        filtered.length
            ? `${start + 1} - ${Math.min(start + pageSize, filtered.length)}`
            : '0';

    document.getElementById('showingTotal').textContent = filtered.length;

    renderPagination(); // ✅ build page buttons
}

function renderPagination() {
    const pagination = document.getElementById('pagination');
    pagination.innerHTML = '';

    const totalPages = Math.ceil(filtered.length / pageSize);
    if (totalPages <= 1) return;

    for (let i = 1; i <= totalPages; i++) {
        const li = document.createElement('li');
        li.className = `page-item ${i === currentPage ? 'active' : ''}`;

        const a = document.createElement('a');
        a.className = 'page-link';
        a.href = '#';
        a.innerText = i;

        a.addEventListener('click', e => {
            e.preventDefault();
            currentPage = i;
            render();
        });

        li.appendChild(a);
        pagination.appendChild(li);
    }
}

function sortTable(key) {
    filtered.sort((a, b) => {
        const ta = a.querySelector(`td:nth-child(${getIndex(key)})`).innerText.toLowerCase();
        const tb = b.querySelector(`td:nth-child(${getIndex(key)})`).innerText.toLowerCase();
        return ta.localeCompare(tb);
    });

    currentPage = 1;
    render();
}

function getIndex(key) {
    return [...document.querySelectorAll('#rawTableHead th')]
        .findIndex(th => th.dataset.key === key) + 1;
}

function exportCSV() {
    let csv = [];
    const headers = [...document.querySelectorAll('#rawTableHead th')]
        .map(h => h.innerText);
    csv.push(headers.join(','));

    filtered.forEach(r => {
        const cols = [...r.children].map(td => `"${td.innerText}"`);
        csv.push(cols.join(','));
    });

    const blob = new Blob([csv.join('\n')], { type: 'text/csv' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'brave_raw_data.csv';
    a.click();
}


function applyFilters() {
    currentPage = 1;
    render();
}

