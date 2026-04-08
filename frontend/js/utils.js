const Utils = {
    formatDate(dateString) {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric', month: 'short', day: 'numeric'
        });
    },

    formatCurrency(amount) {
        if (amount == null) return 'N/A';
        return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
    },

    statusBadge(status) {
        const classes = {
            'Pending': 'badge-pending',
            'Processing': 'badge-processing',
            'Verified': 'badge-verified',
            'DocsRequired': 'badge-docs',
            'Completed': 'badge-completed',
            'InProgress': 'badge-processing',
            'Failed': 'badge-failed',
            'OnHold': 'badge-docs'
        };
        return `<span class="badge ${classes[status] || 'badge-pending'}">${status}</span>`;
    },

    severityBadge(severity) {
        const classes = { 'Critical': 'badge-failed', 'Warning': 'badge-docs', 'Info': 'badge-processing' };
        return `<span class="badge ${classes[severity] || 'badge-processing'}">${severity}</span>`;
    },

    showLoading(elementId) {
        const el = document.getElementById(elementId);
        if (el) el.innerHTML = '<div class="loading"><div class="spinner"></div><p>Loading...</p></div>';
    },

    showError(elementId, message) {
        const el = document.getElementById(elementId);
        if (el) el.innerHTML = `<div class="error-message"><i class="icon-error">⚠</i> ${message}</div>`;
    },

    generateAppNumber() {
        const year = new Date().getFullYear();
        const rand = Math.floor(Math.random() * 9000) + 1000;
        return `FA-${year}-${rand}`;
    }
};
