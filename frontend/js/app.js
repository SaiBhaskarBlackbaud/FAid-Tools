// Application state
const State = {
    currentScreen: 'upload',
    currentApplicationId: null,
    uploadedFiles: {},
    pollingInterval: null
};

// Screen navigation
function showScreen(screenId) {
    document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
    document.querySelectorAll('.nav-link').forEach(n => n.classList.remove('active'));
    const screen = document.getElementById(`screen-${screenId}`);
    if (screen) screen.classList.add('active');
    const navLink = document.querySelector(`[data-screen="${screenId}"]`);
    if (navLink) navLink.classList.add('active');
    State.currentScreen = screenId;

    // Load data for screens
    if (screenId === 'dashboard') loadDashboard();
}

// ===== UPLOAD SCREEN =====
function initUploadScreen() {
    const form = document.getElementById('upload-form');
    if (!form) return;

    // File drop zones
    document.querySelectorAll('.drop-zone').forEach(zone => {
        zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('dragover'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('dragover'));
        zone.addEventListener('drop', e => {
            e.preventDefault();
            zone.classList.remove('dragover');
            const docType = zone.dataset.doctype;
            const file = e.dataTransfer.files[0];
            if (file) handleFileSelect(docType, file, zone);
        });
        zone.addEventListener('click', () => {
            const input = zone.querySelector('input[type="file"]');
            if (input) input.click();
        });
    });

    document.querySelectorAll('.file-input').forEach(input => {
        input.addEventListener('change', e => {
            const docType = input.dataset.doctype;
            const file = e.target.files[0];
            const zone = document.querySelector(`.drop-zone[data-doctype="${docType}"]`);
            if (file && zone) handleFileSelect(docType, file, zone);
        });
    });

    form.addEventListener('submit', handleUploadSubmit);
}

function handleFileSelect(docType, file, zone) {
    State.uploadedFiles[docType] = file;
    zone.classList.add('has-file');
    const label = zone.querySelector('.drop-zone-label');
    if (label) label.textContent = `✓ ${file.name}`;
    updateUploadButton();
}

function updateUploadButton() {
    const btn = document.getElementById('btn-upload-submit');
    if (!btn) return;
    const hasAppForm = !!State.uploadedFiles['ApplicationForm'];
    btn.disabled = !hasAppForm;
}

async function handleUploadSubmit(e) {
    e.preventDefault();
    const familyName = document.getElementById('family-name')?.value?.trim();
    const grade = parseInt(document.getElementById('grade')?.value || '0');
    const dependents = parseInt(document.getElementById('dependents')?.value || '0');

    if (!familyName) { alert('Please enter a family name.'); return; }

    const btn = document.getElementById('btn-upload-submit');
    btn.disabled = true;
    btn.textContent = 'Submitting...';

    try {
        // Create application
        const app = await API.createApplication({
            familyName,
            grade,
            dependentCount: dependents,
            submissionDate: new Date().toISOString(),
            applicationNumber: Utils.generateAppNumber()
        });

        State.currentApplicationId = app.applicationId;

        // Upload documents
        for (const [docType, file] of Object.entries(State.uploadedFiles)) {
            await API.uploadDocument(app.applicationId, docType, file);
        }

        // Start AI review
        await API.startReview(app.applicationId);

        // Go to processing screen
        showScreen('processing');
        startProcessingPolling(app.applicationId);

    } catch (err) {
        alert(`Error: ${err.message}`);
        btn.disabled = false;
        btn.textContent = 'Submit Application';
    }
}

// ===== PROCESSING SCREEN =====
function startProcessingPolling(applicationId) {
    if (State.pollingInterval) clearInterval(State.pollingInterval);
    updateProcessingStatus('Starting AI review...');

    State.pollingInterval = setInterval(async () => {
        try {
            const review = await API.getReview(applicationId);
            updateProcessingStatus(review.reviewStatus);

            if (review.reviewStatus === 'Completed' || review.reviewStatus === 'Failed') {
                clearInterval(State.pollingInterval);
                setTimeout(() => {
                    if (review.reviewStatus === 'Completed') {
                        loadApplicationDetail(applicationId);
                        showScreen('detail');
                    } else {
                        alert('AI review failed. Please try again.');
                        showScreen('upload');
                    }
                }, 1500);
            }
        } catch (err) {
            console.error('Polling error:', err);
        }
    }, 2000);
}

