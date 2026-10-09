(() => {
    const STORAGE_KEY = 'sidebarCollapsed';
    const shell = document.querySelector('.app-shell');
    const toggle = document.getElementById('sidebar-toggle');

    if (localStorage.getItem(STORAGE_KEY) === '1') {
        shell?.classList.add('sidebar-collapsed');
    }

    toggle?.addEventListener('click', () => {
        shell?.classList.toggle('sidebar-collapsed');
        const isCollapsed = shell?.classList.contains('sidebar-collapsed');
        localStorage.setItem(STORAGE_KEY, isCollapsed ? '1' : '0');
    });
})();

(() => {
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
})();

(() => {
    const menus = Array.from(document.querySelectorAll('.topbar-user-menu'));
    if (!menus.length) {
        return;
    }

    const closeMenu = menu => {
        menu.removeAttribute('open');
    };

    menus.forEach(menu => {
        menu.addEventListener('toggle', () => {
            if (!menu.open) {
                return;
            }

            menus.forEach(otherMenu => {
                if (otherMenu !== menu) {
                    closeMenu(otherMenu);
                }
            });
        });
    });

    document.addEventListener('click', event => {
        menus.forEach(menu => {
            if (menu.open && !menu.contains(event.target)) {
                closeMenu(menu);
            }
        });
    });

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') {
            return;
        }

        menus.forEach(menu => {
            if (!menu.open) {
                return;
            }

            closeMenu(menu);
            menu.querySelector('summary')?.focus?.({ preventScroll: true });
        });
    });
})();

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
})();

(() => {
    const STORAGE_KEY = 'knowlThemeMode';
    const root = document.documentElement;
    const toggle = document.getElementById('theme-toggle');

    const applyMode = mode => {
        const selected = mode === 'light' ? 'light' : 'dark';
        root.dataset.bsTheme = selected;
        root.dataset.knowlTheme = selected;
        root.style.colorScheme = selected;

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
