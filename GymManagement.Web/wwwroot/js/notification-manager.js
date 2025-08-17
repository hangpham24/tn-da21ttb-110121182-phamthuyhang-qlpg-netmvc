/**
 * ✅ NOTIFICATION MANAGER - Real-time notifications system
 * Tạo bởi: System Enhancement
 * Mục đích: Cung cấp hệ thống thông báo real-time cho toàn bộ ứng dụng
 */

class NotificationManager {
    constructor() {
        this.notifications = [];
        this.container = null;
        this.maxNotifications = 5;
        this.defaultDuration = 5000; // 5 seconds
        this.init();
    }

    init() {
        this.createContainer();
        this.setupEventListeners();
        this.requestNotificationPermission();
    }

    /**
     * Tạo container cho notifications
     */
    createContainer() {
        if (document.getElementById('notificationContainer')) return;

        const container = document.createElement('div');
        container.id = 'notificationContainer';
        container.className = 'fixed top-4 right-4 z-50 space-y-2';
        container.style.maxWidth = '400px';
        document.body.appendChild(container);
        this.container = container;
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        // Listen for custom notification events
        document.addEventListener('showNotification', (event) => {
            const { message, type, duration, actions } = event.detail;
            this.show(message, type, duration, actions);
        });

        // Listen for server-sent events (if implemented)
        this.setupServerSentEvents();
    }

