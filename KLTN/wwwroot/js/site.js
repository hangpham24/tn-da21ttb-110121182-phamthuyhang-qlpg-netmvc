// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Image error handling
document.addEventListener('DOMContentLoaded', function () {
    // Get all images in the document
    const images = document.querySelectorAll('img');
    
    // Add event listener to each image
    images.forEach(img => {
        img.addEventListener('error', function() {
            // Check the image context and provide appropriate fallback
            if (this.src.includes('default-service') || this.src.includes('service') || this.src.includes('goi-tap') || this.src.includes('lop-hoc')) {
                this.src = 'https://flowbite.s3.amazonaws.com/blocks/marketing-ui/content/office-content-1.png';
            } else if (this.src.includes('default-trainer') || this.src.includes('trainer') || this.src.includes('huan-luyen-vien')) {
                this.src = 'https://flowbite.s3.amazonaws.com/blocks/marketing-ui/avatars/jese-leos.png';
            } else if (this.src.includes('default-news') || this.src.includes('news') || this.src.includes('tin-tuc')) {
                this.src = 'https://flowbite.s3.amazonaws.com/blocks/marketing-ui/blog/office-laptops.png';
            } else {
                // Default fallback for any other images
                this.src = 'https://flowbite.s3.amazonaws.com/blocks/marketing-ui/content/office-content-3.png';
            }
            
            // Add a class to adjust styling if needed
            this.classList.add('fallback-image');
        });
    });
});
