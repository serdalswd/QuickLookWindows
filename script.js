// ─── PARTICLES ───────────────────────────────────────────────────────────────
(function () {
    const canvas = document.getElementById('particles');
    const ctx = canvas.getContext('2d');
    let W, H, pts = [];

    function resize() { W = canvas.width = window.innerWidth; H = canvas.height = window.innerHeight; }
    resize();
    window.addEventListener('resize', resize);

    for (let i = 0; i < 80; i++)
        pts.push({ x: Math.random() * 1920, y: Math.random() * 1080, vx: (Math.random() - .5) * .3, vy: (Math.random() - .5) * .3, r: Math.random() * 1.5 + .5, a: Math.random() });

    function draw() {
        ctx.clearRect(0, 0, W, H);
        pts.forEach(p => {
            p.x += p.vx; p.y += p.vy;
            if (p.x < 0) p.x = W; if (p.x > W) p.x = 0;
            if (p.y < 0) p.y = H; if (p.y > H) p.y = 0;
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
            ctx.fillStyle = `rgba(108,99,255,${p.a * .6})`;
            ctx.fill();
        });
        for (let i = 0; i < pts.length; i++) {
            for (let j = i + 1; j < pts.length; j++) {
                const dx = pts[i].x - pts[j].x, dy = pts[i].y - pts[j].y;
                const d = Math.sqrt(dx * dx + dy * dy);
                if (d < 140) {
                    ctx.beginPath();
                    ctx.moveTo(pts[i].x, pts[i].y);
                    ctx.lineTo(pts[j].x, pts[j].y);
                    ctx.strokeStyle = `rgba(108,99,255,${(1 - d / 140) * .12})`;
                    ctx.lineWidth = 1;
                    ctx.stroke();
                }
            }
        }
        requestAnimationFrame(draw);
    }
    draw();
})();

// ─── TYPED ANIMATION ─────────────────────────────────────────────────────────
(function () {
    const words = ['Anında', 'Hızlıca', 'Kolayca', 'Anında'];
    const el = document.getElementById('typed');
    if (!el) return;
    let wi = 0, ci = 0, deleting = false;
    function tick() {
        const word = words[wi];
        el.textContent = deleting ? word.slice(0, ci--) : word.slice(0, ci++);
        if (!deleting && ci > word.length) { deleting = true; setTimeout(tick, 1400); return; }
        if (deleting && ci < 0) { deleting = false; wi = (wi + 1) % words.length; ci = 0; setTimeout(tick, 400); return; }
        setTimeout(tick, deleting ? 60 : 90);
    }
    setTimeout(tick, 800);
})();

// ─── NAV SCROLL ──────────────────────────────────────────────────────────────
const nav = document.getElementById('nav');
window.addEventListener('scroll', () => nav.classList.toggle('scrolled', window.scrollY > 40), { passive: true });

// ─── REVEAL ON SCROLL ────────────────────────────────────────────────────────
const revealObs = new IntersectionObserver(entries => {
    entries.forEach((e, i) => {
        if (e.isIntersecting) {
            setTimeout(() => e.target.classList.add('in'), i * 60);
            revealObs.unobserve(e.target);
        }
    });
}, { threshold: 0.1 });
document.querySelectorAll('.reveal').forEach(el => revealObs.observe(el));

// ─── MOCKUP MOUSE PARALLAX ───────────────────────────────────────────────────
const mockup = document.querySelector('.mockup-win');
if (mockup) {
    document.addEventListener('mousemove', e => {
        const cx = window.innerWidth / 2, cy = window.innerHeight / 2;
        const rx = ((e.clientY - cy) / cy) * 4;
        const ry = ((e.clientX - cx) / cx) * -4;
        mockup.style.transform = `perspective(1200px) rotateX(${rx}deg) rotateY(${ry}deg)`;
    });
    document.addEventListener('mouseleave', () => mockup.style.transform = '');
}

// ─── DOWNLOAD BUTTON FEEDBACK ────────────────────────────────────────────────
document.querySelectorAll('.btn-download').forEach(btn => {
    btn.addEventListener('click', function () {
        const orig = this.innerHTML;
        this.innerHTML = `<svg class="btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg> İndirme Başladı!<span class="btn-sub">İyi kullanımlar 🎉</span>`;
        setTimeout(() => this.innerHTML = orig, 3000);
    });
});

// ─── STAT COUNTER ────────────────────────────────────────────────────────────
const statObs = new IntersectionObserver(entries => {
    entries.forEach(e => {
        if (!e.isIntersecting) return;
        e.target.querySelectorAll('.stat-val').forEach(el => {
            const raw = el.textContent.trim();
            const num = parseFloat(raw);
            if (isNaN(num)) return;
            const suffix = raw.replace(/[\d.]/g, '');
            let cur = 0;
            const inc = num / 40;
            const t = setInterval(() => {
                cur += inc;
                if (cur >= num) { cur = num; clearInterval(t); }
                el.textContent = Math.floor(cur) + suffix;
            }, 28);
        });
        statObs.unobserve(e.target);
    });
}, { threshold: .5 });
const statsEl = document.querySelector('.hero-stats');
if (statsEl) statObs.observe(statsEl);
