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

// ── Documentation table of contents ─────────────────────
(function () {
    const links = Array.from(document.querySelectorAll('.documentation-toc-link'));
    if (!links.length) {
        return;
    }

    const sections = links
        .map(link => {
            const id = decodeURIComponent((link.getAttribute('href') || '').replace(/^#/, ''));
            return { link, heading: id ? document.getElementById(id) : null };
        })
        .filter(item => item.heading);

    if (!sections.length) {
        return;
    }

    const setActive = activeLink => {
        links.forEach(link => link.classList.toggle('active', link === activeLink));
    };

    const observer = new IntersectionObserver(entries => {
        const visible = entries
            .filter(entry => entry.isIntersecting)
            .sort((left, right) => left.boundingClientRect.top - right.boundingClientRect.top)[0];
        if (!visible) {
            return;
        }

        const active = sections.find(item => item.heading === visible.target);
        if (active) {
            setActive(active.link);
        }
    }, { rootMargin: '-18% 0px -72% 0px', threshold: 0.01 });

    sections.forEach(item => observer.observe(item.heading));
    setActive(sections[0].link);
}());

// ── Documentation focus mode ────────────────────────────
(function () {
    const shell = document.querySelector('.app-shell');
    const panel = document.querySelector('.documentation-render-panel');
    const toggles = Array.from(document.querySelectorAll('[data-documentation-focus-toggle]'));
    const exits = Array.from(document.querySelectorAll('[data-documentation-focus-exit]'));

    if (!shell || !panel || (!toggles.length && !exits.length)) {
        return;
    }

    const reader = panel.querySelector('.documentation-reader');
    const setToggleState = enabled => {
        toggles.forEach(button => {
            const icon = button.querySelector('i');
            const label = button.querySelector('span');
            button.setAttribute('aria-pressed', enabled ? 'true' : 'false');
            button.title = enabled ? 'Exit the full page reading view' : 'Use the full page reading view';

            if (icon) {
                icon.className = enabled ? 'bi bi-fullscreen-exit' : 'bi bi-arrows-fullscreen';
            }

            if (label) {
                label.textContent = enabled ? 'Exit' : 'Focus';
            }
        });
    };

    const setFocusMode = enabled => {
        shell.classList.toggle('documentation-focus-mode', enabled);
        setToggleState(enabled);

        if (enabled) {
            window.setTimeout(() => reader?.focus?.({ preventScroll: true }), 0);
        }
    };

    toggles.forEach(button => {
        button.addEventListener('click', () => {
            setFocusMode(!shell.classList.contains('documentation-focus-mode'));
        });
    });

    exits.forEach(button => {
        button.addEventListener('click', () => setFocusMode(false));
    });

    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && shell.classList.contains('documentation-focus-mode')) {
            setFocusMode(false);
        }
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
    const toggles = Array.from(new Set([
        ...document.querySelectorAll('[data-theme-toggle]'),
        ...document.querySelectorAll('#theme-toggle')
    ]));

    const applyMode = mode => {
        const selected = mode === 'light' ? 'light' : 'dark';
        root.dataset.bsTheme = selected;
        root.dataset.knowlTheme = selected;
        root.style.colorScheme = selected;
        window.ButterMorphHost?.setThemeMode?.(selected);

        toggles.forEach(toggle => {
            const icon = toggle.querySelector('i');
            const label = toggle.querySelector('span');
            if (icon) {
                icon.className = selected === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
            }
            if (label) {
                label.textContent = selected === 'dark' ? 'Light' : 'Dark';
            }
            toggle.setAttribute('aria-pressed', selected === 'dark' ? 'true' : 'false');
        });

        document.dispatchEvent(new CustomEvent('knowl:theme-change', { detail: { mode: selected } }));
    };

    const initialMode = localStorage.getItem(STORAGE_KEY) || root.dataset.knowlTheme || 'light';
    applyMode(initialMode);

    toggles.forEach(toggle => toggle.addEventListener('click', () => {
        const next = root.dataset.knowlTheme === 'dark' ? 'light' : 'dark';
        localStorage.setItem(STORAGE_KEY, next);
        applyMode(next);
    }));
}());

// ── Mermaid diagrams in documentation ───────────────────
(function () {
    const reader = document.querySelector('.documentation-reader');
    if (!reader) {
        return;
    }

    const sourceBlocks = Array.from(reader.querySelectorAll('pre > code.language-mermaid, pre > code.lang-mermaid'));
    if (!sourceBlocks.length) {
        return;
    }

    const MERMAID_SCRIPT_URL = '/_content/KnOwl.ControlPlane.WebUI/lib/mermaid/mermaid.min.js';
    let loadPromise;
    let renderVersion = 0;
    let activeDialog;

    const cssValue = (name, fallback) => {
        const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        return value || fallback;
    };

    const mermaidThemeVariables = () => {
        const surface = cssValue('--knowl-surface', '#ffffff');
        const subtle = cssValue('--knowl-subtle-bg', '#f4f4fb');
        const border = cssValue('--knowl-border', '#d9dcec');
        const text = cssValue('--knowl-text', '#1f1433');
        const primary = cssValue('--knowl-primary', '#2563eb');
        const mode = document.documentElement.dataset.knowlTheme === 'dark' ? 'dark' : 'light';

        return {
            darkMode: mode === 'dark',
            background: 'transparent',
            mainBkg: surface,
            secondBkg: subtle,
            tertiaryColor: surface,
            primaryColor: surface,
            primaryTextColor: text,
            primaryBorderColor: border,
            secondaryColor: subtle,
            secondaryTextColor: text,
            secondaryBorderColor: border,
            tertiaryTextColor: text,
            tertiaryBorderColor: border,
            lineColor: primary,
            textColor: text,
            nodeTextColor: text,
            noteBkgColor: subtle,
            noteTextColor: text,
            clusterBkg: subtle,
            clusterBorder: border,
            edgeLabelBackground: surface,
            actorBkg: surface,
            actorBorder: border,
            actorTextColor: text,
            labelTextColor: text,
            titleColor: text,
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", Helvetica, Arial, sans-serif'
        };
    };

    const loadMermaid = () => {
        if (window.mermaid) {
            return Promise.resolve(window.mermaid);
        }

        if (!loadPromise) {
            loadPromise = new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = MERMAID_SCRIPT_URL;
                script.async = true;
                script.onload = () => window.mermaid
                    ? resolve(window.mermaid)
                    : reject(new Error('Mermaid did not expose a global API.'));
                script.onerror = () => reject(new Error('Mermaid could not be loaded.'));
                document.head.appendChild(script);
            });
        }

        return loadPromise;
    };

    const clampZoom = value => Math.min(4, Math.max(0.35, value));

    const closeDiagramDialog = () => {
        if (!activeDialog) {
            return;
        }

        const { overlay, previousFocus, onKeyDown } = activeDialog;
        document.removeEventListener('keydown', onKeyDown);
        overlay.remove();
        previousFocus?.focus?.({ preventScroll: true });
        activeDialog = null;
    };

    const parseViewBox = value => {
        if (!value) {
            return null;
        }

        const parts = value.split(/[\s,]+/).map(Number.parseFloat).filter(Number.isFinite);
        if (parts.length === 4 && parts[2] > 0 && parts[3] > 0) {
            return {
                x: parts[0],
                y: parts[1],
                width: parts[2],
                height: parts[3]
            };
        }

        return null;
    };

    const serializeViewBox = viewBox => `${viewBox.x} ${viewBox.y} ${viewBox.width} ${viewBox.height}`;

    const readSvgViewBox = svg => {
        const viewBox = parseViewBox(svg.dataset.documentationMermaidBaseViewBox)
            || parseViewBox(svg.getAttribute('viewBox'));
        if (viewBox) {
            return viewBox;
        }

        const width = Number.parseFloat(svg.getAttribute('width')) || svg.getBoundingClientRect().width || 800;
        const height = Number.parseFloat(svg.getAttribute('height')) || svg.getBoundingClientRect().height || 450;
        return {
            x: 0,
            y: 0,
            width,
            height
        };
    };

    const createDiagramButton = (label, icon, titleText, className = 'documentation-mermaid-button') => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = className;
        button.setAttribute('aria-label', titleText);
        button.title = titleText;
        button.innerHTML = `<i class="bi ${icon}"></i><span>${label}</span>`;
        return button;
    };

    const createSvgZoomController = (svg, viewport, zoomTarget) => {
        const baseViewBox = readSvgViewBox(svg);
        svg.dataset.documentationMermaidBaseViewBox = serializeViewBox(baseViewBox);
        svg.setAttribute('viewBox', serializeViewBox(baseViewBox));
        svg.setAttribute('preserveAspectRatio', 'xMidYMid meet');

        const view = {
            centerX: baseViewBox.x + baseViewBox.width / 2,
            centerY: baseViewBox.y + baseViewBox.height / 2,
            scale: 1
        };

        const currentViewBox = () => {
            const width = baseViewBox.width / view.scale;
            const height = baseViewBox.height / view.scale;
            return {
                x: view.centerX - width / 2,
                y: view.centerY - height / 2,
                width,
                height
            };
        };

        const applyViewBox = () => {
            svg.setAttribute('viewBox', serializeViewBox(currentViewBox()));
            if (zoomTarget) {
                zoomTarget.dataset.zoom = view.scale.toFixed(2);
            }
        };

        const setScale = (scale, anchor) => {
            const previous = currentViewBox();
            const nextScale = clampZoom(scale);

            if (anchor) {
                const rect = viewport.getBoundingClientRect();
                const relativeX = rect.width ? (anchor.clientX - rect.left) / rect.width : 0.5;
                const relativeY = rect.height ? (anchor.clientY - rect.top) / rect.height : 0.5;
                const anchorX = previous.x + previous.width * relativeX;
                const anchorY = previous.y + previous.height * relativeY;
                const nextWidth = baseViewBox.width / nextScale;
                const nextHeight = baseViewBox.height / nextScale;
                view.centerX = anchorX - (relativeX - 0.5) * nextWidth;
                view.centerY = anchorY - (relativeY - 0.5) * nextHeight;
            }

            view.scale = nextScale;
            applyViewBox();
        };

        const reset = () => {
            view.scale = 1;
            view.centerX = baseViewBox.x + baseViewBox.width / 2;
            view.centerY = baseViewBox.y + baseViewBox.height / 2;
            applyViewBox();
        };

        const pan = (deltaX, deltaY, origin) => {
            const box = currentViewBox();
            const rect = viewport.getBoundingClientRect();
            const unitsPerPixelX = rect.width ? box.width / rect.width : 1;
            const unitsPerPixelY = rect.height ? box.height / rect.height : 1;
            view.centerX = origin.x - deltaX * unitsPerPixelX;
            view.centerY = origin.y - deltaY * unitsPerPixelY;
            applyViewBox();
        };

        applyViewBox();

        return {
            get scale() {
                return view.scale;
            },
            getCenter: () => ({ x: view.centerX, y: view.centerY }),
            pan,
            reset,
            setScale,
            zoomIn: () => setScale(view.scale + 0.2),
            zoomOut: () => setScale(view.scale - 0.2)
        };
    };

    const bindDiagramPan = (viewport, controller, options = {}) => {
        let dragging = false;
        let inputMode = null;
        let dragStart = { x: 0, y: 0 };
        let dragOrigin = { x: 0, y: 0 };

        const beginPan = (mode, event) => {
            if (dragging || (event.button !== undefined && event.button !== 0)) {
                return;
            }

            event.preventDefault();
            dragging = true;
            inputMode = mode;
            dragStart = { x: event.clientX, y: event.clientY };
            dragOrigin = controller.getCenter();

            if (mode === 'pointer') {
                try {
                    viewport.setPointerCapture?.(event.pointerId);
                } catch {
                    // Some synthetic or browser-emulated pointer events cannot be captured.
                }
            }

            viewport.classList.add('is-panning');
        };

        const movePan = (mode, event) => {
            if (!dragging || inputMode !== mode) {
                return;
            }

            event.preventDefault();
            controller.pan(event.clientX - dragStart.x, event.clientY - dragStart.y, dragOrigin);
        };

        const endPan = (mode, event) => {
            if (!dragging || inputMode !== mode) {
                return;
            }

            dragging = false;
            inputMode = null;

            if (mode === 'pointer') {
                try {
                    viewport.releasePointerCapture?.(event.pointerId);
                } catch {
                    // Ignore release failures for pointers the browser did not capture.
                }
            }

            viewport.classList.remove('is-panning');
        };

        viewport.addEventListener('pointerdown', event => beginPan('pointer', event));
        viewport.addEventListener('pointermove', event => movePan('pointer', event));
        viewport.addEventListener('pointerup', event => endPan('pointer', event));
        viewport.addEventListener('pointercancel', event => endPan('pointer', event));
        viewport.addEventListener('mousedown', event => beginPan('mouse', event));
        document.addEventListener('mousemove', event => movePan('mouse', event));
        document.addEventListener('mouseup', event => endPan('mouse', event));

        if (options.wheelZoom) {
            viewport.addEventListener('wheel', event => {
                event.preventDefault();
                const next = controller.scale + (event.deltaY < 0 ? 0.12 : -0.12);
                controller.setScale(next, event);
            }, { passive: false });
        }
    };

    const openDiagramDialog = diagram => {
        const svg = diagram.querySelector('svg');
        if (!svg) {
            return;
        }

        closeDiagramDialog();

        const previousFocus = document.activeElement;
        const overlay = document.createElement('div');
        overlay.className = 'documentation-mermaid-dialog-overlay';
        overlay.setAttribute('data-documentation-mermaid-dialog', '');

        const dialog = document.createElement('div');
        dialog.className = 'documentation-mermaid-dialog';
        dialog.setAttribute('role', 'dialog');
        dialog.setAttribute('aria-modal', 'true');
        dialog.setAttribute('aria-label', 'Mermaid diagram viewer');
        dialog.tabIndex = -1;

        const header = document.createElement('div');
        header.className = 'documentation-mermaid-dialog-header';

        const title = document.createElement('div');
        title.className = 'documentation-mermaid-dialog-title';
        title.textContent = 'Mermaid diagram';

        const controls = document.createElement('div');
        controls.className = 'documentation-mermaid-dialog-controls';

        const zoomOut = createDiagramButton('Zoom out', 'bi-zoom-out', 'Zoom out', 'documentation-mermaid-dialog-button');
        const zoomReset = createDiagramButton('Reset', 'bi-aspect-ratio', 'Reset zoom', 'documentation-mermaid-dialog-button');
        const zoomIn = createDiagramButton('Zoom in', 'bi-zoom-in', 'Zoom in', 'documentation-mermaid-dialog-button');
        const close = createDiagramButton('Close', 'bi-x-lg', 'Close diagram', 'documentation-mermaid-dialog-button');

        controls.append(zoomOut, zoomReset, zoomIn, close);
        header.append(title, controls);

        const viewport = document.createElement('div');
        viewport.className = 'documentation-mermaid-dialog-viewport';

        const canvas = document.createElement('div');
        canvas.className = 'documentation-mermaid-dialog-canvas';
        const dialogSvg = svg.cloneNode(true);
        dialogSvg.removeAttribute('width');
        dialogSvg.removeAttribute('height');
        dialogSvg.style.height = '100%';
        dialogSvg.style.maxHeight = 'none';
        dialogSvg.style.maxWidth = 'none';
        dialogSvg.style.width = '100%';
        canvas.appendChild(dialogSvg);
        viewport.appendChild(canvas);

        dialog.append(header, viewport);
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        const controller = createSvgZoomController(dialogSvg, viewport, canvas);
        bindDiagramPan(viewport, controller, { wheelZoom: true });

        zoomOut.addEventListener('click', controller.zoomOut);
        zoomReset.addEventListener('click', controller.reset);
        zoomIn.addEventListener('click', controller.zoomIn);
        close.addEventListener('click', closeDiagramDialog);
        overlay.addEventListener('click', event => {
            if (event.target === overlay) {
                closeDiagramDialog();
            }
        });

        function onKeyDown(event) {
            if (event.key === 'Escape') {
                closeDiagramDialog();
            }
        }

        activeDialog = { overlay, previousFocus, onKeyDown };
        document.addEventListener('keydown', onKeyDown);
        dialog.focus();
    };

    const appendDiagramActions = diagram => {
        const svg = diagram.querySelector('svg');
        if (diagram.querySelector('[data-documentation-mermaid-toolbar]') || !svg) {
            return;
        }

        const canvas = document.createElement('div');
        canvas.className = 'documentation-mermaid-inline-canvas';
        svg.replaceWith(canvas);
        canvas.appendChild(svg);

        const controller = createSvgZoomController(svg, canvas, canvas);
        bindDiagramPan(canvas, controller);

        const toolbar = document.createElement('div');
        toolbar.className = 'documentation-mermaid-toolbar';
        toolbar.dataset.documentationMermaidToolbar = '';

        const zoomOut = createDiagramButton('Zoom out', 'bi-zoom-out', 'Zoom out');
        const zoomReset = createDiagramButton('Reset', 'bi-aspect-ratio', 'Reset zoom');
        const zoomIn = createDiagramButton('Zoom in', 'bi-zoom-in', 'Zoom in');
        const open = createDiagramButton('Open', 'bi-arrows-fullscreen', 'Open Mermaid diagram', 'documentation-mermaid-button documentation-mermaid-open');
        open.setAttribute('data-documentation-mermaid-open', '');

        zoomOut.addEventListener('click', controller.zoomOut);
        zoomReset.addEventListener('click', controller.reset);
        zoomIn.addEventListener('click', controller.zoomIn);
        open.addEventListener('click', event => {
            event.stopPropagation();
            openDiagramDialog(diagram);
        });

        toolbar.append(zoomOut, zoomReset, zoomIn, open);
        diagram.prepend(toolbar);
    };

    const showFallback = (diagram, error) => {
        const source = diagram.dataset.mermaidSource || '';
        diagram.className = 'documentation-mermaid documentation-mermaid-error';
        diagram.removeAttribute('aria-busy');
        diagram.innerHTML = '';

        const message = document.createElement('div');
        message.className = 'documentation-mermaid-error-title';
        message.textContent = 'Diagram could not be rendered.';

        const detail = document.createElement('div');
        detail.className = 'documentation-mermaid-error-detail';
        detail.textContent = error?.message || 'Review the Mermaid syntax.';

        const pre = document.createElement('pre');
        const code = document.createElement('code');
        code.textContent = source;
        pre.appendChild(code);

        diagram.appendChild(message);
        diagram.appendChild(detail);
        diagram.appendChild(pre);
    };

    const prepareDiagrams = () => {
        sourceBlocks.forEach((code, index) => {
            const pre = code.closest('pre');
            if (!pre || pre.dataset.documentationMermaidPrepared === 'true') {
                return;
            }

            const diagram = document.createElement('div');
            diagram.className = 'documentation-mermaid';
            diagram.dataset.documentationMermaid = '';
            diagram.dataset.mermaidSource = code.textContent || '';
            diagram.dataset.mermaidIndex = String(index);
            diagram.setAttribute('role', 'img');
            diagram.setAttribute('aria-label', 'Mermaid diagram');
            pre.dataset.documentationMermaidPrepared = 'true';
            pre.replaceWith(diagram);
        });

        return Array.from(reader.querySelectorAll('[data-documentation-mermaid]'));
    };

    const renderDiagram = async (mermaid, diagram, version) => {
        if (version !== renderVersion) {
            return;
        }

        const source = diagram.dataset.mermaidSource || '';
        const index = diagram.dataset.mermaidIndex || '0';
        const id = `documentation-mermaid-${Date.now()}-${index}`;
        diagram.className = 'documentation-mermaid documentation-mermaid-loading';
        diagram.setAttribute('aria-busy', 'true');
        diagram.textContent = 'Rendering diagram...';

        try {
            const result = await mermaid.render(id, source);
            if (version !== renderVersion) {
                return;
            }

            diagram.innerHTML = result.svg;
            result.bindFunctions?.(diagram);
            diagram.className = 'documentation-mermaid documentation-mermaid-ready';
            diagram.removeAttribute('aria-busy');
            appendDiagramActions(diagram);
        } catch (error) {
            if (version === renderVersion) {
                showFallback(diagram, error);
            }
        }
    };

    const renderMermaid = async () => {
        const diagrams = prepareDiagrams();
        if (!diagrams.length) {
            return;
        }

        const version = ++renderVersion;
        let mermaid;

        try {
            mermaid = await loadMermaid();
        } catch (error) {
            diagrams.forEach(diagram => showFallback(diagram, error));
            return;
        }

        mermaid.initialize({
            startOnLoad: false,
            securityLevel: 'strict',
            theme: 'base',
            themeVariables: mermaidThemeVariables(),
            flowchart: {
                htmlLabels: false,
                useMaxWidth: true
            },
            sequence: {
                useMaxWidth: true
            }
        });

        await Promise.all(diagrams.map(diagram => renderDiagram(mermaid, diagram, version)));
    };

    renderMermaid();
    document.addEventListener('knowl:theme-change', () => {
        window.setTimeout(renderMermaid, 0);
    });
}());
