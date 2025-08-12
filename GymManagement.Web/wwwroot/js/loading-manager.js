/**
 * ✅ LOADING MANAGER - Quản lý trạng thái loading cho tất cả AJAX calls
 * Tạo bởi: System Enhancement
 * Mục đích: Cung cấp loading states nhất quán cho toàn bộ ứng dụng
 */

class LoadingManager {
    constructor() {
        this.activeRequests = new Set();
        this.loadingOverlay = null;
        this.init();
    }

    init() {
        this.createLoadingOverlay();
        this.setupGlobalAjaxHandlers();
    }

    /**
     * Tạo loading overlay toàn cục
     */
    createLoadingOverlay() {
        if (document.getElementById('globalLoadingOverlay')) return;

        const overlay = document.createElement('div');
        overlay.id = 'globalLoadingOverlay';
        overlay.className = 'fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 hidden';
        overlay.innerHTML = `
            <div class="bg-white rounded-lg p-6 shadow-xl">
                <div class="flex items-center space-x-3">
                    <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                    <span class="text-gray-700 font-medium">Đang tải dữ liệu...</span>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        this.loadingOverlay = overlay;
    }

    /**
     * Setup global AJAX handlers
     */
    setupGlobalAjaxHandlers() {
        // jQuery AJAX handlers
        if (typeof $ !== 'undefined') {
            $(document).ajaxStart(() => this.showGlobalLoading());
            $(document).ajaxStop(() => this.hideGlobalLoading());
        }

        // Fetch API interceptor
        this.interceptFetch();
    }

    /**
     * Intercept Fetch API calls
     */
    interceptFetch() {
        const originalFetch = window.fetch;
        window.fetch = (...args) => {
            const requestId = this.generateRequestId();
            this.startRequest(requestId);
            
            return originalFetch(...args)
                .finally(() => {
                    this.endRequest(requestId);
                });
        };
    }

    /**
     * Hiển thị loading cho element cụ thể
     */
    showElementLoading(elementId, message = 'Đang tải...') {
        const element = document.getElementById(elementId);
        if (!element) return;

        // Tạo loading overlay cho element
        const loadingDiv = document.createElement('div');
        loadingDiv.className = 'absolute inset-0 bg-white bg-opacity-90 flex items-center justify-center z-10';
        loadingDiv.innerHTML = `
            <div class="flex items-center space-x-2">
                <div class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
                <span class="text-gray-600 text-sm">${message}</span>
            </div>
        `;
        loadingDiv.setAttribute('data-loading-overlay', 'true');

        // Đảm bảo element có position relative
        if (getComputedStyle(element).position === 'static') {
            element.style.position = 'relative';
        }

        element.appendChild(loadingDiv);
    }

    /**
     * Ẩn loading cho element cụ thể
     */
    hideElementLoading(elementId) {
        const element = document.getElementById(elementId);
        if (!element) return;

        const loadingOverlay = element.querySelector('[data-loading-overlay="true"]');
        if (loadingOverlay) {
            loadingOverlay.remove();
        }
    }

    /**
     * Hiển thị loading cho button
     */
    showButtonLoading(buttonId, loadingText = 'Đang xử lý...') {
        const button = document.getElementById(buttonId);
        if (!button) return;

        // Lưu trạng thái ban đầu
        button.setAttribute('data-original-text', button.innerHTML);
        button.setAttribute('data-original-disabled', button.disabled);
        
        // Cập nhật button
        button.disabled = true;
        button.innerHTML = `
            <div class="flex items-center justify-center space-x-2">
                <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                <span>${loadingText}</span>
            </div>
        `;
    }

    /**
     * Ẩn loading cho button
     */
    hideButtonLoading(buttonId) {
        const button = document.getElementById(buttonId);
        if (!button) return;

        const originalText = button.getAttribute('data-original-text');
        const originalDisabled = button.getAttribute('data-original-disabled') === 'true';

        if (originalText) {
            button.innerHTML = originalText;
            button.disabled = originalDisabled;
            button.removeAttribute('data-original-text');
            button.removeAttribute('data-original-disabled');
        }
    }

    /**
     * Hiển thị loading toàn cục
     */
    showGlobalLoading() {
        if (this.loadingOverlay) {
            this.loadingOverlay.classList.remove('hidden');
        }
    }

    /**
     * Ẩn loading toàn cục
     */
    hideGlobalLoading() {
        if (this.loadingOverlay && this.activeRequests.size === 0) {
            this.loadingOverlay.classList.add('hidden');
        }
    }

    /**
     * Bắt đầu request
     */
    startRequest(requestId) {
        this.activeRequests.add(requestId);
        this.showGlobalLoading();
    }

    /**
     * Kết thúc request
     */
    endRequest(requestId) {
        this.activeRequests.delete(requestId);
        if (this.activeRequests.size === 0) {
            this.hideGlobalLoading();
        }
    }

    /**
     * Tạo request ID duy nhất
     */
    generateRequestId() {
        return Date.now() + Math.random().toString(36).substr(2, 9);
    }

    /**
     * Hiển thị loading cho table
     */
    showTableLoading(tableId, message = 'Đang tải dữ liệu...') {
        const table = document.getElementById(tableId);
        if (!table) return;

        const tbody = table.querySelector('tbody');
        if (tbody) {
            const colCount = table.querySelector('thead tr')?.children.length || 1;
            tbody.innerHTML = `
                <tr>
                    <td colspan="${colCount}" class="text-center py-8">
                        <div class="flex items-center justify-center space-x-2">
                            <div class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
                            <span class="text-gray-600">${message}</span>
                        </div>
                    </td>
                </tr>
            `;
        }
    }

    /**
     * Hiển thị loading cho chart
     */
    showChartLoading(chartContainerId, message = 'Đang tải biểu đồ...') {
        const container = document.getElementById(chartContainerId);
        if (!container) return;

        container.innerHTML = `
            <div class="flex items-center justify-center h-full">
                <div class="text-center">
                    <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-3"></div>
                    <p class="text-gray-600">${message}</p>
                </div>
            </div>
        `;
    }
}

// Khởi tạo Loading Manager toàn cục
window.loadingManager = new LoadingManager();

// Export cho module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = LoadingManager;
}
