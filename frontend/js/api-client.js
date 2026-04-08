const API = {
    baseUrl: 'http://localhost:5000',

    getHeaders(isFormData = false) {
        const headers = {};
        if (!isFormData) headers['Content-Type'] = 'application/json';
        const token = localStorage.getItem('faid_token');
        if (token) headers['Authorization'] = `Bearer ${token}`;
        return headers;
    },

    async get(path) {
        const response = await fetch(`${this.baseUrl}${path}`, {
            method: 'GET',
            headers: this.getHeaders()
        });
        if (!response.ok) throw new Error(`GET ${path} failed: ${response.status}`);
        return response.json();
    },

    async post(path, body) {
        const response = await fetch(`${this.baseUrl}${path}`, {
            method: 'POST',
            headers: this.getHeaders(),
            body: JSON.stringify(body)
        });
        if (!response.ok) {
            const err = await response.text();
            throw new Error(`POST ${path} failed: ${response.status} - ${err}`);
        }
        return response.json();
    },

    async put(path, body) {
        const response = await fetch(`${this.baseUrl}${path}`, {
            method: 'PUT',
            headers: this.getHeaders(),
            body: JSON.stringify(body)
        });
        if (!response.ok) throw new Error(`PUT ${path} failed: ${response.status}`);
        return response.json();
    },

    async delete(path) {
        const response = await fetch(`${this.baseUrl}${path}`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });
        if (!response.ok) throw new Error(`DELETE ${path} failed: ${response.status}`);
        return response.status !== 204 ? response.json() : null;
    },

    async uploadFile(path, formData) {
        const response = await fetch(`${this.baseUrl}${path}`, {
            method: 'POST',
            headers: this.getHeaders(true),
            body: formData
        });
        if (!response.ok) {
            const err = await response.text();
            throw new Error(`Upload ${path} failed: ${response.status} - ${err}`);
        }
        return response.json();
    },

    // Application endpoints
    getApplications: (page = 1, pageSize = 20) => API.get(`/api/applications?page=${page}&pageSize=${pageSize}`),
    getApplication: (id) => API.get(`/api/applications/${id}`),
    createApplication: (dto) => API.post('/api/applications', dto),
    updateApplication: (id, dto) => API.put(`/api/applications/${id}`, dto),
    getQueue: () => API.get('/api/applications/queue'),

    // Document endpoints
    uploadDocument: (applicationId, documentType, file) => {
        const fd = new FormData();
        fd.append('applicationId', applicationId);
        fd.append('documentType', documentType);
        fd.append('file', file);
        return API.uploadFile('/api/documents/upload', fd);
    },
    getDocuments: (applicationId) => API.get(`/api/documents/${applicationId}`),
    deleteDocument: (id) => API.delete(`/api/documents/${id}`),

    // AI Review endpoints
    startReview: (applicationId) => API.post(`/api/ai-review/${applicationId}`, {}),
    getReview: (applicationId) => API.get(`/api/ai-review/${applicationId}`),
    getFlags: (applicationId) => API.get(`/api/ai-review/${applicationId}/flags`),
    getEmailDraft: (applicationId) => API.get(`/api/ai-review/${applicationId}/email-draft`),
    sendEmail: (applicationId, dto) => API.post(`/api/ai-review/${applicationId}/send-email`, dto),
    approveReview: (applicationId, dto) => API.put(`/api/ai-review/${applicationId}/approve`, dto),

    // Financial data endpoints
    getFinancialData: (applicationId) => API.get(`/api/financial-data/${applicationId}`),
    createFinancialData: (dto) => API.post('/api/financial-data', dto),

    // Dashboard endpoints
    getMetrics: () => API.get('/api/dashboard/metrics'),
    getQueueSummary: () => API.get('/api/dashboard/queue-summary'),

    // Auth endpoints
    login: (dto) => API.post('/api/auth/login', dto),
    getCurrentUser: () => API.get('/api/auth/current-user')
};
