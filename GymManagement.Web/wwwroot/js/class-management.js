/**
 * Class Management JavaScript
 * Handles loading states, confirmations, and AJAX operations
 */

// Loading state management
const LoadingManager = {
    show: function(element, message = 'Đang xử lý...') {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (element) {
            element.disabled = true;
            const originalText = element.textContent;
            element.setAttribute('data-original-text', originalText);
            element.innerHTML = `
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                ${message}
            `;
        }
    },

    hide: function(element) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (element) {
            element.disabled = false;
            const originalText = element.getAttribute('data-original-text');
            if (originalText) {
                element.textContent = originalText;
                element.removeAttribute('data-original-text');
            }
        }
    }
};

// Confirmation dialog
const ConfirmDialog = {
    show: function(options) {
        const {
            title = 'Xác nhận',
            message = 'Bạn có chắc chắn muốn thực hiện hành động này?',
            confirmText = 'Xác nhận',
            cancelText = 'Hủy bỏ',
            onConfirm = () => {},
            onCancel = () => {},
            type = 'warning' // warning, danger, info
        } = options;

        // Create modal HTML
        const modalHtml = `
            <div id="confirmModal" class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50" role="dialog" aria-modal="true" aria-labelledby="modal-title">
                <div class="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                    <div class="mt-3 text-center">
                        <div class="mx-auto flex items-center justify-center h-12 w-12 rounded-full ${type === 'danger' ? 'bg-red-100' : type === 'info' ? 'bg-blue-100' : 'bg-yellow-100'} mb-4">
                            ${type === 'danger' ? 
                                '<svg class="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.996-.833-2.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" /></svg>' :
                                type === 'info' ?
                                '<svg class="h-6 w-6 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>' :
                                '<svg class="h-6 w-6 text-yellow-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.996-.833-2.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" /></svg>'
                            }
                        </div>
                        <h3 id="modal-title" class="text-lg leading-6 font-medium text-gray-900">${title}</h3>
                        <div class="mt-2 px-7 py-3">
                            <p class="text-sm text-gray-500">${message}</p>
                        </div>
                        <div class="items-center px-4 py-3">
                            <button id="confirmBtn" class="px-4 py-2 ${type === 'danger' ? 'bg-red-500 hover:bg-red-700' : type === 'info' ? 'bg-blue-500 hover:bg-blue-700' : 'bg-yellow-500 hover:bg-yellow-700'} text-white text-base font-medium rounded-md w-24 mr-2 focus:outline-none focus:ring-2 focus:ring-offset-2 ${type === 'danger' ? 'focus:ring-red-500' : type === 'info' ? 'focus:ring-blue-500' : 'focus:ring-yellow-500'}">${confirmText}</button>
                            <button id="cancelBtn" class="px-4 py-2 bg-gray-500 hover:bg-gray-700 text-white text-base font-medium rounded-md w-24 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500">${cancelText}</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Add to DOM
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        
        const modal = document.getElementById('confirmModal');
        const confirmBtn = document.getElementById('confirmBtn');
        const cancelBtn = document.getElementById('cancelBtn');

        // Event handlers
        confirmBtn.onclick = () => {
            modal.remove();
            onConfirm();
        };

        cancelBtn.onclick = () => {
            modal.remove();
            onCancel();
        };

        // Close on backdrop click
        modal.onclick = (e) => {
            if (e.target === modal) {
                modal.remove();
                onCancel();
            }
        };

        // Close on Escape key
        document.addEventListener('keydown', function escapeHandler(e) {
            if (e.key === 'Escape') {
                modal.remove();
                onCancel();
                document.removeEventListener('keydown', escapeHandler);
            }
        });

        // Focus on confirm button
        confirmBtn.focus();
    }
};

// Toast notifications
const Toast = {
    show: function(message, type = 'success', duration = 3000) {
        const toastHtml = `
            <div id="toast" class="fixed top-4 right-4 z-50 max-w-xs bg-white border border-gray-200 rounded-xl shadow-lg" role="alert">
                <div class="flex p-4">
                    <div class="flex-shrink-0">
                        ${type === 'success' ? 
                            '<svg class="h-4 w-4 text-green-500 mt-0.5" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.061L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/></svg>' :
                            type === 'error' ?
                            '<svg class="h-4 w-4 text-red-500 mt-0.5" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM5.354 4.646a.5.5 0 1 0-.708.708L7.293 8l-2.647 2.646a.5.5 0 0 0 .708.708L8 8.707l2.646 2.647a.5.5 0 0 0 .708-.708L8.707 8l2.647-2.646a.5.5 0 0 0-.708-.708L8 7.293 5.354 4.646z"/></svg>' :
                            '<svg class="h-4 w-4 text-blue-500 mt-0.5" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/></svg>'
                        }
                    </div>
                    <div class="ml-3">
                        <p class="text-sm text-gray-700">${message}</p>
                    </div>
                </div>
            </div>
        `;

        // Remove existing toast
        const existingToast = document.getElementById('toast');
        if (existingToast) {
            existingToast.remove();
        }

        // Add new toast
        document.body.insertAdjacentHTML('beforeend', toastHtml);
        
        // Auto remove after duration
        setTimeout(() => {
            const toast = document.getElementById('toast');
            if (toast) {
                toast.remove();
            }
        }, duration);
    }
};

// Class registration functions
function registerClass(classId, button) {
    ConfirmDialog.show({
        title: 'Xác nhận đăng ký',
        message: 'Bạn có chắc chắn muốn đăng ký lớp học này?',
        confirmText: 'Đăng ký',
        type: 'info',
        onConfirm: () => {
            LoadingManager.show(button, 'Đang đăng ký...');
            
            // Get CSRF token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            fetch('/Member/RegisterClass', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    classId: classId,
                    startDate: new Date().toISOString(),
                    endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString() // 30 days from now
                })
            })
            .then(response => response.json())
            .then(data => {
                LoadingManager.hide(button);
                
                if (data.success) {
                    Toast.show(data.message, 'success');
                    // Optionally refresh the page or update UI
                    setTimeout(() => location.reload(), 1500);
                } else {
                    Toast.show(data.message, 'error');
                }
            })
            .catch(error => {
                LoadingManager.hide(button);
                Toast.show('Có lỗi xảy ra khi đăng ký lớp học.', 'error');
                console.error('Error:', error);
            });
        }
    });
}

// Delete class function (for admin)
function deleteClass(classId, className) {
    ConfirmDialog.show({
        title: 'Xác nhận xóa',
        message: `Bạn có chắc chắn muốn xóa lớp học "${className}"? Hành động này không thể hoàn tác.`,
        confirmText: 'Xóa',
        type: 'danger',
        onConfirm: () => {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            fetch(`/LopHoc/Delete/${classId}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                }
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    Toast.show(data.message, 'success');
                    setTimeout(() => location.reload(), 1500);
                } else {
                    Toast.show(data.message, 'error');
                }
            })
            .catch(error => {
                Toast.show('Có lỗi xảy ra khi xóa lớp học.', 'error');
                console.error('Error:', error);
            });
        }
    });
}

// Export for global use
window.LoadingManager = LoadingManager;
window.ConfirmDialog = ConfirmDialog;
window.Toast = Toast;
window.registerClass = registerClass;
window.deleteClass = deleteClass;
