// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ── Sidebar toggle ────────────────────────────────────────
(function () {
    const STORAGE_KEY = 'sidebarCollapsed';
    const shell = document.querySelector('.app-shell');
    const toggle = document.getElementById('sidebar-toggle');

    // Restore persisted state immediately to avoid flash
    if (localStorage.getItem(STORAGE_KEY) === '1') {
        shell?.classList.add('sidebar-collapsed');
    }

    toggle?.addEventListener('click', () => {
        shell?.classList.toggle('sidebar-collapsed');
        const isCollapsed = shell?.classList.contains('sidebar-collapsed');
        localStorage.setItem(STORAGE_KEY, isCollapsed ? '1' : '0');
    });
}());

// ── Clickable cards ───────────────────────────────────────
(function () {
    document.querySelectorAll('[data-open-url]').forEach(card => {
        card.addEventListener('click', event => {
            if (event.target.closest('a, button, input, textarea, select, label, form, .dropdown, .dropdown-menu')) {
                return;
            }

            const url = card.getAttribute('data-open-url');
            if (url) {
                window.location.href = url;
            }
        });
    });
}());

// ── History back buttons ─────────────────────────────────
(function () {
    document.querySelectorAll('[data-history-back]').forEach(button => {
        button.addEventListener('click', () => {
            const fallbackUrl = button.getAttribute('data-fallback-url');
            const referrer = document.referrer ? new URL(document.referrer, window.location.origin) : null;

            if (referrer && referrer.origin === window.location.origin && referrer.href !== window.location.href) {
                window.history.back();
                return;
            }

            if (window.history.length > 1) {
                window.history.back();
                return;
            }

            if (fallbackUrl) {
                window.location.href = fallbackUrl;
            }
        });
    });
}());

// ── Card contextual menus ────────────────────────────────
(function () {
    const openClass = 'od-item-card-menu-open';

    document.addEventListener('show.bs.dropdown', event => {
        const card = event.target.closest('.od-item-card');
        if (!card) {
            return;
        }

        document.querySelectorAll(`.${openClass}`).forEach(openCard => {
            if (openCard !== card) {
                openCard.classList.remove(openClass);
            }
        });

        card.classList.add(openClass);
    });

    document.addEventListener('hidden.bs.dropdown', event => {
        event.target.closest('.od-item-card')?.classList.remove(openClass);
    });
}());

// ── Messages and confirmations ───────────────────────────
(function () {
    const dismissWithFade = element => {
        element.classList.add('knowl-toast-hiding');
        window.setTimeout(() => element.remove(), 160);
    };

    document.querySelectorAll('[data-message-toast]').forEach(toast => {
        const dismiss = () => dismissWithFade(toast);
        const timeout = Number.parseInt(toast.getAttribute('data-message-timeout') || '5200', 10);
        let timer = Number.isFinite(timeout) && timeout > 0
            ? window.setTimeout(dismiss, timeout)
            : 0;

        toast.querySelectorAll('[data-message-dismiss]').forEach(button => {
            button.addEventListener('click', () => {
                if (timer) {
                    window.clearTimeout(timer);
                }
                dismiss();
            });
        });

        toast.addEventListener('mouseenter', () => {
            if (timer) {
                window.clearTimeout(timer);
                timer = 0;
            }
        });

        toast.addEventListener('mouseleave', () => {
            if (!timer && timeout > 0) {
                timer = window.setTimeout(dismiss, 1800);
            }
        });
    });

    const closeDialog = (overlay, previousFocus, onKeyDown) => {
        overlay.remove();
        previousFocus?.focus?.();
        document.removeEventListener('keydown', onKeyDown);
    };

    const appendButton = (container, text, className) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = className;
        button.textContent = text;
        container.appendChild(button);
        return button;
    };

    const showStatusConfirmation = form => {
        document.querySelectorAll('[data-status-confirmation]').forEach(node => node.remove());

        const previousFocus = document.activeElement;
        const overlay = document.createElement('div');
        overlay.className = 'knowl-message-overlay';
        overlay.setAttribute('data-status-confirmation', '');

        const dialog = document.createElement('div');
        dialog.className = 'knowl-message-dialog';
        dialog.setAttribute('role', 'alertdialog');
        dialog.setAttribute('aria-modal', 'true');
        dialog.setAttribute('aria-label', form.getAttribute('data-confirm-title') || 'Confirm status change');
        dialog.tabIndex = -1;

        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'knowl-message-close';
        closeButton.setAttribute('aria-label', 'Cancel status change');
        closeButton.innerHTML = '<i class="bi bi-x-lg"></i>';

        const body = document.createElement('div');
        body.className = 'knowl-message-dialog-body';

        const icon = document.createElement('span');
        icon.className = 'knowl-message-dialog-icon';
        icon.setAttribute('aria-hidden', 'true');
        icon.innerHTML = '<i class="bi bi-question-circle"></i>';

        const copy = document.createElement('div');
        const title = document.createElement('h2');
        title.textContent = form.getAttribute('data-confirm-title') || 'Confirm status change';
        const message = document.createElement('p');
        message.textContent = form.getAttribute('data-confirm-message') || 'Apply this status change?';
        copy.appendChild(title);
        copy.appendChild(message);

        body.appendChild(icon);
        body.appendChild(copy);

        const actions = document.createElement('div');
        actions.className = 'knowl-message-dialog-actions';
        const cancel = appendButton(actions, 'Cancel', 'btn btn-outline-secondary btn-sm');
        const confirmLabel = form.getAttribute('data-confirm-label') || 'Confirm';
        const isDestructive = /abandon|archive|deprecat/i.test(confirmLabel);
        const confirm = appendButton(actions, confirmLabel, isDestructive ? 'btn btn-danger btn-sm' : 'btn btn-primary btn-sm');

        dialog.appendChild(closeButton);
        dialog.appendChild(body);
        dialog.appendChild(actions);
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        const dismiss = () => closeDialog(overlay, previousFocus, onKeyDown);
        const submit = () => {
            form.dataset.statusConfirmed = 'true';
            closeDialog(overlay, previousFocus, onKeyDown);
            form.requestSubmit ? form.requestSubmit() : form.submit();
        };

        function onKeyDown(event) {
            if (event.key === 'Escape') {
                dismiss();
            }
        }

        closeButton.addEventListener('click', dismiss);
        cancel.addEventListener('click', dismiss);
        confirm.addEventListener('click', submit);
        overlay.addEventListener('click', event => {
            if (event.target === overlay) {
                dismiss();
            }
        });
        document.addEventListener('keydown', onKeyDown);
        dialog.focus();
    };

    document.querySelectorAll('[data-status-transition-form]').forEach(form => {
        form.addEventListener('submit', event => {
            if (form.dataset.statusConfirmed === 'true') {
                delete form.dataset.statusConfirmed;
                return;
            }

            event.preventDefault();
            showStatusConfirmation(form);
        });
    });
}());

// ── Theme toggle ──────────────────────────────────────────
(function () {
    const STORAGE_KEY = 'knowlThemeMode';
    const root = document.documentElement;
    const toggle = document.getElementById('theme-toggle');

    const applyMode = mode => {
        const selected = mode === 'light' ? 'light' : 'dark';
        root.dataset.bsTheme = selected;
        root.dataset.knowlTheme = selected;
        root.style.colorScheme = selected;
        window.ButterMorphHost?.setThemeMode?.(selected);

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
}());
