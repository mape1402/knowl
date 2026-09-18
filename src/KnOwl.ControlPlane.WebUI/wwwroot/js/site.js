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
