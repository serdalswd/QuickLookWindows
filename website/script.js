// Scroll-based fade-in animations
const observer = new IntersectionObserver(
    (entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
            }
        });
    },
    { threshold: 0.15 }
);

document.querySelectorAll('.format-card, .step, .req-card').forEach(el => {
    el.classList.add('fade-up');
    observer.observe(el);
});

// Download button click counter (cosmetic)
const downloadBtn = document.getElementById('download-btn');
if (downloadBtn) {
    downloadBtn.addEventListener('click', () => {
        downloadBtn.textContent = 'İndirme Başlıyor...';
        setTimeout(() => {
            downloadBtn.innerHTML = `
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/>
                    <polyline points="7 10 12 15 17 10"/>
                    <line x1="12" y1="15" x2="12" y2="3"/>
                </svg>
                Ücretsiz İndir
            `;
        }, 2000);
    });
}

// Smooth active nav link highlight on scroll
const sections = document.querySelectorAll('section[id]');
const navLinks = document.querySelectorAll('.nav-links a[href^="#"]');

window.addEventListener('scroll', () => {
    const scrollY = window.scrollY + 100;
    sections.forEach(section => {
        const top = section.offsetTop;
        const height = section.offsetHeight;
        const id = section.getAttribute('id');
        const link = document.querySelector(`.nav-links a[href="#${id}"]`);
        if (link) {
            if (scrollY >= top && scrollY < top + height) {
                navLinks.forEach(l => l.style.color = '');
                link.style.color = '#FAFAFA';
            }
        }
    });
}, { passive: true });

// Animated placeholder graphic (subtle float)
const graphic = document.querySelector('.placeholder-graphic');
if (graphic) {
    let t = 0;
    function animateGraphic() {
        t += 0.02;
        graphic.style.transform = `translateY(${Math.sin(t) * 6}px)`;
        requestAnimationFrame(animateGraphic);
    }
    animateGraphic();
}
