// ── Formatter cédula Costa Rica ────────────────────────────────────────────
(function () {
    function formatCedula(value) {
        var digits = (value || '').replace(/\D/g, '');
        if (digits.length === 0) return '';
        if (digits.length <= 1) return digits;
        if (digits.length <= 5) return digits.slice(0, 1) + '-' + digits.slice(1);
        return digits.slice(0, 1) + '-' + digits.slice(1, 5) + '-' + digits.slice(5, 9);
    }

    function onCedulaInput(e) {
        var el = e.target;
        var cursor = el.selectionStart;
        var oldLen = el.value.length;
        var newVal = formatCedula(el.value);
        el.value = newVal;
        var newCursor = Math.max(0, Math.min(cursor + (newVal.length - oldLen), newVal.length));
        el.setSelectionRange(newCursor, newCursor);
    }

    function initCedulaFormat() {
        document.querySelectorAll(
            'input#cedula, input#Cedula, input[name="cedula"], input[name="Cedula"], input[data-format-cedula], input.cedula-format'
        ).forEach(function (input) {
            if (input._cedulaFormatInit) return;
            input._cedulaFormatInit = true;
            input.addEventListener('input', onCedulaInput);
            input.addEventListener('paste', () => setTimeout(() => onCedulaInput({ target: input }), 0));
            if (input.value) input.value = formatCedula(input.value);
        });
    }

    document.readyState === 'loading'
        ? document.addEventListener('DOMContentLoaded', initCedulaFormat)
        : initCedulaFormat();

    document.addEventListener('DOMContentLoaded', () => setTimeout(initCedulaFormat, 100));
})();

// ── Animaciones al hacer scroll (Intersection Observer) ───────────────────
document.addEventListener('DOMContentLoaded', function () {

    // Animar elementos con clase animate-on-scroll al entrar al viewport
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-fade-up');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.card-dashboard, .stat-card, .quick-card').forEach(el => {
        observer.observe(el);
    });

    // ── Feedback visual en botones de submit ──────────────────────────────
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            const btn = form.querySelector('[type="submit"]');
            if (btn && !btn.dataset.noLoader) {
                const original = btn.innerHTML;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Procesando...';
                btn.disabled = true;
                // Restaurar si la página no redirige (error de validación)
                setTimeout(() => {
                    btn.innerHTML = original;
                    btn.disabled = false;
                }, 6000);
            }
        });
    });

    // ── Auto-dismiss de alertas después de 5 segundos ─────────────────────
    document.querySelectorAll('.alert.alert-success').forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert?.close();
        }, 5000);
    });

    // ── Ripple effect en botones ──────────────────────────────────────────
    document.querySelectorAll('.btn').forEach(btn => {
        btn.addEventListener('click', function (e) {
            const ripple = document.createElement('span');
            const rect = btn.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);

            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                left: ${e.clientX - rect.left - size / 2}px;
                top: ${e.clientY - rect.top - size / 2}px;
                background: rgba(255,255,255,.25);
                border-radius: 50%;
                transform: scale(0);
                animation: ripple-anim .5s ease;
                pointer-events: none;
            `;

            if (getComputedStyle(btn).position === 'static') {
                btn.style.position = 'relative';
            }
            btn.style.overflow = 'hidden';
            btn.appendChild(ripple);
            setTimeout(() => ripple.remove(), 500);
        });
    });
});

// Keyframe para el ripple vía JS
const rippleStyle = document.createElement('style');
rippleStyle.textContent = `
    @keyframes ripple-anim {
        to { transform: scale(2.5); opacity: 0; }
    }
`;
document.head.appendChild(rippleStyle);