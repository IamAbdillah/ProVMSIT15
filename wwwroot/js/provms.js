// ProVMS Core JavaScript

// =============================================
// NOTIFICATION BELL HUB
// =============================================
async function loadNotifications() {
    try {
        const res = await fetch('/api/Notifications/list', { credentials: 'include' });
        const notes = await res.json();
        const list = document.getElementById('notifList');
        if (!notes || notes.length === 0) {
            list.innerHTML = '<div class="text-center text-muted py-4 small"><i class="bi bi-bell-slash fs-4 d-block mb-2"></i>No notifications</div>';
            return;
        }
        list.innerHTML = notes.map(n => `
            <div class="notif-item ${!n.isRead ? 'unread' : ''}" id="notif-${n.id}" onclick="markRead(${n.id})">
                <div>${n.notificationText}</div>
                <div class="notif-time"><i class="bi bi-clock me-1"></i>${formatTime(n.createdAt)}</div>
            </div>`).join('');
    } catch (e) { console.warn('Notifications unavailable'); }
}

async function markRead(id) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    await fetch(`/api/Notifications/mark-read/${id}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': token || '' },
        credentials: 'include'
    });
    const el = document.getElementById(`notif-${id}`);
    if (el) el.classList.remove('unread');
    updateBadgeCount();
}

async function markAllRead() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    await fetch('/api/Notifications/mark-all-read', {
        method: 'POST',
        headers: { 'RequestVerificationToken': token || '' },
        credentials: 'include'
    });
    document.querySelectorAll('.notif-item.unread').forEach(el => el.classList.remove('unread'));
    const badge = document.getElementById('notifBadge');
    if (badge) badge.style.display = 'none';
}

async function updateBadgeCount() {
    try {
        const res = await fetch('/api/Notifications/unread-count', { credentials: 'include' });
        const data = await res.json();
        const badge = document.getElementById('notifBadge');
        if (badge) {
            if (data.count > 0) {
                badge.textContent = data.count > 99 ? '99+' : data.count;
                badge.style.display = '';
            } else {
                badge.style.display = 'none';
            }
        }
    } catch (e) {}
}

function formatTime(dt) {
    const d = new Date(dt);
    const now = new Date();
    const diff = Math.floor((now - d) / 60000);
    if (diff < 1) return 'Just now';
    if (diff < 60) return `${diff}m ago`;
    if (diff < 1440) return `${Math.floor(diff/60)}h ago`;
    return d.toLocaleDateString();
}

// Poll notification badge every 30 seconds
if (document.getElementById('notifBadge')) {
    updateBadgeCount();
    setInterval(updateBadgeCount, 30000);
}

// =============================================
// VENDOR ONBOARDING WIZARD
// =============================================
let currentStep = 1;
const totalSteps = 3;

function goToStep(step) {
    document.querySelectorAll('.wizard-panel').forEach(p => p.classList.remove('active'));
    for (let i = 1; i <= totalSteps; i++) {
        const ws = document.getElementById(`ws-${i}`);
        if (!ws) continue;
        ws.classList.remove('active', 'completed');
        const numEl = ws.querySelector('.wizard-step-num');
        if (i < step) {
            ws.classList.add('completed');
            if (numEl) numEl.innerHTML = '<i class="bi bi-check-lg"></i>';
        } else {
            if (numEl) numEl.textContent = i;
        }
        if (i === step) ws.classList.add('active');
    }
    const panel = document.getElementById(`step-${step}`);
    if (panel) panel.classList.add('active');
    currentStep = step;
    updateWizardButtons();
}

function nextStep() {
    if (!validateStep(currentStep)) return;
    if (currentStep < totalSteps) goToStep(currentStep + 1);
}

function prevStep() {
    if (currentStep > 1) goToStep(currentStep - 1);
}

function validateStep(step) {
    const panel = document.getElementById(`step-${step}`);
    if (!panel) return true;
    const required = panel.querySelectorAll('[required]');
    let valid = true;
    required.forEach(el => {
        el.classList.remove('is-invalid');
        if (!el.value.trim()) { el.classList.add('is-invalid'); valid = false; }
    });
    if (step === 1) {
        const pw = panel.querySelector('#Password');
        const cpw = panel.querySelector('#ConfirmPassword');
        if (pw && cpw && pw.value !== cpw.value) {
            cpw.classList.add('is-invalid');
            valid = false;
        }
    }
    return valid;
}

function updateWizardButtons() {
    const prev = document.getElementById('btnPrev');
    const next = document.getElementById('btnNext');
    const submit = document.getElementById('btnSubmit');
    if (prev) prev.style.display = currentStep === 1 ? 'none' : '';
    if (next) next.style.display = currentStep === totalSteps ? 'none' : '';
    if (submit) submit.style.display = currentStep === totalSteps ? '' : 'none';
}

// =============================================
// DOCUMENT DRAG & DROP
// =============================================
function initDragDrop() {
    const zone = document.getElementById('dropZone');
    const input = document.getElementById('DocumentFile');
    if (!zone || !input) return;

    zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('dragover'); });
    zone.addEventListener('dragleave', () => zone.classList.remove('dragover'));
    zone.addEventListener('drop', e => {
        e.preventDefault();
        zone.classList.remove('dragover');
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            const file = files[0];
            if (file.type !== 'application/pdf') {
                showZoneError('Only PDF files are accepted.');
                return;
            }
            if (file.size > 5242880) {
                showZoneError('File exceeds 5MB limit.');
                return;
            }
            const dt = new DataTransfer();
            dt.items.add(file);
            input.files = dt.files;
            zone.querySelector('.drop-label').textContent = file.name;
            zone.classList.add('border-success');
        }
    });
    zone.addEventListener('click', () => input.click());
    input.addEventListener('change', () => {
        if (input.files.length > 0) {
            zone.querySelector('.drop-label').textContent = input.files[0].name;
        }
    });
}

function showZoneError(msg) {
    const zone = document.getElementById('dropZone');
    if (zone) { zone.classList.add('border-danger'); zone.querySelector('.drop-label').textContent = msg; }
}

// =============================================
// CATALOG ITEM TABLE EDITOR
// =============================================
let catalogRowIndex = 0;

function addCatalogRow() {
    const tbody = document.getElementById('catalogTbody');
    if (!tbody) return;
    const idx = catalogRowIndex++;
    const row = document.createElement('tr');
    row.innerHTML = `
        <td><input type="text" name="CatalogItems[${idx}].ItemName" placeholder="e.g. ProWorkstation V1" class="form-control form-control-sm" required /></td>
        <td>
            <select name="CatalogItems[${idx}].Category" class="form-select form-select-sm" required>
                <option value="IT_Hardware">IT Hardware</option>
                <option value="Office_Facilities">Office Facilities</option>
                <option value="Marketing_Collateral">Marketing Collateral</option>
            </select>
        </td>
        <td><input type="text" inputmode="decimal" name="CatalogItems[${idx}].UnitPrice" placeholder="0.00" class="form-control form-control-sm price-input" required /></td>
        <td><button type="button" class="btn btn-sm btn-outline-danger" onclick="removeRow(this)"><i class="bi bi-trash"></i></button></td>`;
    tbody.appendChild(row);
    initPriceMasks();
}

function removeRow(btn) {
    btn.closest('tr').remove();
    reindexCatalogRows();
}

function reindexCatalogRows() {
    const tbody = document.getElementById('catalogTbody');
    if (!tbody) return;
    tbody.querySelectorAll('tr').forEach((row, i) => {
        row.querySelectorAll('[name]').forEach(el => {
            el.name = el.name.replace(/\[\d+\]/, `[${i}]`);
        });
    });
    catalogRowIndex = tbody.querySelectorAll('tr').length;
}

// =============================================
// PRICE INPUT MASK — blocks non-numeric input in catalog price fields
// =============================================
function initPriceMasks() {
    document.querySelectorAll('.price-input').forEach(input => {
        if (input.dataset.masked) return;
        input.dataset.masked = '1';
        input.addEventListener('keydown', e => {
            const allowed = ['Backspace','Delete','ArrowLeft','ArrowRight','Tab','Home','End','.'];
            if (allowed.includes(e.key)) return;
            if (e.key >= '0' && e.key <= '9') return;
            // Block anything else
            e.preventDefault();
            input.classList.add('input-error');
            setTimeout(() => input.classList.remove('input-error'), 600);
        });
        input.addEventListener('blur', () => {
            const val = parseFloat(input.value);
            if (!isNaN(val) && val > 0) {
                input.value = val.toFixed(2);
                input.classList.remove('input-error');
            } else if (input.value !== '') {
                input.classList.add('input-error');
            }
        });
        input.addEventListener('focus', () => input.classList.remove('input-error'));
    });
}

// =============================================
// MARKETPLACE PRICE LOCK
// =============================================
function initPriceLock() {
    const qtyInput = document.getElementById('quantityInput');
    const itemSelect = document.getElementById('itemSelect');
    const totalBox = document.getElementById('totalBox');
    const unitPriceBox = document.getElementById('unitPriceBox');
    if (!qtyInput || !totalBox) return;

    async function updateTotal() {
        const itemId = itemSelect ? itemSelect.value : document.getElementById('itemId')?.value;
        const qty = parseInt(qtyInput.value) || 0;
        if (!itemId || qty <= 0) { totalBox.textContent = '---'; return; }
        try {
            const res = await fetch(`/Catalog/GetItemPrice?itemId=${itemId}`);
            const data = await res.json();
            if (unitPriceBox) unitPriceBox.textContent = `$${parseFloat(data.unitPrice).toFixed(2)}`;
            totalBox.textContent = `$${(data.unitPrice * qty).toFixed(2)}`;
        } catch { totalBox.textContent = 'Error'; }
    }

    qtyInput.addEventListener('input', updateTotal);
    if (itemSelect) itemSelect.addEventListener('change', updateTotal);
}

// =============================================
// CHART.JS DASHBOARD INITIALIZATION
// =============================================
function initDashboardCharts(expenseLabels, expenseValues, budgetLabels, budgetAllocated, budgetRemaining, scoreLabels, scoreValues) {
    const colors = ['#2563eb','#7c3aed','#059669','#d97706','#dc2626','#0891b2','#65a30d','#c2410c'];

    const ctx1 = document.getElementById('expenseChart');
    if (ctx1) {
        new Chart(ctx1, {
            type: 'bar',
            data: {
                labels: expenseLabels,
                datasets: [{
                    label: 'Total Expenses',
                    data: expenseValues,
                    backgroundColor: colors.slice(0, expenseLabels.length),
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { callback: v => '₱' + v.toLocaleString(), font: { size: 11 } } }, x: { grid: { display: false }, ticks: { font: { size: 11 } } } }
            }
        });
    }

    const ctx2 = document.getElementById('budgetChart');
    if (ctx2) {
        new Chart(ctx2, {
            type: 'bar',
            data: {
                labels: budgetLabels,
                datasets: [
                    { label: 'Allocated', data: budgetAllocated, backgroundColor: '#2563eb', borderRadius: 4 },
                    { label: 'Remaining', data: budgetRemaining, backgroundColor: '#059669', borderRadius: 4 }
                ]
            },
            options: {
                responsive: true,
                plugins: { legend: { position: 'top', labels: { font: { size: 11 }, usePointStyle: true } } },
                scales: { y: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { callback: v => '₱' + v.toLocaleString(), font: { size: 11 } } }, x: { grid: { display: false }, ticks: { font: { size: 11 } } } }
            }
        });
    }

    const ctx3 = document.getElementById('vendorScoreChart');
    if (ctx3) {
        new Chart(ctx3, {
            type: 'bar',
            data: {
                labels: scoreLabels,
                datasets: [{
                    label: 'Avg Rating',
                    data: scoreValues,
                    backgroundColor: scoreValues.map(v => v >= 4 ? '#059669' : v >= 2.5 ? '#d97706' : '#dc2626'),
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                plugins: { legend: { display: false } },
                scales: { x: { min: 0, max: 5, grid: { color: '#f1f5f9' }, ticks: { stepSize: 1, font: { size: 11 } } }, y: { grid: { display: false }, ticks: { font: { size: 11 } } } }
            }
        });
    }
}

// =============================================
// INIT ON DOM READY
// =============================================
document.addEventListener('DOMContentLoaded', () => {
    initDragDrop();
    initPriceLock();
    initPriceMasks();
    if (document.getElementById('step-1')) goToStep(1);
});
