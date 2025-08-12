/**
 * Real-time Updates for Class Capacity
 * Uses polling to update class information in real-time
 */

class RealTimeUpdater {
    constructor(options = {}) {
        this.updateInterval = options.updateInterval || 30000; // 30 seconds
        this.endpoints = {
            classCapacity: '/api/classes/capacity',
            activeClasses: '/api/classes/active'
        };
        this.isRunning = false;
        this.intervalId = null;
    }

    start() {
        if (this.isRunning) return;
        
        this.isRunning = true;
        this.updateClassCapacities();
        
        this.intervalId = setInterval(() => {
            this.updateClassCapacities();
        }, this.updateInterval);
        
        console.log('Real-time updates started');
    }

    stop() {
        if (!this.isRunning) return;
        
        this.isRunning = false;
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
        
        console.log('Real-time updates stopped');
    }

    async updateClassCapacities() {
        try {
            // Get all class cards on the page
            const classCards = document.querySelectorAll('[data-class-id]');
            if (classCards.length === 0) return;

            // Extract class IDs
            const classIds = Array.from(classCards).map(card => 
                parseInt(card.getAttribute('data-class-id'))
            ).filter(id => !isNaN(id));

            if (classIds.length === 0) return;

            // Fetch updated capacity data
            const response = await fetch('/LopHoc/CheckCapacities', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getCSRFToken()
                },
                body: JSON.stringify({ classIds })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const capacityData = await response.json();
            
            // Update UI with new data
            this.updateClassCards(capacityData);
            
        } catch (error) {
            console.error('Error updating class capacities:', error);
        }
    }

    updateClassCards(capacityData) {
        capacityData.forEach(classData => {
            const classCard = document.querySelector(`[data-class-id="${classData.classId}"]`);
            if (!classCard) return;

            // Update registered count
            const registeredElement = classCard.querySelector('.registered-count');
            if (registeredElement) {
                registeredElement.textContent = `${classData.registeredCount}/${classData.capacity}`;
            }

            // Update available slots
            const availableElement = classCard.querySelector('.available-slots');
            if (availableElement) {
                availableElement.textContent = classData.availableSlots;
            }

            // Update progress bar
            const progressBar = classCard.querySelector('.progress-bar');
            if (progressBar) {
                const fillRate = classData.capacity > 0 ? 
                    (classData.registeredCount / classData.capacity * 100) : 0;
                progressBar.style.width = `${fillRate}%`;
                
                // Update aria attributes
                const progressContainer = progressBar.parentElement;
                if (progressContainer) {
                    progressContainer.setAttribute('aria-valuenow', Math.round(fillRate));
                }
            }

            // Update status badge
            const statusBadge = classCard.querySelector('.status-badge');
            if (statusBadge) {
                const isAvailable = classData.availableSlots > 0;
                const statusText = isAvailable ? 'Còn chỗ' : 'Đã đầy';
                const statusClass = isAvailable ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800';
                
                statusBadge.textContent = statusText;
                statusBadge.className = `inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${statusClass} status-badge`;
            }

            // Update register button state
            const registerButton = classCard.querySelector('.register-btn');
            if (registerButton) {
                const isAvailable = classData.availableSlots > 0;
                registerButton.disabled = !isAvailable;
                
                if (isAvailable) {
                    registerButton.className = registerButton.className.replace(/bg-gray-\d+/, 'bg-blue-600');
                    registerButton.className = registerButton.className.replace(/hover:bg-gray-\d+/, 'hover:bg-blue-700');
                    registerButton.textContent = 'Đăng ký ngay';
                } else {
                    registerButton.className = registerButton.className.replace(/bg-blue-\d+/, 'bg-gray-400');
                    registerButton.className = registerButton.className.replace(/hover:bg-blue-\d+/, 'hover:bg-gray-400');
                    registerButton.textContent = 'Đã đầy';
                }
            }

            // Add visual indicator for recent updates
            this.addUpdateIndicator(classCard);
        });
    }

    addUpdateIndicator(element) {
        // Add a subtle pulse animation to indicate update
        element.classList.add('animate-pulse');
        setTimeout(() => {
            element.classList.remove('animate-pulse');
        }, 1000);

        // Add a small indicator dot
        let indicator = element.querySelector('.update-indicator');
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.className = 'update-indicator absolute top-2 right-2 w-2 h-2 bg-green-400 rounded-full animate-ping';
            element.style.position = 'relative';
            element.appendChild(indicator);
        }

        // Remove indicator after animation
        setTimeout(() => {
            if (indicator && indicator.parentNode) {
                indicator.remove();
            }
        }, 2000);
    }

    getCSRFToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }

    // Visibility API to pause updates when tab is not active
    handleVisibilityChange() {
        if (document.hidden) {
            this.stop();
        } else {
            this.start();
        }
    }

    init() {
        // Start updates when page loads
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.start());
        } else {
            this.start();
        }

        // Handle visibility changes
        document.addEventListener('visibilitychange', () => this.handleVisibilityChange());

        // Stop updates when page unloads
        window.addEventListener('beforeunload', () => this.stop());

        // Handle focus/blur events for additional optimization
        window.addEventListener('focus', () => {
            if (!this.isRunning) this.start();
        });

        window.addEventListener('blur', () => {
            // Optionally reduce update frequency when window loses focus
            // this.updateInterval = 60000; // 1 minute
        });
    }
}

// Initialize real-time updater
const realTimeUpdater = new RealTimeUpdater({
    updateInterval: 30000 // Update every 30 seconds
});

// Auto-initialize when script loads
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        // Only initialize on pages with class cards
        if (document.querySelector('[data-class-id]')) {
            realTimeUpdater.init();
        }
    });
} else {
    if (document.querySelector('[data-class-id]')) {
        realTimeUpdater.init();
    }
}

// Export for manual control
window.RealTimeUpdater = realTimeUpdater;