function updateProcessingStatus(status) {
    const el = document.getElementById('processing-status');
    if (el) el.textContent = `Status: ${status}`;
    const steps = {
        'InProgress': 1,
        'Completed': 3,
        'Failed': 0
    };
    const step = steps[status] ?? 0;
    document.querySelectorAll('.step').forEach((s, i) => {
        s.className = 'step ' + (i < step ? 'completed' : i === step ? 'active' : '');
    });
}

// ===== DASHBOARD SCREEN =====
async function loadDashboard() {
    try {
        const [metrics, queueData] = await Promise.all([
            API.getMetrics(),
            API.getQueueSummary()
        ]);
        renderMetrics(metrics);
        renderQueue(queueData.recentItems || []);
    } catch (err) {
        Utils.showError('dashboard-content', `Failed to load dashboard: ${err.message}`);
    }
}

function renderMetrics(metrics) {
    const el = document.getElementById('metrics-cards');
    if (!el) return;
    el.innerHTML = `
        <div class="metric-card">
            <div class="metric-value">${metrics.totalApplications}</div>
            <div class="metric-label">Total Applications</div>
        </div>
        <div class="metric-card pending">
            <div class="metric-value">${metrics.pendingReviews}</div>
            <div class="metric-label">Pending Review</div>
        </div>
        <div class="metric-card verified">
            <div class="metric-value">${metrics.verifiedApplications}</div>
            <div class="metric-label">Verified</div>
        </div>
        <div class="metric-card docs">
            <div class="metric-value">${metrics.docsRequiredApplications}</div>
            <div class="metric-label">Docs Required</div>
        </div>
        <div class="metric-card completed">
            <div class="metric-value">${metrics.completedApplications}</div>
            <div class="metric-label">Completed</div>
        </div>
    `;
}

function renderQueue(items) {
    const el = document.getElementById('queue-table-body');
    if (!el) return;
    if (!items || items.length === 0) {
        el.innerHTML = '<tr><td colspan="6" class="text-center">No items in queue</td></tr>';
        return;
    }
    el.innerHTML = items.map(item => `
        <tr>
            <td>${item.applicationNumber}</td>
            <td>${item.familyName}</td>
            <td>${Utils.statusBadge(item.queueStatus)}</td>
            <td>${item.assignedTo || '—'}</td>
            <td>${item.daysInQueue} days</td>
            <td>
                <button class="btn btn-sm btn-primary" onclick="viewApplication('${item.applicationId}')">
                    Review
                </button>
            </td>
        </tr>
    `).join('');
}

async function viewApplication(applicationId) {
    State.currentApplicationId = applicationId;
    await loadApplicationDetail(applicationId);
    showScreen('detail');
}

// ===== APPLICATION DETAIL SCREEN =====
async function loadApplicationDetail(applicationId) {
    try {
        const [app, review, financialData] = await Promise.all([
            API.getApplication(applicationId),
            API.getReview(applicationId).catch(() => null),
            API.getFinancialData(applicationId)
        ]);

        renderApplicationInfo(app);
        renderFinancialComparison(financialData);
        if (review) renderFlags(review.flags || []);
        renderActionButtons(applicationId, review);
    } catch (err) {
        Utils.showError('detail-content', `Failed to load application: ${err.message}`);
    }
}

function renderApplicationInfo(app) {
    const el = document.getElementById('app-info');
    if (!el) return;
    el.innerHTML = `
        <div class="info-grid">
            <div class="info-item"><label>Application #</label><span>${app.applicationNumber}</span></div>
            <div class="info-item"><label>Family Name</label><span>${app.familyName}</span></div>
            <div class="info-item"><label>Grade</label><span>${app.grade}</span></div>
            <div class="info-item"><label>Dependents</label><span>${app.dependentCount}</span></div>
            <div class="info-item"><label>Status</label><span>${Utils.statusBadge(app.status)}</span></div>
            <div class="info-item"><label>Submission Date</label><span>${Utils.formatDate(app.submissionDate)}</span></div>
        </div>
    `;
}