    /**
     * Request browser notification permission
     */
    async requestNotificationPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            try {
                await Notification.requestPermission();
            } catch (error) {
                console.log('Notification permission request failed:', error);
            }
        }
    }

    /**
     * Setup Server-Sent Events for real-time notifications
     */
    setupServerSentEvents() {
        // Check if user is authenticated
        const isAuthenticated = document.querySelector('meta[name="user-authenticated"]')?.content === 'true';
        if (!isAuthenticated) return;

        try {
            const eventSource = new EventSource('/api/notifications/stream');
            
            eventSource.onmessage = (event) => {
                try {
                    const notification = JSON.parse(event.data);
                    this.show(notification.message, notification.type, notification.duration);
                    
                    // Show browser notification if permission granted
                    this.showBrowserNotification(notification.title || 'Thông báo', notification.message);
                } catch (error) {
                    console.error('Error parsing notification:', error);
                }
            };

            eventSource.onerror = (error) => {
                console.log('SSE connection error:', error);
                // Attempt to reconnect after 5 seconds
                setTimeout(() => {
                    if (eventSource.readyState === EventSource.CLOSED) {
                        this.setupServerSentEvents();
                    }
                }, 5000);
            };

            // Store reference for cleanup
            this.eventSource = eventSource;
        } catch (error) {
            console.log('SSE not supported or failed to connect:', error);
        }
    }

    /**
     * Show notification
     */
    show(message, type = 'info', duration = null, actions = []) {
        const notification = this.createNotification(message, type, duration, actions);
        this.addNotification(notification);
        return notification.id;
    }

    /**
     * Create notification element
     */
    createNotification(message, type, duration, actions) {
        const id = this.generateId();
        const notification = {
            id,
            message,
            type,
            duration: duration || this.defaultDuration,
            element: null,
            timeout: null
        };

        const element = document.createElement('div');
        element.className = `notification-item transform transition-all duration-300 translate-x-full opacity-0 ${this.getTypeClasses(type)}`;
        element.setAttribute('data-notification-id', id);
        
        element.innerHTML = `
            <div class="flex items-start space-x-3 p-4 rounded-lg shadow-lg">
                <div class="flex-shrink-0">
                    ${this.getTypeIcon(type)}
                </div>
                <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium">${message}</p>
                    ${actions.length > 0 ? this.createActionButtons(actions, id) : ''}
                </div>
                <button onclick="notificationManager.dismiss('${id}')" class="flex-shrink-0 ml-2 text-current opacity-70 hover:opacity-100">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                </button>
            </div>
        `;

        notification.element = element;
        return notification;
    }

    /**
     * Get CSS classes for notification type
     */
    getTypeClasses(type) {
        const classes = {
            success: 'bg-green-500 text-white',
            error: 'bg-red-500 text-white',
            warning: 'bg-yellow-500 text-white',
            info: 'bg-blue-500 text-white',
            revenue: 'bg-gradient-to-r from-green-500 to-emerald-500 text-white',
            payment: 'bg-gradient-to-r from-blue-500 to-indigo-500 text-white'
        };
        return classes[type] || classes.info;
    }

    /**
     * Get icon for notification type
     */
    getTypeIcon(type) {
        const icons = {
            success: '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>',
            error: '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>',
            warning: '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"></path></svg>',
            info: '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>',
            revenue: '💰',
            payment: '💳'
        };
        return icons[type] || icons.info;
    }

    /**
     * Create action buttons
     */
    createActionButtons(actions, notificationId) {
        return `
            <div class="mt-2 flex space-x-2">
                ${actions.map(action => `
                    <button onclick="notificationManager.handleAction('${notificationId}', '${action.id}')" 
                            class="px-3 py-1 text-xs bg-white bg-opacity-20 rounded hover:bg-opacity-30 transition-colors">
                        ${action.label}
                    </button>
                `).join('')}
            </div>
        `;
    }

    /**
     * Add notification to container
     */
    addNotification(notification) {
        // Remove oldest notification if at max capacity
        if (this.notifications.length >= this.maxNotifications) {
            const oldest = this.notifications.shift();
            this.dismiss(oldest.id);
        }

        this.notifications.push(notification);
        this.container.appendChild(notification.element);

        // Animate in
        requestAnimationFrame(() => {
            notification.element.classList.remove('translate-x-full', 'opacity-0');
            notification.element.classList.add('translate-x-0', 'opacity-100');
        });

        // Auto dismiss
        if (notification.duration > 0) {
            notification.timeout = setTimeout(() => {
                this.dismiss(notification.id);
            }, notification.duration);
        }
    }

    /**
     * Dismiss notification
     */
    dismiss(id) {
        const notification = this.notifications.find(n => n.id === id);
        if (!notification) return;

        // Clear timeout
        if (notification.timeout) {
            clearTimeout(notification.timeout);
        }

        // Animate out
        notification.element.classList.add('translate-x-full', 'opacity-0');
        
        setTimeout(() => {
            if (notification.element.parentNode) {
                notification.element.parentNode.removeChild(notification.element);
            }
            this.notifications = this.notifications.filter(n => n.id !== id);
        }, 300);
    }

    /**
     * Handle action button clicks
     */
    handleAction(notificationId, actionId) {
        const event = new CustomEvent('notificationAction', {
            detail: { notificationId, actionId }
        });
        document.dispatchEvent(event);
        this.dismiss(notificationId);
    }

    /**
     * Show browser notification
     */
    showBrowserNotification(title, message) {
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, {
                body: message,
                icon: '/favicon.ico',
                badge: '/favicon.ico'
            });
        }
    }

    /**
     * Generate unique ID
     */
    generateId() {
        return Date.now().toString(36) + Math.random().toString(36).substr(2);
    }

    /**
     * Show revenue notification
     */
    showRevenue(amount, message = null) {
        const formattedAmount = new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
        
        const msg = message || `Doanh thu mới: ${formattedAmount}`;
        return this.show(msg, 'revenue', 8000);
    }

    /**
     * Show payment notification
     */
    showPayment(method, amount, message = null) {
        const formattedAmount = new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
        
        const msg = message || `Thanh toán ${method}: ${formattedAmount}`;
        return this.show(msg, 'payment', 6000);
    }

    /**
     * Cleanup
     */
    destroy() {
        if (this.eventSource) {
            this.eventSource.close();
        }
        
        this.notifications.forEach(notification => {
            if (notification.timeout) {
                clearTimeout(notification.timeout);
            }
        });
        
        if (this.container) {
            this.container.remove();
        }
    }
}

// Initialize global notification manager
window.notificationManager = new NotificationManager();

// Backward compatibility
window.showNotification = (message, type, duration) => {
    return window.notificationManager.show(message, type, duration);
};

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NotificationManager;
}
