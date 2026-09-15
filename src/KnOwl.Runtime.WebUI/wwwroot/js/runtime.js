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