function renderFinancialComparison(financialData) {
    const el = document.getElementById('financial-comparison');
    if (!el) return;
    if (!financialData || financialData.length === 0) {
        el.innerHTML = '<p class="text-muted">No financial data available.</p>';
        return;
    }

    const sources = ['ApplicationForm', 'Form1040', 'IRSTranscript'];
    const dataBySource = {};
    financialData.forEach(fd => { dataBySource[fd.dataSource] = fd; });

    const rows = [
        { label: 'Household Income', key: 'householdIncome', format: Utils.formatCurrency },
        { label: 'AGI', key: 'agi', format: Utils.formatCurrency },
        { label: 'Filing Status', key: 'filingStatus', format: v => v || 'N/A' },
        { label: 'Dependents', key: 'dependentCount', format: v => v ?? 'N/A' },
        { label: 'Other Income', key: 'otherIncome', format: Utils.formatCurrency }
    ];

    el.innerHTML = `
        <table class="comparison-table">
            <thead>
                <tr>
                    <th>Field</th>
                    ${sources.map(s => `<th>${s}</th>`).join('')}
                </tr>
            </thead>
            <tbody>
                ${rows.map(row => `
                    <tr>
                        <td><strong>${row.label}</strong></td>
                        ${sources.map(src => {
                            const fd = dataBySource[src];
                            const val = fd ? fd[row.key] : null;
                            return `<td>${row.format(val)}</td>`;
                        }).join('')}
                    </tr>
                `).join('')}
            </tbody>
        </table>
    `;
}

function renderFlags(flags) {
    const el = document.getElementById('flags-list');
    if (!el) return;
    if (!flags || flags.length === 0) {
        el.innerHTML = '<div class="success-message">✓ No issues identified</div>';
        return;
    }
    el.innerHTML = flags.map(flag => `
        <div class="flag-item flag-${flag.severity.toLowerCase()}">
            <div class="flag-header">
                ${Utils.severityBadge(flag.severity)}
                <strong>${flag.title}</strong>
            </div>
            <p>${flag.description}</p>
            ${flag.discrepancyAmount ? `<small>Discrepancy: ${Utils.formatCurrency(flag.discrepancyAmount)}</small>` : ''}
        </div>
    `).join('');
}

function renderActionButtons(applicationId, review) {
    const el = document.getElementById('action-buttons');
    if (!el) return;
    el.innerHTML = `
        <button class="btn btn-primary" onclick="viewEmailDraft('${applicationId}')">
            📧 View Email Draft
        </button>
        <button class="btn btn-success" onclick="approveApplication('${applicationId}')">
            ✓ Approve &amp; Verify
        </button>
        <button class="btn btn-secondary" onclick="showScreen('dashboard')">
            ← Back to Dashboard
        </button>
    `;
}

// ===== EMAIL DRAFT SCREEN =====
async function viewEmailDraft(applicationId) {
    State.currentApplicationId = applicationId;
    showScreen('email');
    try {
        const draft = await API.getEmailDraft(applicationId);
        const el = document.getElementById('email-draft-content');
        if (el) el.value = typeof draft === 'string' ? draft : JSON.stringify(draft);
    } catch (err) {
        Utils.showError('email-content', `Failed to load email draft: ${err.message}`);
    }
}

async function sendEmail() {
    const applicationId = State.currentApplicationId;
    if (!applicationId) return;

    const recipientEmail = document.getElementById('recipient-email')?.value?.trim();
    if (!recipientEmail) { alert('Please enter a recipient email address.'); return; }

    const customMessage = document.getElementById('email-draft-content')?.value;

    try {
        await API.sendEmail(applicationId, { recipientEmail, customMessage });
        alert('Email sent successfully!');
        showScreen('dashboard');
    } catch (err) {
        alert(`Failed to send email: ${err.message}`);
    }
}

async function approveApplication(applicationId) {
    if (!confirm('Approve and verify this application?')) return;
    try {
        await API.approveReview(applicationId, { reviewedBy: 'Admin' });
        alert('Application approved and verified!');
        showScreen('dashboard');
    } catch (err) {
        alert(`Failed to approve: ${err.message}`);
    }
}

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
    initUploadScreen();
    showScreen('upload');

    // Navigation clicks
    document.querySelectorAll('.nav-link').forEach(link => {
        link.addEventListener('click', e => {
            e.preventDefault();
            showScreen(link.dataset.screen);
        });
    });
});
