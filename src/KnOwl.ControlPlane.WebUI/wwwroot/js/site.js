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

// ── Message boxes ────────────────────────────────────────
(function () {
    document.querySelectorAll('[data-message-box]').forEach(box => {
        const dialog = box.querySelector('.knowl-message-dialog');
        const previousFocus = document.activeElement;

        const dismiss = () => {
            box.remove();
            previousFocus?.focus?.();
            document.removeEventListener('keydown', onKeyDown);
        };

        function onKeyDown(event) {
            if (event.key === 'Escape') {
                dismiss();
            }
        }

        box.querySelectorAll('[data-message-dismiss]').forEach(button => {
            button.addEventListener('click', dismiss);
        });

        box.addEventListener('click', event => {
            if (event.target === box) {
                dismiss();
            }
        });

        document.addEventListener('keydown', onKeyDown);
        dialog?.focus();
    });
}());

// ── Responsive card grids ─────────────────────────────────
(function () {
    const parsePixels = (value, fallback) => {
        const parsed = Number.parseFloat(value);
        return Number.isFinite(parsed) ? parsed : fallback;
    };

    const layoutGrid = grid => {
        const cards = Array.from(grid.children).filter(child => child.classList.contains('od-item-card'));
        if (cards.length === 0) {
            return;
        }

        const styles = window.getComputedStyle(grid);
        const width = grid.clientWidth;
        const gap = parsePixels(styles.columnGap, 12);
        const minWidth = parsePixels(styles.getPropertyValue('--od-card-min-width'), 280);
        const maxWidth = parsePixels(styles.getPropertyValue('--od-card-max-width'), 320);
        const preferredWidth = parsePixels(styles.getPropertyValue('--od-card-preferred-width'), 300);
        const maxColumns = Math.max(1, Math.floor((width + gap) / (minWidth + gap)));

        if (cards.length < maxColumns) {
            grid.style.setProperty('--od-card-width', `${preferredWidth}px`);
            grid.style.gridTemplateColumns = `repeat(${cards.length}, minmax(0, var(--od-card-width)))`;
            return;
        }

        const columns = maxColumns;
        const computedWidth = (width - (gap * (columns - 1))) / columns;
        const cardWidth = Math.min(maxWidth, Math.max(minWidth, computedWidth));
        grid.style.setProperty('--od-card-width', `${cardWidth}px`);
        grid.style.gridTemplateColumns = `repeat(${columns}, minmax(0, var(--od-card-width)))`;
    };

    const grids = document.querySelectorAll('.od-grid');
    if (grids.length === 0) {
        return;
    }

    grids.forEach(layoutGrid);

    if ('ResizeObserver' in window) {
        const observer = new ResizeObserver(entries => {
            entries.forEach(entry => layoutGrid(entry.target));
        });
        grids.forEach(grid => observer.observe(grid));
        return;
    }

    window.addEventListener('resize', () => grids.forEach(layoutGrid));
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
