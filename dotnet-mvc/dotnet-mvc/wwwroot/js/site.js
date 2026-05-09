/* =============================================================
   DEC – Dinheiro em Casa
   JavaScript global
   ============================================================= */

(function () {
    'use strict';

    // -----------------------------------------------------------
    // Toggle do menu mobile (navbar pública)
    // -----------------------------------------------------------
    const navToggle = document.querySelector('[data-nav-toggle]');
    const navMenu = document.querySelector('[data-nav-menu]');

    if (navToggle && navMenu) {
        navToggle.addEventListener('click', function () {
            navMenu.classList.toggle('is-open');
            navToggle.setAttribute(
                'aria-expanded',
                navMenu.classList.contains('is-open') ? 'true' : 'false'
            );
        });
    }

    // -----------------------------------------------------------
    // Toggle da sidebar (área autenticada, mobile)
    // -----------------------------------------------------------
    const sidebarToggle = document.querySelector('[data-sidebar-toggle]');
    const sidebar = document.querySelector('[data-sidebar]');
    const sidebarBackdrop = document.querySelector('[data-sidebar-backdrop]');

    function closeSidebar() {
        if (sidebar) sidebar.classList.remove('is-open');
        if (sidebarBackdrop) sidebarBackdrop.classList.remove('is-open');
    }

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('is-open');
            if (sidebarBackdrop) sidebarBackdrop.classList.toggle('is-open');
        });
    }

    if (sidebarBackdrop) {
        sidebarBackdrop.addEventListener('click', closeSidebar);
    }

    // -----------------------------------------------------------
    // Fechar alertas/notificações
    // -----------------------------------------------------------
    document.querySelectorAll('[data-alert-close]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const alert = btn.closest('.alert');
            if (alert) {
                alert.style.transition = 'opacity 0.25s ease';
                alert.style.opacity = '0';
                setTimeout(function () { alert.remove(); }, 250);
            }
        });
    });

    // -----------------------------------------------------------
    // Confirmação antes de ações destrutivas
    // Uso: <button data-confirm="Tens a certeza?">Apagar</button>
    // -----------------------------------------------------------
    document.querySelectorAll('[data-confirm]').forEach(function (el) {
        el.addEventListener('click', function (e) {
            const message = el.getAttribute('data-confirm') || 'Confirmas esta ação?';
            if (!window.confirm(message)) {
                e.preventDefault();
                e.stopImmediatePropagation();
            }
        });
    });

    // -----------------------------------------------------------
    // Copiar para a área de transferência
    // Uso: <button data-copy="ABC123">Copiar</button>
    // -----------------------------------------------------------
    document.querySelectorAll('[data-copy]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const text = btn.getAttribute('data-copy');
            if (!text) return;
            navigator.clipboard.writeText(text).then(function () {
                const original = btn.textContent;
                btn.textContent = 'Copiado!';
                setTimeout(function () { btn.textContent = original; }, 1500);
            }).catch(function () {
                // fallback silencioso
            });
        });
    });

    // -----------------------------------------------------------
    // Toggle de password (mostrar/esconder)
    // Uso: <button data-toggle-password="#pwdInputId">Mostrar</button>
    // -----------------------------------------------------------
    document.querySelectorAll('[data-toggle-password]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const target = document.querySelector(btn.getAttribute('data-toggle-password'));
            if (!target) return;
            target.type = target.type === 'password' ? 'text' : 'password';
        });
    });

    // -----------------------------------------------------------
    // Auto-dismiss de alertas marcados como --auto
    // -----------------------------------------------------------
    document.querySelectorAll('.alert[data-auto-dismiss]').forEach(function (alert) {
        const delay = parseInt(alert.getAttribute('data-auto-dismiss'), 10) || 4000;
        setTimeout(function () {
            alert.style.transition = 'opacity 0.4s ease';
            alert.style.opacity = '0';
            setTimeout(function () { alert.remove(); }, 400);
        }, delay);
    });
})();
