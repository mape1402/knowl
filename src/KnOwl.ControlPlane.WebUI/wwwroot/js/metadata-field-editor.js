(function () {
    const dataTypeSelect = document.getElementById('metadata-data-type');
    const allowedValuesHidden = document.getElementById('allowed-values-hidden');
    const allowedValueInput = document.getElementById('allowed-value-input');
    const allowedValuesChips = document.getElementById('allowed-values-chips');
    let allowedValues = readInitialAllowedValues();

    if (!dataTypeSelect) {
        return;
    }

    function refreshValidationFields() {
        const type = dataTypeSelect.value;
        document.querySelectorAll('.metadata-validation').forEach((node) => {
            node.classList.add('d-none');
        });

        document.querySelectorAll(`.metadata-validation-${type}`).forEach((node) => {
            node.classList.remove('d-none');
        });

        if (type !== 'boolean' && type !== 'date') {
            document.querySelectorAll('.metadata-validation-allowed-values').forEach((node) => {
                node.classList.remove('d-none');
            });
        } else {
            allowedValues = [];
            syncAllowedValues();
        }

        refreshAllowedValueInput(type);
    }

    function readInitialAllowedValues() {
        if (!allowedValuesHidden?.value) {
            return [];
        }

        return allowedValuesHidden.value
            .split(/\r?\n/)
            .map((value) => value.trim())
            .filter((value, index, values) => value && values.indexOf(value) === index);
    }

    function refreshAllowedValueInput(type) {
        if (!allowedValueInput) {
            return;
        }

        allowedValueInput.value = '';
        allowedValueInput.type = type === 'number' || type === 'integer' ? 'number' : 'text';
        allowedValueInput.step = type === 'integer' ? '1' : type === 'number' ? 'any' : '';
        allowedValueInput.disabled = type === 'boolean' || type === 'date';
    }

    function addAllowedValue() {
        if (!allowedValueInput) {
            return;
        }

        const value = normalizeAllowedValue(allowedValueInput.value, dataTypeSelect.value);
        if (!value || allowedValues.includes(value)) {
            allowedValueInput.value = '';
            return;
        }

        allowedValues.push(value);
        allowedValueInput.value = '';
        syncAllowedValues();
    }

    function normalizeAllowedValue(value, type) {
        const trimmed = value.trim();
        if (!trimmed) {
            return '';
        }

        if (type === 'integer') {
            const parsed = Number.parseInt(trimmed, 10);
            return Number.isInteger(Number(trimmed)) ? String(parsed) : '';
        }

        if (type === 'number') {
            const parsed = Number.parseFloat(trimmed);
            return Number.isNaN(parsed) ? '' : String(parsed);
        }

        return trimmed;
    }

    function removeAllowedValue(value) {
        allowedValues = allowedValues.filter((item) => item !== value);
        syncAllowedValues();
    }

    function syncAllowedValues() {
        if (allowedValuesHidden) {
            allowedValuesHidden.value = allowedValues.join('\n');
        }

        renderAllowedValueChips();
    }

    function renderAllowedValueChips() {
        if (!allowedValuesChips) {
            return;
        }

        allowedValuesChips.innerHTML = '';
        allowedValues.forEach((value) => {
            const chip = document.createElement('span');
            chip.className = 'badge rounded-pill text-bg-primary d-inline-flex align-items-center gap-1';
            chip.textContent = value;

            const remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'btn-close btn-close-white';
            remove.style.fontSize = '0.55rem';
            remove.setAttribute('aria-label', `Delete ${value}`);
            remove.addEventListener('click', () => removeAllowedValue(value));

            chip.appendChild(remove);
            allowedValuesChips.appendChild(chip);
        });
    }

    dataTypeSelect.addEventListener('change', refreshValidationFields);
    allowedValueInput?.addEventListener('keydown', (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            addAllowedValue();
        }
    });

    syncAllowedValues();
    refreshValidationFields();
}());

