/**
 * Global Face-API Models Manager
 * Load models once and reuse across all pages
 */
window.FaceAPIGlobal = {
    isLoaded: false,
    isLoading: false,
    loadPromise: null,
    
    /**
     * Load Face-API models globally
     * @returns {Promise} Promise that resolves when models are loaded
     */
    async loadModels() {
        if (this.isLoaded) {
            console.log('✅ Face-API models already loaded globally');
            return Promise.resolve();
        }
        
        if (this.isLoading) {
            console.log('⏳ Face-API models are loading globally, waiting...');
            return this.loadPromise;
        }
        
        this.isLoading = true;
        console.log('🚀 Starting global Face-API models loading...');
        
        this.loadPromise = this._loadModelsInternal();
        
        try {
            await this.loadPromise;
            this.isLoaded = true;
            this.isLoading = false;
            console.log('✅ Global Face-API models loaded successfully');
            
            // Dispatch event to notify other scripts
            window.dispatchEvent(new CustomEvent('faceapi-models-loaded'));
        } catch (error) {
            this.isLoading = false;
            console.error('❌ Failed to load global Face-API models:', error);
            
            // Dispatch error event
            window.dispatchEvent(new CustomEvent('faceapi-models-error', { detail: error }));
            throw error;
        }
        
        return this.loadPromise;
    },
    
    /**
     * Internal method to load models
     * @private
     */
    async _loadModelsInternal() {
        const baseUrl = window.location.origin;
        const modelsPath = `${baseUrl}/FaceTest/models`;
        
        console.log('📦 Loading Face-API models from:', modelsPath);
        
        // Load models sequentially for better reliability
        console.log('Loading TinyFaceDetector...');
        await faceapi.nets.tinyFaceDetector.loadFromUri(modelsPath);
        console.log('✅ TinyFaceDetector loaded');

        console.log('Loading FaceLandmark68Net...');
        await faceapi.nets.faceLandmark68Net.loadFromUri(modelsPath);
        console.log('✅ FaceLandmark68Net loaded');

        console.log('Loading FaceRecognitionNet...');
        await faceapi.nets.faceRecognitionNet.loadFromUri(modelsPath);
        console.log('✅ FaceRecognitionNet loaded');

        // Verify all models are loaded
        if (!faceapi.nets.tinyFaceDetector.isLoaded) {
            throw new Error('TinyFaceDetector failed to load');
        }
        if (!faceapi.nets.faceLandmark68Net.isLoaded) {
            throw new Error('FaceLandmark68Net failed to load');
        }
        if (!faceapi.nets.faceRecognitionNet.isLoaded) {
            throw new Error('FaceRecognitionNet failed to load');
        }
        
        console.log('🎯 All Face-API models verified successfully');
    },
    
    /**
     * Check if models are ready to use
     * @returns {boolean} True if models are loaded and ready
     */
    isReady() {
        return this.isLoaded && 
               faceapi.nets.tinyFaceDetector.isLoaded &&
               faceapi.nets.faceLandmark68Net.isLoaded &&
               faceapi.nets.faceRecognitionNet.isLoaded;
    },
    
    /**
     * Wait for models to be ready
     * @param {number} timeout Timeout in milliseconds (default: 30000)
     * @returns {Promise} Promise that resolves when models are ready
     */
    async waitForModels(timeout = 30000) {
        if (this.isReady()) {
            return Promise.resolve();
        }
        
        return new Promise((resolve, reject) => {
            const timeoutId = setTimeout(() => {
                reject(new Error('Timeout waiting for Face-API models to load'));
            }, timeout);
            
            const checkReady = () => {
                if (this.isReady()) {
                    clearTimeout(timeoutId);
                    resolve();
                } else {
                    setTimeout(checkReady, 100);
                }
            };
            
            // Start loading if not already loading
            if (!this.isLoading && !this.isLoaded) {
                this.loadModels().catch(reject);
            }
            
            checkReady();
        });
    }
};

// Auto-load models when Face-API.js is available
(function() {
    function initializeGlobalModels() {
        if (typeof faceapi !== 'undefined') {
            console.log('🚀 Face-API.js detected, auto-loading models on app startup...');
            window.FaceAPIGlobal.loadModels().then(() => {
                console.log('🎉 Global Face-API models ready for entire application!');

                // Store in sessionStorage to persist across page navigation
                sessionStorage.setItem('faceapi-models-loaded', 'true');
                sessionStorage.setItem('faceapi-models-loaded-time', Date.now().toString());

                // Show global notification only once per session
                const hasShownNotification = sessionStorage.getItem('faceapi-notification-shown');
                if (!hasShownNotification && typeof showNotification === 'function') {
                    showNotification('success', 'Face Recognition models loaded successfully!');
                    sessionStorage.setItem('faceapi-notification-shown', 'true');
                }
            }).catch(error => {
                console.error('❌ Failed to auto-load Face-API models:', error);

                // Show global error notification only once per session
                const hasShownErrorNotification = sessionStorage.getItem('faceapi-error-notification-shown');
                if (!hasShownErrorNotification && typeof showNotification === 'function') {
                    showNotification('error', 'Failed to load Face Recognition models');
                    sessionStorage.setItem('faceapi-error-notification-shown', 'true');
                }
            });
        } else {
            console.log('⏳ Waiting for Face-API.js to load...');
            setTimeout(initializeGlobalModels, 500);
        }
    }
    
    // Start initialization when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeGlobalModels);
    } else {
        initializeGlobalModels();
    }
})();
