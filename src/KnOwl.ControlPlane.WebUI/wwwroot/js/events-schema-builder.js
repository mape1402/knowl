(function () {
    const form = document.getElementById('event-editor-form');
    const rootList = document.getElementById('schema-root-fields');
    const addRootButton = document.getElementById('add-root-field');
    const template = document.getElementById('schema-field-template');
    const hiddenSchemaInput = document.getElementById('payload-schema-json');
    const catalog = readTypeCatalog();
    const metadataCatalog = readMetadataCatalog();
    const metadataModalNode = document.getElementById('field-metadata-modal');
    const fieldValidationSection = document.getElementById('field-validation-section');
    const fieldValidationFieldsNode = document.getElementById('field-validation-fields');
    const metadataFieldsNode = document.getElementById('field-metadata-fields');
    const metadataValidationNode = document.getElementById('field-metadata-validation');
    const saveMetadataButton = document.getElementById('save-field-metadata-btn');
    const metadataModal = metadataModalNode && window.bootstrap ? new bootstrap.Modal(metadataModalNode) : null;
    const objectSchemaModalNode = document.getElementById('object-schema-modal');
    const objectSchemaModalTitle = document.getElementById('object-schema-modal-title');
    const objectSchemaBreadcrumb = document.getElementById('object-schema-breadcrumb');
    const objectSchemaFieldsHost = document.getElementById('object-schema-fields-host');
    const objectSchemaAddFieldButton = document.getElementById('object-schema-add-field-btn');
    const objectSchemaBackButton = document.getElementById('object-schema-back-btn');
    const objectSchemaModal = objectSchemaModalNode && window.bootstrap ? new bootstrap.Modal(objectSchemaModalNode) : null;
    let activeMetadataField = null;
    let objectSchemaStack = [];
    let activeObjectSchemaContext = null;

    if (!form || !rootList || !addRootButton || !template || !hiddenSchemaInput) {
        return;
    }

    addRootButton.addEventListener('click', () => {
        rootList.appendChild(createFieldNode());
    });

    hydrateFromHiddenSchema();

    form.addEventListener('submit', (event) => {
        if (!validateAllMetadata()) {
            event.preventDefault();
            event.stopPropagation();
            return;
        }

        const defs = {};
        const schema = buildRootSchema(rootList, defs);
        if (Object.keys(defs).length > 0) {
            schema.$defs = defs;
        }
        hiddenSchemaInput.value = JSON.stringify(schema);
    });

    if (saveMetadataButton) {
        saveMetadataButton.addEventListener('click', saveActiveMetadata);
    }

    if (objectSchemaAddFieldButton) {
        objectSchemaAddFieldButton.addEventListener('click', () => {
            const listNode = activeObjectSchemaContext?.listNode;
            if (!listNode) {
                return;
            }

            listNode.appendChild(createFieldNode());
            updateNestedSummaries();
        });
    }

    if (objectSchemaBackButton) {
        objectSchemaBackButton.addEventListener('click', navigateObjectSchemaBack);
    }

    if (objectSchemaModalNode) {
        objectSchemaModalNode.addEventListener('hidden.bs.modal', closeObjectSchemaEditor);
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

    function readMetadataCatalog() {
        const catalogNode = document.getElementById('field-metadata-catalog');
        if (!catalogNode) {
            return [];
        }

        try {
            const parsed = JSON.parse(catalogNode.textContent || '[]');
            return Array.isArray(parsed) ? parsed : [];
        } catch (e) {
            return [];
        }
    }

    function hydrateFromHiddenSchema() {
        let schema;
        try {
            schema = JSON.parse(hiddenSchemaInput.value || '{}');
        } catch (e) {
            schema = null;
        }

        if (!schema || schema.type !== 'object' || !schema.properties || Object.keys(schema.properties).length === 0) {
            rootList.appendChild(createFieldNode());
            return;
        }

        rootList.innerHTML = '';
        const rootRequired = Array.isArray(schema.required) ? schema.required : [];
        Object.entries(schema.properties).forEach(([fieldName, definition]) => {
            rootList.appendChild(createPopulatedFieldNode(fieldName, definition, true, isRequiredField(definition, rootRequired, fieldName)));
        });

        if (rootList.children.length === 0) {
            rootList.appendChild(createFieldNode());
        }
    }

    function createFieldNode() {
        const fragment = template.content.cloneNode(true);
        const field = fragment.querySelector('.schema-field');

        const typeSelect = field.querySelector('.field-type-select');
        const removeButton = field.querySelector('.remove-field-btn');
        const metadataButton = field.querySelector('.field-metadata-btn');
        const addChildButton = field.querySelector('.add-child-field-btn');
        const editObjectFieldsButton = field.querySelector('.edit-object-fields-btn');
        const objectBuilder = field.querySelector('.schema-object-builder');
        const childList = field.querySelector('.child-fields-list');

        const arrayBuilder = field.querySelector('.schema-array-builder');
        const arrayItemTypeSelect = field.querySelector('.array-item-type-select');
        const arrayObjectBuilder = field.querySelector('.schema-array-object-builder');
        const arrayObjectList = field.querySelector('.array-object-fields-list');
        const addArrayObjectFieldButton = field.querySelector('.add-array-object-field-btn');
        const editArrayObjectFieldsButton = field.querySelector('.edit-array-object-fields-btn');
        const nestedArrayBuilder = field.querySelector('.schema-array-nested-builder');
        const nestedArrayItemList = field.querySelector('.nested-array-item-list');
        const addNestedArrayItemButton = field.querySelector('.add-nested-array-item-btn');

        populateTypeSelect(typeSelect);
        populateTypeSelect(arrayItemTypeSelect);

        removeButton.addEventListener('click', () => {
            const contextIndex = objectSchemaStack.findIndex((context) => context.ownerField === field);
            if (contextIndex >= 0) {
                objectSchemaStack.splice(contextIndex);
            }
            field.remove();
            updateNestedSummaries();
        });

        if (metadataButton) {
            metadataButton.addEventListener('click', () => {
                openMetadataModal(field);
            });
        }

        addChildButton.addEventListener('click', () => {
            childList.appendChild(createFieldNode());
            updateNestedSummaries();
        });

        addArrayObjectFieldButton.addEventListener('click', () => {
            arrayObjectList.appendChild(createFieldNode());
            updateNestedSummaries();
        });

        addNestedArrayItemButton.addEventListener('click', () => {
            nestedArrayItemList.appendChild(createFieldNodeWithoutName());
        });

        typeSelect.addEventListener('change', () => {
            updateFieldTypeUi(typeSelect, objectBuilder, arrayBuilder);
            updateNestedSummaries();
        });

        arrayItemTypeSelect.addEventListener('change', () => {
            updateArrayItemUi(arrayItemTypeSelect, arrayObjectBuilder, nestedArrayBuilder);
            updateNestedSummaries();
        });

        field.querySelector('.field-name-input')?.addEventListener('input', updateNestedSummaries);

        if (editObjectFieldsButton) {
            editObjectFieldsButton.addEventListener('click', () => {
                openObjectSchemaEditor(childList, getFieldDisplayName(field, 'Objeto'), field);
            });
        }

        if (editArrayObjectFieldsButton) {
            editArrayObjectFieldsButton.addEventListener('click', () => {
                openObjectSchemaEditor(arrayObjectList, `${getFieldDisplayName(field, 'Array')}[]`, field);
            });
        }

        updateFieldTypeUi(typeSelect, objectBuilder, arrayBuilder);
        updateArrayItemUi(arrayItemTypeSelect, arrayObjectBuilder, nestedArrayBuilder);
        updateFieldNestedSummary(field);

        return field;
    }

    function populateTypeSelect(select) {
        if (!select) {
            return;
        }

        select.innerHTML = '';
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
            option.dataset.typeId = item.typeId || '';
            option.dataset.typeVersionId = item.typeVersionId || '';
            option.dataset.typeName = item.name;
            option.dataset.versionNumber = item.versionNumber || '';
            option.dataset.jsonSchema = item.jsonSchema || '';

            if (item.isSystem) {
                basicGroup.appendChild(option);
            } else {
                customGroup.appendChild(option);
            }
        });

        select.appendChild(basicGroup);
        if (customGroup.children.length > 0) {
            select.appendChild(customGroup);
        }
    }

    function createFieldNodeWithoutName() {
        const node = createFieldNode();
        const nameContainer = node.querySelector('.field-name-input')?.closest('.col-md-3');
        const requiredContainer = node.querySelector('.field-required-input')?.closest('.col-md-2');
        if (nameContainer) {
            nameContainer.remove();
            if (requiredContainer) {
                requiredContainer.remove();
            }
            const typeCol = node.querySelector('.field-type-select')?.closest('.col-md-2');
            const descCol = node.querySelector('.field-description-input')?.closest('.col-md-4');
            if (typeCol) {
                typeCol.className = 'col-md-4';
            }
            if (descCol) {
                descCol.className = 'col-md-7';
            }
        }
        node.dataset.noName = '1';
        return node;
    }

    function createPopulatedFieldNode(name, definition, includeName, isRequired) {
        const node = includeName === false ? createFieldNodeWithoutName() : createFieldNode();

        const typeSelect = node.querySelector('.field-type-select');
        const descriptionInput = node.querySelector('.field-description-input');
        const nameInput = node.querySelector('.field-name-input');
        const requiredInput = node.querySelector('.field-required-input');

        if (includeName !== false && nameInput) {
            nameInput.value = name || '';
        }

        if (requiredInput) {
            requiredInput.checked = Boolean(isRequired);
        }

        if (descriptionInput && definition && typeof definition.description === 'string') {
            descriptionInput.value = definition.description;
        }

        if (definition?.metadata && typeof definition.metadata === 'object') {
            setFieldMetadata(node, definition.metadata);
        }

        setFieldValidation(node, readFieldValidationFromDefinition(definition));

        setSelectFromDefinition(typeSelect, definition);
        const safeType = getSelectedBaseType(typeSelect);

        if (safeType === 'object' && isSystemSelection(typeSelect) && definition?.properties) {
            const childList = node.querySelector('.child-fields-list');
            const childRequired = Array.isArray(definition.required) ? definition.required : [];
            if (childList) {
                Object.entries(definition.properties).forEach(([childName, childDefinition]) => {
                    childList.appendChild(createPopulatedFieldNode(childName, childDefinition, true, isRequiredField(childDefinition, childRequired, childName)));
                });
            }
        }

        if (safeType === 'array' && isSystemSelection(typeSelect) && definition?.items) {
            const arrayItemTypeSelect = node.querySelector('.array-item-type-select');
            setSelectFromDefinition(arrayItemTypeSelect, definition.items);
            const itemType = getSelectedBaseType(arrayItemTypeSelect);

            if (itemType === 'object' && definition.items.properties) {
                const arrayObjectList = node.querySelector('.array-object-fields-list');
                const objectRequired = Array.isArray(definition.items.required) ? definition.items.required : [];
                if (arrayObjectList) {
                    Object.entries(definition.items.properties).forEach(([itemName, itemDefinition]) => {
                        arrayObjectList.appendChild(createPopulatedFieldNode(itemName, itemDefinition, true, isRequiredField(itemDefinition, objectRequired, itemName)));
                    });
                }
            }

            if (itemType === 'array' && definition.items.items) {
                const nestedArrayList = node.querySelector('.nested-array-item-list');
                if (nestedArrayList) {
                    nestedArrayList.appendChild(createPopulatedFieldNode('', definition.items.items, false));
                }
            }
        }

        updateFieldNestedSummary(node);
        return node;
    }

    function setSelectFromDefinition(select, definition) {
        if (!select) {
            return;
        }

        const typeVersionId = definition?.typeVersionId;
        if (typeVersionId && Array.from(select.options).some((option) => option.value === typeVersionId)) {
            select.value = typeVersionId;
            select.dispatchEvent(new Event('change'));
            return;
        }

        const matchingSystemOption = findMatchingSystemOption(select, definition);
        if (matchingSystemOption) {
            select.value = matchingSystemOption.value;
            select.dispatchEvent(new Event('change'));
            return;
        }

        const type = definition?.type || 'string';
        select.value = type;
        select.dispatchEvent(new Event('change'));
    }

    function findMatchingSystemOption(select, definition) {
        if (!definition || typeof definition !== 'object') {
            return null;
        }

        return Array.from(select?.options || []).find((option) => {
            if (option.dataset.isSystem !== 'true' || !option.dataset.typeVersionId) {
                return false;
            }

            const schema = parseCustomSchema(option.dataset.jsonSchema);
            return schema.type === definition.type &&
                schema.pattern === definition.pattern &&
                option.dataset.typeName;
        });
    }

    function updateFieldTypeUi(select, objectBuilder, arrayBuilder) {
        const type = getSelectedBaseType(select);
        const showNestedBuilders = isSystemSelection(select);
        const field = select?.closest('.schema-field');
        const editObjectButton = field?.querySelector(':scope > .schema-field-main .edit-object-fields-btn');
        const editArrayObjectButton = field?.querySelector(':scope > .schema-field-main .edit-array-object-fields-btn');

        objectBuilder.classList.toggle('d-none', !showNestedBuilders || type !== 'object');
        arrayBuilder.classList.toggle('d-none', !showNestedBuilders || type !== 'array');
        editObjectButton?.classList.toggle('d-none', !showNestedBuilders || type !== 'object');
        editArrayObjectButton?.classList.add('d-none');
    }

    function updateArrayItemUi(select, arrayObjectBuilder, nestedArrayBuilder) {
        const itemType = getSelectedBaseType(select);
        const showNestedBuilders = isSystemSelection(select);
        const field = select?.closest('.schema-field');
        const rootType = getSelectedBaseType(field?.querySelector(':scope > .schema-field-main .field-type-select'));
        const editArrayObjectButton = field?.querySelector(':scope > .schema-field-main .edit-array-object-fields-btn');

        arrayObjectBuilder.classList.toggle('d-none', !showNestedBuilders || itemType !== 'object');
        nestedArrayBuilder.classList.toggle('d-none', !showNestedBuilders || itemType !== 'array');
        editArrayObjectButton?.classList.toggle('d-none', rootType !== 'array' || !showNestedBuilders || itemType !== 'object');
    }

    function buildRootSchema(listNode, defs) {
        const objectDefinition = buildObjectDefinition(listNode, defs);
        return {
            type: 'object',
            properties: objectDefinition.properties
        };
    }

    function buildObjectDefinition(listNode, defs) {
        const properties = {};
        const fields = listNode.querySelectorAll(':scope > .schema-field');

        fields.forEach((field) => {
            const name = field.querySelector('.field-name-input')?.value.trim();
            if (!name) {
                return;
            }

            properties[name] = buildFieldDefinition(field, defs);
        });

        return { properties: properties };
    }

    function buildArrayItemDefinition(listNode, defs) {
        const firstField = listNode.querySelector(':scope > .schema-field');
        if (!firstField) {
            return { type: 'string' };
        }

        return buildFieldDefinition(firstField, defs);
    }

    function buildFieldDefinition(field, defs) {
        const typeSelect = field.querySelector('.field-type-select');
        const description = field.querySelector('.field-description-input')?.value.trim();
        const definition = buildSelectedTypeDefinition(typeSelect, defs);

        if (description) {
            definition.description = description;
        }

        if (field.querySelector('.field-required-input')?.checked) {
            definition.required = true;
        }

        const metadata = getFieldMetadata(field);
        if (Object.keys(metadata).length > 0) {
            definition.metadata = metadata;
        }

        const type = getSelectedBaseType(typeSelect);
        applyFieldValidationToDefinition(definition, getFieldValidation(field), type);
        if (type === 'object' && isSystemSelection(typeSelect)) {
            const childList = field.querySelector('.child-fields-list');
            const childDefinition = childList ? buildObjectDefinition(childList, defs) : { properties: {} };
            definition.properties = childDefinition.properties;
        }

        if (type === 'array' && isSystemSelection(typeSelect)) {
            const itemSelect = field.querySelector('.array-item-type-select');
            const itemType = getSelectedBaseType(itemSelect);
            definition.items = buildSelectedTypeDefinition(itemSelect, defs);

            if (itemType === 'object' && isSystemSelection(itemSelect)) {
                const objectFields = field.querySelector('.array-object-fields-list');
                const objectDefinition = objectFields ? buildObjectDefinition(objectFields, defs) : { properties: {} };
                definition.items.properties = objectDefinition.properties;
            }

            if (itemType === 'array' && isSystemSelection(itemSelect)) {
                const nestedList = field.querySelector('.nested-array-item-list');
                definition.items = {
                    type: 'array',
                    items: buildArrayItemDefinition(nestedList, defs)
                };
            }
        }

        return definition;
    }

    function buildSelectedTypeDefinition(select, defs) {
        const selected = select?.selectedOptions?.[0];
        if (!selected || selected.dataset.isSystem === 'true') {
            const schema = parseCustomSchema(selected?.dataset.jsonSchema);
            return Object.keys(schema).length > 0
                ? schema
                : { type: selected?.dataset.baseType || select?.value || 'string' };
        }

        const refName = `${selected.dataset.typeName}@${selected.dataset.versionNumber}`;
        const schema = parseCustomSchema(selected.dataset.jsonSchema);
        addDefinition(defs, refName, schema);

        return {
            $ref: `#/$defs/${refName}`,
            typeId: selected.dataset.typeId,
            typeVersionId: selected.dataset.typeVersionId
        };
    }

    function openObjectSchemaEditor(listNode, title, ownerField) {
        if (!objectSchemaModal || !objectSchemaFieldsHost || !listNode) {
            return;
        }

        const context = createObjectSchemaContext(listNode, title, ownerField);
        objectSchemaStack.push(context);
        showObjectSchemaContext(context);
        objectSchemaModal.show();
    }

    function createObjectSchemaContext(listNode, title, ownerField) {
        return {
            listNode,
            title,
            ownerField,
            homeParent: listNode.parentElement,
            homeNextSibling: listNode.nextSibling
        };
    }

    function showObjectSchemaContext(context) {
        if (!context || !objectSchemaFieldsHost) {
            return;
        }

        if (activeObjectSchemaContext && activeObjectSchemaContext !== context) {
            restoreObjectSchemaContext(activeObjectSchemaContext);
        }

        activeObjectSchemaContext = context;
        objectSchemaFieldsHost.innerHTML = '';
        objectSchemaFieldsHost.appendChild(context.listNode);
        context.listNode.classList.add('schema-fields-list');

        if (objectSchemaModalTitle) {
            objectSchemaModalTitle.textContent = context.title || 'Object Properties';
        }

        renderObjectSchemaBreadcrumb();
        updateObjectSchemaBackButton();
        updateNestedSummaries();
    }

    function restoreObjectSchemaContext(context) {
        if (!context?.listNode || !context.homeParent) {
            return;
        }

        if (context.homeNextSibling && context.homeNextSibling.parentElement === context.homeParent) {
            context.homeParent.insertBefore(context.listNode, context.homeNextSibling);
        } else {
            context.homeParent.appendChild(context.listNode);
        }
    }

    function navigateObjectSchemaBack() {
        if (objectSchemaStack.length <= 1) {
            objectSchemaModal?.hide();
            return;
        }

        const current = objectSchemaStack.pop();
        restoreObjectSchemaContext(current);
        showObjectSchemaContext(objectSchemaStack[objectSchemaStack.length - 1]);
    }

    function closeObjectSchemaEditor() {
        if (activeObjectSchemaContext) {
            restoreObjectSchemaContext(activeObjectSchemaContext);
        }

        activeObjectSchemaContext = null;
        objectSchemaStack = [];
        if (objectSchemaFieldsHost) {
            objectSchemaFieldsHost.innerHTML = '';
        }
        updateNestedSummaries();
    }

    function renderObjectSchemaBreadcrumb() {
        if (!objectSchemaBreadcrumb) {
            return;
        }

        objectSchemaBreadcrumb.innerHTML = '';
        objectSchemaStack.forEach((context, index) => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = index === objectSchemaStack.length - 1 ? 'active' : '';
            button.textContent = context.title || 'Object';
            button.disabled = index === objectSchemaStack.length - 1;
            button.addEventListener('click', () => {
                while (objectSchemaStack.length - 1 > index) {
                    restoreObjectSchemaContext(objectSchemaStack.pop());
                }
                showObjectSchemaContext(objectSchemaStack[index]);
            });
            objectSchemaBreadcrumb.appendChild(button);
        });
    }

    function updateObjectSchemaBackButton() {
        if (!objectSchemaBackButton) {
            return;
        }

        objectSchemaBackButton.disabled = objectSchemaStack.length <= 1;
    }

    function updateNestedSummaries() {
        rootList.querySelectorAll('.schema-field').forEach(updateFieldNestedSummary);
        if (activeObjectSchemaContext?.listNode) {
            activeObjectSchemaContext.listNode.querySelectorAll('.schema-field').forEach(updateFieldNestedSummary);
        }
    }

    function updateFieldNestedSummary(field) {
        if (!field) {
            return;
        }

        const objectSummary = field.querySelector(':scope > .schema-object-builder .object-summary');
        const childList = field.querySelector(':scope > .schema-object-builder .child-fields-list');
        if (objectSummary && childList) {
            objectSummary.textContent = getPropertiesSummary(childList);
        }

        const arrayObjectSummary = field.querySelector(':scope > .schema-array-builder .array-object-summary');
        const arrayObjectList = field.querySelector(':scope > .schema-array-builder .array-object-fields-list');
        if (arrayObjectSummary && arrayObjectList) {
            arrayObjectSummary.textContent = getPropertiesSummary(arrayObjectList);
        }
    }

    function getPropertiesSummary(listNode) {
        const fields = Array.from(listNode?.querySelectorAll(':scope > .schema-field') || []);
        const namedFields = fields
            .map((field) => field.querySelector('.field-name-input')?.value.trim())
            .filter(Boolean);

        if (namedFields.length === 0) {
            return 'No properties configured';
        }

        const preview = namedFields.slice(0, 3).join(', ');
        const suffix = namedFields.length > 3 ? ` +${namedFields.length - 3}` : '';
        return `${namedFields.length} ${namedFields.length === 1 ? 'property' : 'properties'}: ${preview}${suffix}`;
    }

    function getFieldDisplayName(field, fallback) {
        return field.querySelector('.field-name-input')?.value.trim()
            || field.querySelector('.field-description-input')?.value.trim()
            || fallback;
    }

    function parseCustomSchema(jsonSchema) {
        try {
            return JSON.parse(jsonSchema || '{}');
        } catch (e) {
            return {};
        }
    }

    function openMetadataModal(field) {
        if (!metadataModal || !metadataFieldsNode) {
            return;
        }

        activeMetadataField = field;
        hideMetadataValidation();
        renderFieldValidationInputs(field);
        metadataFieldsNode.innerHTML = '';

        const values = getFieldMetadata(field);
        metadataCatalog.forEach((definition) => {
            metadataFieldsNode.appendChild(createMetadataInput(definition, values[definition.key]));
        });

        metadataModal.show();
    }

    function renderFieldValidationInputs(field) {
        if (!fieldValidationSection || !fieldValidationFieldsNode) {
            return;
        }

        fieldValidationFieldsNode.innerHTML = '';
        const type = getSelectedBaseType(field.querySelector('.field-type-select'));
        const values = getFieldValidation(field);
        const controls = getFieldValidationControls(type);

        fieldValidationSection.classList.toggle('d-none', controls.length === 0);
        controls.forEach((control) => {
            fieldValidationFieldsNode.appendChild(createFieldValidationInput(control, values[control.key]));
        });
    }

    function getFieldValidationControls(type) {
        if (type === 'string') {
            return [
                { key: 'minLength', label: 'Min length', inputType: 'number', step: '1' },
                { key: 'maxLength', label: 'Max length', inputType: 'number', step: '1' },
                { key: 'pattern', label: 'REGEX', inputType: 'text' },
                { key: 'enum', label: 'Valores permitidos', inputType: 'text', isList: true }
            ];
        }

        if (type === 'number' || type === 'integer') {
            return [
                { key: 'minimum', label: 'Min', inputType: 'number', step: type === 'integer' ? '1' : 'any' },
                { key: 'maximum', label: 'Max', inputType: 'number', step: type === 'integer' ? '1' : 'any' },
                { key: 'enum', label: 'Valores permitidos', inputType: 'number', step: type === 'integer' ? '1' : 'any', isList: true }
            ];
        }

        if (type === 'array') {
            return [
                { key: 'minItems', label: 'Min items', inputType: 'number', step: '1' },
                { key: 'maxItems', label: 'Max items', inputType: 'number', step: '1' }
            ];
        }

        return [];
    }

    function createFieldValidationInput(control, value) {
        const wrapper = document.createElement('div');
        wrapper.className = control.isList ? 'col-12' : 'col-md-4';

        const label = document.createElement('label');
        label.className = 'form-label';
        label.textContent = control.label;
        wrapper.appendChild(label);

        if (control.isList) {
            wrapper.appendChild(createFieldValidationListInput(control, value));
            const help = document.createElement('small');
            help.className = 'text-muted d-block mt-1';
            help.textContent = 'Presiona Enter para agregar cada valor.';
            wrapper.appendChild(help);
            return wrapper;
        }

        const input = document.createElement('input');
        input.className = 'form-control field-validation-input';
        input.type = control.inputType;
        input.dataset.validationKey = control.key;
        if (control.step) {
            input.step = control.step;
        }
        if (value !== undefined && value !== null) {
            input.value = String(value);
        }
        wrapper.appendChild(input);
        return wrapper;
    }

    function createFieldValidationListInput(control, value) {
        const listWrapper = document.createElement('div');
        listWrapper.className = 'field-validation-chip-editor';

        const chips = document.createElement('div');
        chips.className = 'field-validation-chips';
        listWrapper.appendChild(chips);

        const input = document.createElement('input');
        input.type = control.inputType;
        input.className = 'form-control field-validation-list-input';
        input.dataset.validationKey = control.key;
        input.dataset.validationList = 'true';
        if (control.step) {
            input.step = control.step;
        }
        listWrapper.appendChild(input);

        const addChip = (rawValue) => {
            const text = String(rawValue || '').trim();
            if (!text || getChipValues(chips).includes(text)) {
                return;
            }

            const chip = document.createElement('span');
            chip.className = 'field-validation-chip';
            chip.dataset.value = text;
            chip.textContent = text;

            const remove = document.createElement('button');
            remove.type = 'button';
            remove.textContent = 'x';
            remove.addEventListener('click', () => chip.remove());
            chip.appendChild(remove);
            chips.appendChild(chip);
        };

        (Array.isArray(value) ? value : []).forEach(addChip);
        input.addEventListener('keydown', (event) => {
            if (event.key !== 'Enter') {
                return;
            }

            event.preventDefault();
            addChip(input.value);
            input.value = '';
        });

        return listWrapper;
    }

    function collectFieldValidationFromModal(field) {
        const values = {};
        const errors = [];
        const type = getSelectedBaseType(field.querySelector('.field-type-select'));

        fieldValidationFieldsNode?.querySelectorAll('.field-validation-input').forEach((input) => {
            const key = input.dataset.validationKey;
            const rawValue = input.value.trim();
            if (!rawValue) {
                return;
            }

            const value = key === 'pattern' ? rawValue : Number(rawValue);
            if (key !== 'pattern' && (!Number.isFinite(value) || (type === 'integer' && !Number.isInteger(value)))) {
                errors.push(`${key} debe ser numerico${type === 'integer' ? ' entero' : ''}.`);
                return;
            }

            values[key] = value;
        });

        fieldValidationFieldsNode?.querySelectorAll('.field-validation-chip-editor').forEach((editor) => {
            const input = editor.querySelector('.field-validation-list-input');
            const key = input?.dataset.validationKey;
            const chips = editor.querySelector('.field-validation-chips');
            const chipValues = getChipValues(chips);
            if (!key || chipValues.length === 0) {
                return;
            }

            values[key] = type === 'string' ? chipValues : chipValues.map((item) => Number(item));
            if (type !== 'string' && values[key].some((item) => !Number.isFinite(item) || (type === 'integer' && !Number.isInteger(item)))) {
                errors.push(`${key} solo acepta valores numericos${type === 'integer' ? ' enteros' : ''}.`);
            }
        });

        if (values.minLength !== undefined && values.maxLength !== undefined && values.minLength > values.maxLength) {
            errors.push('Min length no puede ser mayor que Max length.');
        }
        if (values.minimum !== undefined && values.maximum !== undefined && values.minimum > values.maximum) {
            errors.push('Min no puede ser mayor que Max.');
        }
        if (values.minItems !== undefined && values.maxItems !== undefined && values.minItems > values.maxItems) {
            errors.push('Min items no puede ser mayor que Max items.');
        }
        if (values.pattern) {
            try {
                new RegExp(values.pattern);
            } catch (e) {
                errors.push('Invalid regex.');
            }
        }

        return { values, errors };
    }

    function getChipValues(container) {
        return Array.from(container?.querySelectorAll('.field-validation-chip') || [])
            .map((chip) => chip.dataset.value)
            .filter(Boolean);
    }

    function createMetadataInput(definition, value) {
        const wrapper = document.createElement('div');
        wrapper.className = 'col-md-6';

        const label = document.createElement('label');
        label.className = 'form-label';
        label.textContent = definition.isRequired ? `${definition.name} *` : definition.name;
        wrapper.appendChild(label);

        const input = createMetadataControl(definition, value);
        wrapper.appendChild(input);

        if (definition.description) {
            const help = document.createElement('small');
            help.className = 'text-muted d-block mt-1';
            help.textContent = definition.description;
            wrapper.appendChild(help);
        }

        return wrapper;
    }

    function createMetadataControl(definition, value) {
        const allowedValues = Array.isArray(definition.validation?.allowedValues) ? definition.validation.allowedValues : [];
        const input = allowedValues.length > 0 && definition.dataType !== 'boolean'
            ? createAllowedValuesSelect(allowedValues, value)
            : createTypedInput(definition, value);

        input.dataset.metadataKey = definition.key;
        input.dataset.metadataType = definition.dataType;
        input.dataset.metadataRequired = definition.isRequired ? 'true' : 'false';
        input.dataset.metadataValidation = JSON.stringify(definition.validation || {});
        return input;
    }

    function createAllowedValuesSelect(allowedValues, value) {
        const select = document.createElement('select');
        select.className = 'form-control field-metadata-input';
        const emptyOption = document.createElement('option');
        emptyOption.value = '';
        emptyOption.textContent = 'Select a value';
        select.appendChild(emptyOption);

        allowedValues.forEach((allowedValue) => {
            const option = document.createElement('option');
            option.value = String(allowedValue);
            option.textContent = String(allowedValue);
            select.appendChild(option);
        });

        if (value !== undefined && value !== null) {
            select.value = String(value);
        }

        return select;
    }

    function createTypedInput(definition, value) {
        if (definition.dataType === 'boolean') {
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.className = 'form-check-input field-metadata-input d-block';
            checkbox.checked = value === true;
            checkbox.dataset.metadataHasValue = value !== undefined ? 'true' : 'false';
            checkbox.addEventListener('change', () => {
                checkbox.dataset.metadataHasValue = 'true';
            });
            return checkbox;
        }

        const input = document.createElement('input');
        input.className = 'form-control field-metadata-input';
        input.type = definition.dataType === 'date' ? 'date' : definition.dataType === 'string' ? 'text' : 'number';
        if (definition.dataType === 'integer') {
            input.step = '1';
        }
        if (definition.dataType === 'number') {
            input.step = 'any';
        }

        applyMetadataValidationAttributes(input, definition);
        if (value !== undefined && value !== null) {
            input.value = String(value);
        }

        return input;
    }

    function applyMetadataValidationAttributes(input, definition) {
        const validation = definition.validation || {};
        if (definition.dataType === 'string') {
            if (validation.minLength !== undefined) input.minLength = Number(validation.minLength);
            if (validation.maxLength !== undefined) input.maxLength = Number(validation.maxLength);
            if (validation.pattern) input.pattern = validation.pattern;
        }

        if (definition.dataType === 'number' || definition.dataType === 'integer' || definition.dataType === 'date') {
            if (validation.minimum !== undefined) input.min = String(validation.minimum);
            if (validation.maximum !== undefined) input.max = String(validation.maximum);
        }
    }

    function saveActiveMetadata() {
        if (!activeMetadataField) {
            return;
        }

        const validationResult = collectFieldValidationFromModal(activeMetadataField);
        const metadataResult = collectMetadataFromModal();
        const errors = [...validationResult.errors, ...metadataResult.errors];
        if (errors.length > 0) {
            showMetadataValidation(errors);
            return;
        }

        setFieldValidation(activeMetadataField, validationResult.values);
        setFieldMetadata(activeMetadataField, metadataResult.values);
        metadataModal?.hide();
    }

    function collectMetadataFromModal() {
        const values = {};
        const errors = [];

        metadataFieldsNode?.querySelectorAll('.field-metadata-input').forEach((input) => {
            const key = input.dataset.metadataKey;
            const type = input.dataset.metadataType || 'string';
            const isRequired = input.dataset.metadataRequired === 'true';
            const validation = parseJson(input.dataset.metadataValidation, {});
            const rawValue = readMetadataRawValue(input, type, isRequired);

            if (isEmptyMetadataValue(rawValue)) {
                if (isRequired) {
                    errors.push(`Captura ${key}.`);
                }
                return;
            }

            const converted = convertMetadataValue(rawValue, type);
            const validationError = validateMetadataValue(converted, type, validation, key);
            if (validationError) {
                errors.push(validationError);
                return;
            }

            values[key] = converted;
        });

        return { isValid: errors.length === 0, errors, values };
    }

    function readMetadataRawValue(input, type, isRequired) {
        if (type === 'boolean') {
            if (input.checked || input.dataset.metadataHasValue === 'true' || isRequired) {
                return input.checked;
            }

            return '';
        }

        return input.value.trim();
    }

    function convertMetadataValue(value, type) {
        if (type === 'boolean') {
            return Boolean(value);
        }

        if (type === 'integer') {
            return Number.parseInt(value, 10);
        }

        if (type === 'number') {
            return Number.parseFloat(value);
        }

        return value;
    }

    function validateMetadataValue(value, type, validation, key) {
        const allowedValues = Array.isArray(validation.allowedValues) ? validation.allowedValues.map((x) => String(x)) : [];
        if (allowedValues.length > 0 && !allowedValues.includes(String(value))) {
            return `${key} debe ser uno de los valores permitidos.`;
        }

        if (type === 'string') {
            if (validation.minLength !== undefined && value.length < Number(validation.minLength)) return `${key} debe tener al menos ${validation.minLength} caracteres.`;
            if (validation.maxLength !== undefined && value.length > Number(validation.maxLength)) return `${key} debe tener maximo ${validation.maxLength} caracteres.`;
            if (validation.pattern) {
                try {
                    if (!(new RegExp(validation.pattern).test(value))) return `${key} no cumple el regex configurado.`;
                } catch (e) {
                    return `${key} has an invalid configured regex.`;
                }
            }
        }

        if (type === 'number' || type === 'integer') {
            if (Number.isNaN(value)) return `${key} debe ser numerico.`;
            if (type === 'integer' && !Number.isInteger(value)) return `${key} debe ser entero.`;
            if (validation.minimum !== undefined && value < Number(validation.minimum)) return `${key} debe ser mayor o igual a ${validation.minimum}.`;
            if (validation.maximum !== undefined && value > Number(validation.maximum)) return `${key} debe ser menor o igual a ${validation.maximum}.`;
        }

        if (type === 'date') {
            if (validation.minimum && value < validation.minimum) return `${key} debe ser mayor o igual a ${validation.minimum}.`;
            if (validation.maximum && value > validation.maximum) return `${key} debe ser menor o igual a ${validation.maximum}.`;
        }

        return null;
    }

    function validateAllMetadata() {
        const invalidRequired = [];
        rootList.querySelectorAll('.schema-field').forEach((field) => {
            const metadata = getFieldMetadata(field);
            metadataCatalog.forEach((definition) => {
                if (definition.isRequired && isEmptyMetadataValue(metadata[definition.key])) {
                    const name = field.querySelector('.field-name-input')?.value.trim() || '(item array)';
                    invalidRequired.push(`${name}: ${definition.name}`);
                }
            });
        });

        if (invalidRequired.length === 0) {
            return true;
        }

        alert(`Falta metadata requerida:\n${invalidRequired.join('\n')}`);
        return false;
    }

    function setFieldMetadata(field, metadata) {
        field.dataset.metadataValues = JSON.stringify(metadata || {});
        updateMetadataButtonState(field);
    }

    function getFieldMetadata(field) {
        return parseJson(field.dataset.metadataValues, {});
    }

    function setFieldValidation(field, validation) {
        field.dataset.validationValues = JSON.stringify(validation || {});
        updateMetadataButtonState(field);
    }

    function getFieldValidation(field) {
        return parseJson(field.dataset.validationValues, {});
    }

    function readFieldValidationFromDefinition(definition) {
        const validation = {};
        ['minLength', 'maxLength', 'pattern', 'minimum', 'maximum', 'minItems', 'maxItems', 'enum'].forEach((key) => {
            if (definition && definition[key] !== undefined) {
                validation[key] = definition[key];
            }
        });
        return validation;
    }

    function applyFieldValidationToDefinition(definition, validation, type) {
        if (!definition || !validation || typeof validation !== 'object') {
            return;
        }

        getAllowedFieldValidationKeys(type).forEach((key) => {
            if (validation[key] !== undefined && validation[key] !== null && validation[key] !== '') {
                definition[key] = validation[key];
            }
        });
    }

    function getAllowedFieldValidationKeys(type) {
        if (type === 'string') {
            return ['minLength', 'maxLength', 'pattern', 'enum'];
        }
        if (type === 'number' || type === 'integer') {
            return ['minimum', 'maximum', 'enum'];
        }
        if (type === 'array') {
            return ['minItems', 'maxItems'];
        }
        return [];
    }

    function updateMetadataButtonState(field) {
        const button = field.querySelector('.field-metadata-btn');
        if (!button) {
            return;
        }

        const hasValues = Object.keys(getFieldMetadata(field)).length > 0 || Object.keys(getFieldValidation(field)).length > 0;
        button.classList.toggle('btn-primary', hasValues);
        button.classList.toggle('btn-secondary', !hasValues);
    }

    function showMetadataValidation(errors) {
        if (!metadataValidationNode) {
            return;
        }

        metadataValidationNode.innerHTML = errors.join('<br />');
        metadataValidationNode.classList.remove('d-none');
    }

    function hideMetadataValidation() {
        if (!metadataValidationNode) {
            return;
        }

        metadataValidationNode.textContent = '';
        metadataValidationNode.classList.add('d-none');
    }

    function isEmptyMetadataValue(value) {
        return value === undefined || value === null || value === '';
    }

    function parseJson(value, fallback) {
        try {
            return JSON.parse(value || '');
        } catch (e) {
            return fallback;
        }
    }

    function addDefinition(defs, refName, schema) {
        if (!refName || !schema || typeof schema !== 'object') {
            return;
        }

        if (!Object.prototype.hasOwnProperty.call(defs, refName)) {
            defs[refName] = cloneSchemaWithoutDefs(schema);
        }

        if (schema.$defs && typeof schema.$defs === 'object') {
            Object.entries(schema.$defs).forEach(([nestedRefName, nestedSchema]) => {
                addDefinition(defs, nestedRefName, nestedSchema);
            });
        }
    }

    function cloneSchemaWithoutDefs(schema) {
        const clone = normalizeRequiredFields(schema);
        delete clone.$defs;
        return clone;
    }

    function normalizeRequiredFields(schema) {
        if (!schema || typeof schema !== 'object') {
            return schema;
        }

        if (Array.isArray(schema)) {
            return schema.map((item) => normalizeRequiredFields(item));
        }

        const clone = {};
        Object.entries(schema).forEach(([key, value]) => {
            clone[key] = normalizeRequiredFields(value);
        });

        if (clone.properties && typeof clone.properties === 'object' && Array.isArray(clone.required)) {
            clone.required.forEach((fieldName) => {
                if (clone.properties[fieldName] && typeof clone.properties[fieldName] === 'object') {
                    clone.properties[fieldName].required = true;
                }
            });
            delete clone.required;
        }

        return clone;
    }

    function isRequiredField(definition, legacyRequired, fieldName) {
        return definition?.required === true || legacyRequired.includes(fieldName);
    }

    function getSelectedBaseType(select) {
        return select?.selectedOptions?.[0]?.dataset.baseType || select?.value || 'string';
    }

    function isSystemSelection(select) {
        return (select?.selectedOptions?.[0]?.dataset.isSystem || 'true') === 'true';
    }
}());

