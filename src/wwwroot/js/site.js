// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Global loading state management
window.GymApp = {
    isLoading: false,

    // Show loading indicator
    showLoading: function(button) {
        if (button) {
            button.disabled = true;
            const originalText = button.innerHTML;
            button.setAttribute('data-original-text', originalText);
            button.innerHTML = `
                <svg class="animate-spin -ml-1 mr-3 h-4 w-4 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Đang xử lý...
            `;
        }
        this.isLoading = true;
    },

    // Hide loading indicator
    hideLoading: function(button) {
        if (button) {
            button.disabled = false;
            const originalText = button.getAttribute('data-original-text');
            if (originalText) {
                button.innerHTML = originalText;
            }
        }
        this.isLoading = false;
    },

    // Show confirm dialog
    showConfirmDialog: function(options) {
        const {
            title = 'Xác nhận',
            message = 'Bạn có chắc chắn muốn thực hiện thao tác này?',
            itemName = '',
            confirmText = 'Xác nhận',
            cancelText = 'Hủy',
            onConfirm = () => {},
            onCancel = () => {},
            type = 'warning' // warning, danger, info
        } = options;

        const iconClass = type === 'danger' ? 'text-red-600' : type === 'info' ? 'text-blue-600' : 'text-yellow-600';
        const confirmButtonClass = type === 'danger' ? 'bg-red-600 hover:bg-red-700' : 'bg-blue-600 hover:bg-blue-700';

        const modalHtml = `
            <div id="confirmModal" class="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
                <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
                    <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true"></div>
                    <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
                    <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
                        <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
                            <div class="sm:flex sm:items-start">
                                <div class="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-red-100 sm:mx-0 sm:h-10 sm:w-10">
                                    <svg class="h-6 w-6 ${iconClass}" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                                    </svg>
                                </div>
                                <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
                                    <h3 class="text-lg leading-6 font-medium text-gray-900" id="modal-title">${title}</h3>
                                    <div class="mt-2">
                                        <p class="text-sm text-gray-500">${message}</p>
                                        ${itemName ? `<p class="text-sm font-medium text-gray-900 mt-2">"${itemName}"</p>` : ''}
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
                            <button type="button" id="confirmButton" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 ${confirmButtonClass} text-base font-medium text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 sm:ml-3 sm:w-auto sm:text-sm">
                                ${confirmText}
                            </button>
                            <button type="button" id="cancelButton" class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
                                ${cancelText}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal if any
        const existingModal = document.getElementById('confirmModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Add modal to body
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Add event listeners
        document.getElementById('confirmButton').addEventListener('click', function() {
            document.getElementById('confirmModal').remove();
            onConfirm();
        });

        document.getElementById('cancelButton').addEventListener('click', function() {
            document.getElementById('confirmModal').remove();
            onCancel();
        });

        // Close on backdrop click
        document.getElementById('confirmModal').addEventListener('click', function(e) {
            if (e.target === this) {
                this.remove();
                onCancel();
            }
        });

        // Close on Escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                const modal = document.getElementById('confirmModal');
                if (modal) {
                    modal.remove();
                    onCancel();
                }
            }
        });
    }
};

// Delete confirmation helper
function confirmDelete(options) {
    const {
        url,
        itemName = '',
        itemType = 'mục này',
        onSuccess = () => { location.reload(); },
        onError = (error) => { console.error('Delete error:', error); }
    } = options;

    GymApp.showConfirmDialog({
        title: 'Xác nhận xóa',
        message: `Bạn có chắc chắn muốn xóa ${itemType}? Thao tác này không thể hoàn tác.`,
        itemName: itemName,
        confirmText: 'Xóa',
        cancelText: 'Hủy',
        type: 'danger',
        onConfirm: function() {
            // Show loading
            const deleteButton = document.querySelector(`[onclick*="${url}"]`);
            GymApp.showLoading(deleteButton);

            // Perform delete
            const formData = new FormData();
            formData.append('id', url.split('=')[1] || url.split('/').pop());

            // Try to find antiforgery token from multiple sources
            let token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (!token) {
                // Try to find token in forms
                const forms = document.querySelectorAll('form');
                for (const form of forms) {
                    const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
                    if (tokenInput) {
                        token = tokenInput.value;
                        break;
                    }
                }
            }

            if (token) {
                formData.append('__RequestVerificationToken', token);
                console.log('Antiforgery token found and added to request');
            } else {
                console.error('Antiforgery token not found - delete request may fail');
                // Still try to send the request without token for debugging
            }

            fetch(url, {
                method: 'POST',
                body: formData
            })
            .then(response => {
                if (response.ok) {
                    return response.json();
                }
                throw new Error('Network response was not ok');
            })
            .then(data => {
                GymApp.hideLoading(deleteButton);
                if (data.success) {
                    // Show success message
                    showNotification('Xóa thành công!', 'success');
                    onSuccess(data);
                } else {
                    showNotification(data.message || 'Có lỗi xảy ra khi xóa.', 'error');
                    onError(data);
                }
            })
            .catch(error => {
                GymApp.hideLoading(deleteButton);
                showNotification('Có lỗi xảy ra khi xóa.', 'error');
                onError(error);
            });
        }
    });
}

// Notification system
function showNotification(message, type = 'info') {
    const typeClasses = {
        success: 'bg-green-500',
        error: 'bg-red-500',
        warning: 'bg-yellow-500',
        info: 'bg-blue-500'
    };

    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 z-50 ${typeClasses[type]} text-white px-6 py-3 rounded-lg shadow-lg transform transition-all duration-300 translate-x-full`;
    notification.textContent = message;

    document.body.appendChild(notification);

    // Animate in
    setTimeout(() => {
        notification.classList.remove('translate-x-full');
    }, 100);

    // Auto remove after 3 seconds
    setTimeout(() => {
        notification.classList.add('translate-x-full');
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, 3000);
}

// Form submission with loading state
function handleFormSubmission(form, submitButton) {
    if (GymApp.isLoading) return false;

    // Show loading state
    GymApp.showLoading(submitButton);

    // Disable all form inputs
    const inputs = form.querySelectorAll('input, select, textarea, button');
    inputs.forEach(input => {
        if (input !== submitButton) {
            input.disabled = true;
        }
    });

    return true;
}

// Initialize form loading handlers
document.addEventListener('DOMContentLoaded', function() {
    // Handle all forms with data-loading attribute
    const forms = document.querySelectorAll('form[data-loading="true"]');
    forms.forEach(form => {
        const submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
            form.addEventListener('submit', function(e) {
                if (!handleFormSubmission(form, submitButton)) {
                    e.preventDefault();
                }
            });
        }
    });

    // Handle AJAX forms
    const ajaxForms = document.querySelectorAll('form[data-ajax="true"]');
    ajaxForms.forEach(form => {
        const submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
            form.addEventListener('submit', function(e) {
                e.preventDefault();

                if (!handleFormSubmission(form, submitButton)) {
                    return;
                }

                const formData = new FormData(form);
                const url = form.action || window.location.href;
                const method = form.method || 'POST';

                fetch(url, {
                    method: method,
                    body: formData
                })
                .then(response => {
                    if (response.ok) {
                        return response.json();
                    }
                    throw new Error('Network response was not ok');
                })
                .then(data => {
                    GymApp.hideLoading(submitButton);

                    // Re-enable form inputs
                    const inputs = form.querySelectorAll('input, select, textarea, button');
                    inputs.forEach(input => input.disabled = false);

                    if (data.success) {
                        showNotification(data.message || 'Thao tác thành công!', 'success');

                        // Handle redirect
                        if (data.redirectUrl) {
                            setTimeout(() => {
                                window.location.href = data.redirectUrl;
                            }, 1000);
                        }
                    } else {
                        showNotification(data.message || 'Có lỗi xảy ra.', 'error');
                    }
                })
                .catch(error => {
                    GymApp.hideLoading(submitButton);

                    // Re-enable form inputs
                    const inputs = form.querySelectorAll('input, select, textarea, button');
                    inputs.forEach(input => input.disabled = false);

                    showNotification('Có lỗi xảy ra khi xử lý yêu cầu.', 'error');
                    console.error('Form submission error:', error);
                });
            });
        }
    });
});
