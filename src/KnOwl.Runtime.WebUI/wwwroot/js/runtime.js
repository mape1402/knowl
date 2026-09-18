(() => {
    const showModal = modal => {
        if (!modal) {
            return;
        }

        modal.classList.add('show');
        modal.removeAttribute('aria-hidden');
        document.body.style.overflow = 'hidden';
    };

    const hideModal = modal => {
        if (!modal) {
            return;
        }

        modal.classList.remove('show');
        modal.setAttribute('aria-hidden', 'true');
        if (!document.querySelector('.modal.show')) {
            document.body.style.overflow = '';
        }
    };

    window.bootstrap = window.bootstrap || {};
    window.bootstrap.Modal = window.bootstrap.Modal || {
        getOrCreateInstance: modal => ({
            show: () => showModal(modal),
            hide: () => hideModal(modal)
        })
    };

    document.addEventListener('click', event => {
        const opener = event.target.closest('[data-bs-toggle="modal"][data-bs-target]');
        if (opener) {
            const modal = document.querySelector(opener.dataset.bsTarget);
            if (modal) {
                const showEvent = new Event('show.bs.modal');
                Object.defineProperty(showEvent, 'relatedTarget', { value: opener });
                modal.dispatchEvent(showEvent);
                showModal(modal);
            }
            return;
        }

        const dismiss = event.target.closest('[data-bs-dismiss="modal"]');
        if (dismiss) {
            hideModal(dismiss.closest('.modal'));
        }
    });

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') {
            return;
        }

        const openModal = document.querySelector('.modal.show');
        if (openModal) {
            hideModal(openModal);
        }
    });
})();

(() => {
    const STORAGE_KEY = 'knowlThemeMode';
    const root = document.documentElement;
    const toggle = document.getElementById('theme-toggle');
    const palettes = {
        light: {
            '--knowl-color-scheme': 'light',
            '--knowl-content-bg': '#f8fafc',
            '--knowl-surface': '#ffffff',
            '--knowl-text': '#102033',
            '--knowl-muted-text': '#64748b',
            '--knowl-border': '#d8e0ea',
            '--knowl-subtle-bg': '#f1f5f9',
            '--knowl-code-bg': '#f8fafc',
            '--knowl-code-text': '#0f172a',
            '--knowl-shadow-color': 'rgba(15, 23, 42, 0.12)'
        },
        dark: {
            '--knowl-color-scheme': 'dark',
            '--knowl-content-bg': '#0f172a',
            '--knowl-surface': '#1e293b',
            '--knowl-text': '#e5e7eb',
            '--knowl-muted-text': '#94a3b8',
            '--knowl-border': '#334155',
            '--knowl-subtle-bg': '#172033',
            '--knowl-code-bg': '#020617',
            '--knowl-code-text': '#dbeafe',
            '--knowl-shadow-color': 'rgba(2, 6, 23, 0.48)'
        }
    };

    const applyMode = mode => {
        const selected = mode === 'light' ? 'light' : 'dark';
        root.dataset.bsTheme = selected;
        root.dataset.knowlTheme = selected;
        root.style.colorScheme = selected;

        Object.entries(palettes[selected]).forEach(([name, value]) => root.style.setProperty(name, value));
        root.style.setProperty('--bs-body-bg', palettes[selected]['--knowl-content-bg']);
        root.style.setProperty('--bs-body-color', palettes[selected]['--knowl-text']);
        root.style.setProperty('--bs-border-color', palettes[selected]['--knowl-border']);

        if (toggle) {
            const icon = toggle.querySelector('i');
            const label = toggle.querySelector('span');
            if (icon) {
                icon.className = selected === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
            }
            if (label) {
                label.textContent = selected === 'dark' ? 'Light' : 'Dark';
            }
            toggle.setAttribute('aria-pressed', selected === 'dark' ? 'true' : 'false');
        }
    };

    const initialMode = localStorage.getItem(STORAGE_KEY) || root.dataset.knowlTheme || 'light';
    applyMode(initialMode);

    toggle?.addEventListener('click', () => {
        const next = root.dataset.knowlTheme === 'dark' ? 'light' : 'dark';
        localStorage.setItem(STORAGE_KEY, next);
        applyMode(next);
    });
})();
