// wwwroot/app.js
document.addEventListener('DOMContentLoaded', function () {
    const elements = document.querySelectorAll('.fadeinup');
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate');
            }
        });
    });

    elements.forEach(element => {
        observer.observe(element);
    });
});
