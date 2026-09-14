(function () {
    const baseSelect = document.getElementById('type-base-select');
    const form = document.getElementById('event-editor-form');
    const addRootButton = document.getElementById('add-root-field');
    const arrayItemSelect = document.getElementById('array-item-type-select');
    const arrayItemTypeInput = document.getElementById('array-item-type');
    const arrayItemTypeVersionInput = document.getElementById('array-item-type-version-id');
    const allowedValuesInput = document.getElementById('type-allowed-values-json');
    const catalog = readTypeCatalog();

    if (!baseSelect || !form) {
        return;
    }

    populateArrayItemSelect();
    hydrateAllowedValues();

    function refresh() {
        const baseType = baseSelect.value;
        document.querySelectorAll('.type-constraints').forEach((node) => {
            node.classList.add('d-none');
        });

        const active = document.querySelector(`.type-constraints-${baseType}`);
        if (active) {
            active.classList.remove('d-none');
        }

        if (addRootButton) {
            addRootButton.disabled = baseType !== 'object';
        }

        syncAllowedValuesInput();
    }

    function hydrateAllowedValues() {
        document.querySelectorAll('.type-enum-chip-editor').forEach((editor) => {
            const type = editor.dataset.enumType || 'string';
            const input = editor.querySelector('.type-enum-input');
            const chips = editor.querySelector('.type-enum-chips');
            if (!input || !chips) {
                return;
            }

            if (type === baseSelect.value) {
                readAllowedValues().forEach((value) => addEnumChip(chips, value));
            }
            input.addEventListener('keydown', (event) => {
                if (event.key !== 'Enter') {
                    return;
                }

                event.preventDefault();
                const value = normalizeEnumInput(input.value, type);
                if (value === null) {
                    return;
                }

                addEnumChip(chips, value);
                input.value = '';
                syncAllowedValuesInput();
            });
        });
    }

    function readAllowedValues() {
        if (!allowedValuesInput) {
            return [];
        }

        try {
            const parsed = JSON.parse(allowedValuesInput.value || '[]');
            return Array.isArray(parsed) ? parsed : [];
        } catch (e) {
            return [];
        }
    }

    function normalizeEnumInput(value, type) {
        const trimmed = String(value || '').trim();
        if (!trimmed) {
            return null;
        }

        if (type === 'string') {
            return trimmed;
        }

        const numeric = Number(trimmed);
        if (!Number.isFinite(numeric)) {
            return null;
        }

        if (type === 'integer' && !Number.isInteger(numeric)) {
            return null;
        }

        return numeric;
    }

    function addEnumChip(container, value) {
        const normalized = String(value);
        const exists = Array.from(container.querySelectorAll('.type-enum-chip'))
            .some((chip) => chip.dataset.value === normalized);
        if (exists) {
            return;
        }

        const chip = document.createElement('span');
        chip.className = 'type-enum-chip';
        chip.dataset.value = normalized;
        chip.dataset.rawValue = JSON.stringify(value);
        chip.textContent = normalized;

        const remove = document.createElement('button');
        remove.type = 'button';
        remove.textContent = 'x';
        remove.addEventListener('click', () => {
            chip.remove();
            syncAllowedValuesInput();
        });

        chip.appendChild(remove);
        container.appendChild(chip);
    }

    function syncAllowedValuesInput() {
        if (!allowedValuesInput) {
            return;
        }

        const activeEditor = document.querySelector(`.type-constraints-${baseSelect.value} .type-enum-chip-editor`);
        if (!activeEditor) {
            allowedValuesInput.value = '[]';
            return;
        }

        const values = Array.from(activeEditor.querySelectorAll('.type-enum-chip'))
            .map((chip) => JSON.parse(chip.dataset.rawValue || 'null'))
            .filter((value) => value !== null);
        allowedValuesInput.value = JSON.stringify(values);
    }

    function readTypeCatalog() {
        const catalogNode = document.getElementById('schema-type-catalog');
        if (!catalogNode) {
            return defaultCatalog();
        }

        try {
            const parsed = JSON.parse(catalogNode.textContent || '[]');
            return Array.isArray(parsed) && parsed.length > 0 ? parsed : defaultCatalog();
        } catch (e) {
            return defaultCatalog();
        }
    }

    function defaultCatalog() {
        return ['string', 'number', 'integer', 'boolean', 'object', 'array'].map((type) => ({
            name: type,
            baseType: type,
            versionNumber: '1.0.0',
            isSystem: true
        }));
    }

    function populateArrayItemSelect() {
        if (!arrayItemSelect) {
            return;
        }

        arrayItemSelect.innerHTML = '';
        const basicGroup = document.createElement('optgroup');
        basicGroup.label = 'Basic';
        const customGroup = document.createElement('optgroup');
        customGroup.label = 'Personalizados';

        catalog.forEach((item) => {
            const option = document.createElement('option');
            option.value = item.isSystem && !item.typeVersionId ? item.baseType : item.typeVersionId;
            option.textContent = item.isSystem ? item.name : `${item.name} (${item.versionNumber})`;
            option.dataset.baseType = item.baseType;
            option.dataset.isSystem = item.isSystem ? 'true' : 'false';
            option.dataset.typeVersionId = item.typeVersionId || '';

            if (item.isSystem) {
                basicGroup.appendChild(option);
            } else {
                customGroup.appendChild(option);
            }
        });

        arrayItemSelect.appendChild(basicGroup);
        if (customGroup.children.length > 0) {
            arrayItemSelect.appendChild(customGroup);
        }

        const savedVersionId = arrayItemTypeVersionInput?.value;
        const savedType = arrayItemTypeInput?.value;
        if (savedVersionId && Array.from(arrayItemSelect.options).some((option) => option.value === savedVersionId)) {
            arrayItemSelect.value = savedVersionId;
        } else if (savedType && Array.from(arrayItemSelect.options).some((option) => option.value === savedType)) {
            arrayItemSelect.value = savedType;
        }

        syncArrayItemInputs();
    }

    function syncArrayItemInputs() {
        const selected = arrayItemSelect?.selectedOptions?.[0];
        if (!selected || !arrayItemTypeInput || !arrayItemTypeVersionInput) {
            return;
        }

        if (selected.dataset.isSystem === 'true' && !selected.dataset.typeVersionId) {
            arrayItemTypeInput.value = selected.dataset.baseType || selected.value;
            arrayItemTypeVersionInput.value = '';
            return;
        }

        arrayItemTypeInput.value = selected.dataset.baseType || '';
        arrayItemTypeVersionInput.value = selected.dataset.typeVersionId || selected.value;
    }

    baseSelect.addEventListener('change', refresh);
    if (arrayItemSelect) {
        arrayItemSelect.addEventListener('change', syncArrayItemInputs);
    }
    form.addEventListener('submit', () => {
        syncArrayItemInputs();
        syncAllowedValuesInput();
    });
    refresh();
}());
