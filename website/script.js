// ─── TYPED ANIMATION ─────────────────────────────────────────────────────────
(function () {
    const words = ['Anında', 'Hızlıca', 'Kolayca', 'Anında'];
    const el = document.getElementById('typed');
    if (!el) return;
    let wi = 0, ci = 0, deleting = false;
    function tick() {
        const word = words[wi];
        el.textContent = deleting ? word.slice(0, ci--) : word.slice(0, ci++);
        if (!deleting && ci > word.length) { deleting = true; setTimeout(tick, 1500); return; }
        if (deleting && ci < 0) { deleting = false; wi = (wi + 1) % words.length; ci = 0; setTimeout(tick, 420); return; }
        setTimeout(tick, deleting ? 55 : 88);
    }
    setTimeout(tick, 900);
})();

// ─── NAV SCROLL ──────────────────────────────────────────────────────────────
const nav = document.getElementById('nav');
window.addEventListener('scroll', () => nav.classList.toggle('scrolled', window.scrollY > 40), { passive: true });

// ─── REVEAL ON SCROLL ────────────────────────────────────────────────────────
const revealObs = new IntersectionObserver(entries => {
    entries.forEach((e, i) => {
        if (e.isIntersecting) {
            setTimeout(() => e.target.classList.add('in'), i * 70);
            revealObs.unobserve(e.target);
        }
    });
}, { threshold: 0.08 });
document.querySelectorAll('.reveal').forEach(el => revealObs.observe(el));

// ─── FEATURE CARD: CURSOR GLOW + 3D TILT ────────────────────────────────────
document.querySelectorAll('.feat-card').forEach(card => {
    card.addEventListener('mousemove', e => {
        const r = card.getBoundingClientRect();
        const x = e.clientX - r.left;
        const y = e.clientY - r.top;
        const cx = r.width / 2, cy = r.height / 2;
        const rx = ((y - cy) / cy) * -7;
        const ry = ((x - cx) / cx) * 7;
        card.style.transform = `perspective(900px) rotateX(${rx}deg) rotateY(${ry}deg) translateY(-6px) scale(1.01)`;
        card.style.setProperty('--mx', `${(x / r.width) * 100}%`);
        card.style.setProperty('--my', `${(y / r.height) * 100}%`);
    });
    card.addEventListener('mouseleave', () => {
        card.style.transform = '';
        card.style.removeProperty('--mx');
        card.style.removeProperty('--my');
    });
});

// ─── APP MOCKUP PARALLAX ─────────────────────────────────────────────────────
const appWin = document.getElementById('app-win');
if (appWin) {
    let tX = 0, tY = 0, cX = 0, cY = 0;
    document.addEventListener('mousemove', e => {
        const cx = window.innerWidth / 2, cy = window.innerHeight / 2;
        tX = ((e.clientY - cy) / cy) * 4.5;
        tY = ((e.clientX - cx) / cx) * -4.5;
    });
    // Smooth interpolation via rAF
    function animMockup() {
        cX += (tX - cX) * 0.06;
        cY += (tY - cY) * 0.06;
        appWin.style.transform = `perspective(1400px) rotateX(${cX}deg) rotateY(${cY}deg)`;
        requestAnimationFrame(animMockup);
    }
    animMockup();
    document.addEventListener('mouseleave', () => { tX = 0; tY = 0; });
}

// ─── FORMAT TAG HOVER RIPPLE ─────────────────────────────────────────────────
document.querySelectorAll('.fmt-tags span').forEach((tag, i) => {
    tag.style.transitionDelay = `${i * 18}ms`;
});

// ─── DOWNLOAD BUTTON FEEDBACK ────────────────────────────────────────────────
document.querySelectorAll('.btn-download').forEach(btn => {
    btn.addEventListener('click', function () {
        const orig = this.innerHTML;
        this.innerHTML = `<svg class="btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg> İndirme Başladı!<span class="btn-sub">İyi kullanımlar 🎉</span>`;
        this.style.background = 'linear-gradient(135deg,#22C55E,#16A34A)';
        this.style.boxShadow = '0 0 0 1px rgba(34,197,94,.4),0 8px 48px rgba(34,197,94,.4)';
        setTimeout(() => {
            this.innerHTML = orig;
            this.style.background = '';
            this.style.boxShadow = '';
        }, 3500);
    });
});

// ─── STAT COUNTER ────────────────────────────────────────────────────────────
const statObs = new IntersectionObserver(entries => {
    entries.forEach(e => {
        if (!e.isIntersecting) return;
        e.target.querySelectorAll('.stat-val').forEach(el => {
            const raw = el.textContent.trim();
            const num = parseFloat(raw.replace(/[^\d.]/g, ''));
            if (isNaN(num) || num === 0) return;
            const prefix = raw.match(/^[^\d]*/)?.[0] ?? '';
            const suffix = raw.replace(/^[^\d]*/, '').replace(/[\d.]+/, '');
            let cur = 0;
            const inc = num / 38;
            const timer = setInterval(() => {
                cur += inc;
                if (cur >= num) { cur = num; clearInterval(timer); }
                el.textContent = prefix + Math.floor(cur) + suffix;
            }, 25);
        });
        statObs.unobserve(e.target);
    });
}, { threshold: 0.5 });
const statsEl = document.querySelector('.hero-stats');
if (statsEl) statObs.observe(statsEl);

// ─── SMOOTH SCROLL FOR NAV LINKS ─────────────────────────────────────────────
document.querySelectorAll('a[href^="#"]').forEach(a => {
    a.addEventListener('click', e => {
        const target = document.querySelector(a.getAttribute('href'));
        if (!target) return;
        e.preventDefault();
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
});
