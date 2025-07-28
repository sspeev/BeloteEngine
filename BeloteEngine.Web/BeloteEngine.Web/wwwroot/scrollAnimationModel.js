window.initScrollAnimations = function () {
    // Progress bar functionality
    const progressBar = document.getElementById('progressBar');
    const totalHeight = document.body.scrollHeight - window.innerHeight;

    // Header hide/show functionality
    const header = document.getElementById('header');
    let lastScrollTop = 0;

    // Intersection Observer for fade-in elements
    const fadeElements = document.querySelectorAll('.fade-in');
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
            }
        });
    }, { threshold: 0.1 });

    fadeElements.forEach(element => {
        observer.observe(element);
    });

    // Parallax effect
    const parallaxBg = document.getElementById('parallaxBg');

    // Update elements on scroll
    window.addEventListener('scroll', () => {
        // Update progress bar
        const scrolled = window.scrollY;
        const width = (scrolled / totalHeight) * 100;
        progressBar.style.width = `${width}%`;

        // Header hide/show logic
        if (scrolled > lastScrollTop && scrolled > 100) {
            // Scrolling down
            header.classList.add('hidden');
        } else {
            // Scrolling up
            header.classList.remove('hidden');
        }
        lastScrollTop = scrolled;

        // Parallax effect
        if (parallaxBg) {
            parallaxBg.style.transform = `translateY(${scrolled * 0.5}px)`;
        }
    });
};