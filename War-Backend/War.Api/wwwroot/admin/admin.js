const state = {
    overview: null,
    options: null,
    selectedClassKey: null,
    selectedRecordId: null,
    detail: null,
    draft: null,
    preview: null,
    compare: null,
    compareTargetRecordId: null,
    metadataEntries: [],
    ascensionMeta: {},
    isNew: false,
    isBusy: false
};

const elements = {
    classList: document.getElementById('class-list'),
    skillList: document.getElementById('skill-list'),
    skillCountBadge: document.getElementById('skill-count-badge'),
    adminStatus: document.getElementById('admin-status'),
    newSkillButton: document.getElementById('new-skill-button'),
    resetDraftButton: document.getElementById('reset-draft-button'),
    previewButton: document.getElementById('preview-button'),
    saveButton: document.getElementById('save-button'),
    publishButton: document.getElementById('publish-button'),
    unpublishButton: document.getElementById('unpublish-button'),
    archiveButton: document.getElementById('archive-button'),
    editorModeBadge: document.getElementById('editor-mode-badge'),
    skillForm: document.getElementById('skill-form'),
    detailPanel: document.getElementById('detail-panel'),
    previewPanel: document.getElementById('preview-panel'),
    compareSelect: document.getElementById('compare-select'),
    compareButton: document.getElementById('compare-button'),
    comparePanel: document.getElementById('compare-panel')
};

document.addEventListener('DOMContentLoaded', () => {
    wireInteractions();
    loadOverview();
});

function wireInteractions() {
    elements.newSkillButton.addEventListener('click', startNewSkill);
    elements.resetDraftButton.addEventListener('click', resetDraft);
    elements.previewButton.addEventListener('click', () => void previewDraft());
    elements.saveButton.addEventListener('click', () => void saveDraft());
    elements.publishButton.addEventListener('click', () => void publishCurrent());
    elements.unpublishButton.addEventListener('click', () => void unpublishCurrent());
    elements.archiveButton.addEventListener('click', () => void archiveCurrent());
    elements.compareButton.addEventListener('click', () => void compareCurrent());
    elements.compareSelect.addEventListener('change', (event) => {
        state.compareTargetRecordId = event.target.value || null;
    });

    elements.classList.addEventListener('click', (event) => {
        const button = event.target.closest('[data-class-key]');
        if (!button) {
            return;
        }

        state.selectedClassKey = button.dataset.classKey;
        renderAll();
    });

    elements.skillList.addEventListener('click', (event) => {
        const button = event.target.closest('[data-record-id]');
        if (!button) {
            return;
        }

        void loadDetail(button.dataset.recordId);
    });

    elements.skillForm.addEventListener('input', handleFormInput);
    elements.skillForm.addEventListener('change', handleFormInput);
    elements.skillForm.addEventListener('click', handleFormClick);
}

async function loadOverview() {
    setBusy(true);
    setStatus('Loading persisted admin skill catalog...');

    try {
        const overview = await fetchJson('/api/admin/skills/overview');
        state.overview = overview;
        state.options = overview.options;
        state.selectedClassKey = state.selectedClassKey || overview.classes.find((item) => item.skills.length > 0)?.classKey || overview.classes[0]?.classKey || null;
        renderAll();
        setStatus('Administrative catalog loaded. Select a skill or start a new draft.');
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The admin skill catalog could not be loaded.');
    } finally {
        setBusy(false);
    }
}

async function loadDetail(recordId) {
    if (!recordId) {
        return;
    }

    setBusy(true);
    setStatus('Loading persisted admin skill detail...');

    try {
        const detail = await fetchJson(`/api/admin/skills/${recordId}`);
        state.selectedRecordId = detail.recordId;
        state.detail = detail;
        state.isNew = false;
        state.preview = detail.preview;
        state.compare = null;
        hydrateDraft(detail.definition);
        renderAll();
        setStatus(`Loaded ${detail.definition.name} from the persisted admin catalog.`);
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The selected admin skill could not be loaded.');
    } finally {
        setBusy(false);
    }
}

function startNewSkill() {
    state.selectedRecordId = null;
    state.detail = null;
    state.preview = null;
    state.compare = null;
    state.isNew = true;
    hydrateDraft(createBlankSkillDefinition());
    setStatus('Started a new persisted skill draft. Configure the modules and run preview before saving.');
    renderAll();
}

function resetDraft() {
    if (state.isNew || !state.detail) {
        hydrateDraft(createBlankSkillDefinition());
        state.preview = null;
        state.compare = null;
        setStatus('Draft reset to a clean blank skill definition.');
    } else {
        hydrateDraft(state.detail.definition);
        state.preview = state.detail.preview;
        state.compare = null;
        setStatus('Draft reverted to the last persisted version of the selected skill.');
    }

    renderAll();
}

async function previewDraft() {
    if (!state.draft || state.isBusy) {
        return;
    }

    setBusy(true);
    setStatus('Running validation, combat translation preview, and Power Score impact preview...');

    try {
        const preview = await fetchJson(`/api/admin/skills/preview${state.selectedRecordId ? `?currentRecordId=${state.selectedRecordId}` : ''}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Accept: 'application/json'
            },
            body: JSON.stringify({ definition: sanitizeDefinitionForSubmit() })
        });

        state.preview = preview;
        state.compare = null;
        renderAll();
        setStatus('Preview updated from backend validation and referential balance services.');
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The preview could not be generated.');
    } finally {
        setBusy(false);
    }
}

async function saveDraft() {
    if (!state.draft || state.isBusy) {
        return;
    }

    setBusy(true);
    setStatus('Persisting skill draft through the admin backend...');

    const method = state.selectedRecordId ? 'PUT' : 'POST';
    const url = state.selectedRecordId ? `/api/admin/skills/${state.selectedRecordId}` : '/api/admin/skills';

    try {
        const detail = await fetchJson(url, {
            method,
            headers: {
                'Content-Type': 'application/json',
                Accept: 'application/json'
            },
            body: JSON.stringify({ definition: sanitizeDefinitionForSubmit() })
        });

        state.selectedRecordId = detail.recordId;
        state.detail = detail;
        state.preview = detail.preview;
        state.isNew = false;
        hydrateDraft(detail.definition);
        await loadOverview();
        setStatus(`Persisted ${detail.definition.name} successfully.`);
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The skill draft could not be saved.');
    } finally {
        setBusy(false);
    }
}

async function publishCurrent() {
    if (!state.selectedRecordId || state.isBusy) {
        return;
    }

    setBusy(true);
    setStatus('Publishing draft for controlled runtime resolution...');

    try {
        const result = await fetchJson(`/api/admin/skills/${state.selectedRecordId}/publish`, {
            method: 'POST',
            headers: {
                Accept: 'application/json'
            }
        });

        if (state.selectedRecordId) {
            await loadDetail(state.selectedRecordId);
        }
        await loadOverview();
        setStatus(result.message || 'Skill published successfully.');
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The skill could not be published.');
    } finally {
        setBusy(false);
    }
}

async function unpublishCurrent() {
    if (!state.selectedRecordId || state.isBusy) {
        return;
    }

    setBusy(true);
    setStatus('Removing the published runtime snapshot and restoring fallback policy...');

    try {
        const result = await fetchJson(`/api/admin/skills/${state.selectedRecordId}/unpublish`, {
            method: 'POST',
            headers: {
                Accept: 'application/json'
            }
        });

        if (state.selectedRecordId) {
            await loadDetail(state.selectedRecordId);
        }
        await loadOverview();
        setStatus(result.message || 'Published snapshot removed.');
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The skill could not be unpublished.');
    } finally {
        setBusy(false);
    }
}

async function archiveCurrent() {
    if (!state.selectedRecordId || state.isBusy) {
        return;
    }

    const confirmed = window.confirm('Archive this skill? Archived records remain visible in admin but leave runtime resolution.');
    if (!confirmed) {
        return;
    }

    setBusy(true);
    setStatus('Archiving skill from the admin catalog...');

    try {
        const result = await fetchJson(`/api/admin/skills/${state.selectedRecordId}/archive`, {
            method: 'POST',
            headers: {
                Accept: 'application/json'
            }
        });

        startNewSkill();
        await loadOverview();
        setStatus(result.message || 'Skill archived from the admin catalog.');
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The skill could not be archived.');
    } finally {
        setBusy(false);
    }
}

async function compareCurrent() {
    if (!state.selectedRecordId || !state.compareTargetRecordId || state.isBusy) {
        return;
    }

    setBusy(true);
    setStatus('Loading comparison preview between persisted skills...');

    try {
        const compare = await fetchJson(`/api/admin/skills/compare?leftRecordId=${state.selectedRecordId}&rightRecordId=${state.compareTargetRecordId}`);
        state.compare = compare;
        renderComparePanel();
        setStatus('Comparison ready. Review modules, ascensions, and referential Power Score deltas side by side.');
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'The comparison could not be loaded.');
    } finally {
        setBusy(false);
    }
}

function hydrateDraft(definition) {
    state.draft = normalizeDefinition(deepClone(definition));
    state.metadataEntries = metadataToEntries(state.draft.metadata);
    state.ascensionMeta = deriveAscensionMeta(state.draft.ascensionOverrides || {});
}

function renderAll() {
    renderClassList();
    renderSkillList();
    renderEditor();
    renderDetailPanel();
    renderPreviewPanel();
    renderCompareOptions();
    renderComparePanel();
    updateToolbarState();
}

function renderClassList() {
    const classes = state.overview?.classes || [];
    elements.classList.innerHTML = classes.map((item) => `
        <article class="class-card ${item.classKey === state.selectedClassKey ? 'class-card--active' : ''}">
            <button type="button" data-class-key="${escapeHtml(item.classKey)}">
                <div class="panel-card__header">
                    <strong>${escapeHtml(item.classLabel)}</strong>
                    <span class="skill-badge skill-badge--${item.completenessStatus.toLowerCase()}">${escapeHtml(item.completenessStatus)}</span>
                </div>
                <div class="class-card__meta">
                    ${renderMiniMetric('Skills', String(item.activeSkillCount))}
                    ${renderMiniMetric('Ultimates', String(item.ultimateCount))}
                </div>
            </button>
        </article>`).join('');
}

function renderSkillList() {
    const currentClass = getSelectedClass();
    const skills = currentClass?.skills || [];
    elements.skillCountBadge.textContent = String(skills.length);
    elements.skillList.innerHTML = skills.length === 0
        ? '<p class="empty-state">No persisted admin skills exist for this class yet.</p>'
        : skills.map((skill) => {
            const publicationClass = `skill-badge--publication-${skill.publication.state.toLowerCase()}`;
            return `
            <article class="skill-card ${skill.recordId === state.selectedRecordId ? 'skill-card--active' : ''}">
                <button type="button" data-record-id="${escapeHtml(skill.recordId)}">
                    <div class="panel-card__header">
                        <div>
                            <p class="eyebrow">${escapeHtml(skill.slotKey)}</p>
                            <strong>${escapeHtml(skill.name)}</strong>
                        </div>
                        <div class="section-toggle-row">
                            <span class="skill-badge skill-badge--${skill.completenessStatus.toLowerCase()}">${escapeHtml(skill.completenessStatus)}</span>
                            <span class="skill-badge ${publicationClass}">${escapeHtml(skill.publication.state)}</span>
                        </div>
                    </div>
                    <div class="skill-card__meta">
                        ${renderMiniMetric('Id', skill.skillId)}
                        ${renderMiniMetric('Unlock', `Lv ${skill.unlockLevel}`)}
                        ${renderMiniMetric('Kind', skill.isUltimate ? 'Ultimate' : 'Standard')}
                        ${renderMiniMetric('Runtime', skill.publication.isRuntimePublished ? 'Published' : (skill.publication.hasProgrammedFallback ? 'Fallback' : 'Offline'))}
                    </div>
                </button>
            </article>`;
        }).join('');
}

function renderEditor() {
    if (!state.draft) {
        elements.skillForm.innerHTML = '<p class="empty-state">Select a skill or create a new one.</p>';
        return;
    }

    const definition = state.draft;
    const baseAction = definition.baseTuning.action;
    const baseTargeting = definition.baseTuning.targeting;
    const baseCadence = definition.baseTuning.cadence;

    elements.skillForm.innerHTML = `
        ${renderGeneralSection(definition)}
        ${renderActionSection('Combate base', 'baseTuning.action', baseAction, true)}
        ${renderTargetingSection('Targeting y rango', 'baseTuning.targeting', baseTargeting)}
        ${renderCadenceSection('Costos y cooldown', definition, baseCadence)}
        ${renderMultiHitSection('Multi-hit', 'baseTuning.multiHit', definition.baseTuning.multiHit, 'base')}
        ${renderEffectsSection('Efectos y CC', 'baseTuning.effects', definition.baseTuning.effects || [])}
        ${renderProtectionsSection('Protecciones', 'baseTuning.castProtections', definition.baseTuning.castProtections || [])}
        ${renderTriggeredActionsSection('Acciones disparadas', 'baseTuning.triggeredActions', definition.baseTuning.triggeredActions || [])}
        ${renderNotesSection(definition)}
        ${renderAscensionsSection(definition)}
    `;
}

function renderGeneralSection(definition) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Informacion general</h3>
                <div class="section-toggle-row">
                    <span class="skill-badge skill-badge--${resolvePreviewStatus().toLowerCase()}">${escapeHtml(resolvePreviewStatus())}</span>
                </div>
            </div>
            <div class="field-grid">
                ${renderTextField('Skill Id', 'id', definition.id)}
                ${renderTextField('Nombre', 'name', definition.name)}
                ${renderSelectField('Clase', 'classType', definition.classType, state.options.classes)}
                ${renderSelectField('Slot', 'slot', definition.slot, state.options.slots)}
                ${renderNumberField('Unlock Level', 'unlockLevel', definition.unlockLevel, 'number', 1, 80)}
                ${renderCheckboxField('Es ultimate', 'isUltimate', definition.isUltimate)}
            </div>
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label for="field-description">Descripcion</label>
                    <textarea id="field-description" data-path="description">${escapeHtml(definition.description || '')}</textarea>
                </div>
                <div class="field" style="grid-column: 1 / -1;">
                    <label for="field-notes">Notas tecnicas</label>
                    <textarea id="field-notes" data-path="notes">${escapeHtml(definition.notes || '')}</textarea>
                </div>
            </div>
            <div class="inline-grid">
                ${renderCheckboxArray('Elementos', 'elements', definition.elements || [], state.options.elements)}
                ${renderCheckboxArray('Roles de combate', 'roles', definition.roles || [], state.options.combatRoles)}
            </div>
            ${renderMetadataSection()}
        </section>`;
}

function renderActionSection(title, prefix, action, includeDamageType) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>${escapeHtml(title)}</h3>
            </div>
            <div class="field-grid">
                ${renderSelectField('Action Type', `${prefix}.actionType`, action.actionType, state.options.actionTypes)}
                ${includeDamageType ? renderNullableSelectField('Damage Type', `${prefix}.damageType`, action.damageType, state.options.damageTypes) : ''}
                ${renderSelectField('Target Resource', `${prefix}.targetResourceType`, action.targetResourceType, state.options.resourceTypes)}
                ${renderCheckboxField('Hit check', `${prefix}.requiresHitCheck`, action.requiresHitCheck)}
                ${renderCheckboxField('Puede crit', `${prefix}.canCrit`, action.canCrit)}
                ${renderNullableSelectField('Damage Condition', `${prefix}.damageConditionType`, action.damageConditionType, state.options.conditionTypes)}
            </div>
            <div class="field-grid">
                ${renderNumberField('Base Magnitude', `${prefix}.magnitudeProfile.baseMagnitude`, action.magnitudeProfile.baseMagnitude, 'nullable-number')}
                ${renderSelectField('Scaling Type', `${prefix}.magnitudeProfile.scalingType`, action.magnitudeProfile.scalingType, state.options.scalingTypes)}
                ${renderNumberField('Scaling Coefficient', `${prefix}.magnitudeProfile.scalingCoefficient`, action.magnitudeProfile.scalingCoefficient, 'nullable-number')}
                ${renderTextField('Configuration Name', `${prefix}.magnitudeProfile.configurationName`, action.magnitudeProfile.configurationName || '')}
            </div>
            ${renderConditionSynergySection(`${prefix}.conditionSynergies`, action.conditionSynergies || [])}
        </section>`;
}

function renderTargetingSection(title, prefix, targeting) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>${escapeHtml(title)}</h3>
            </div>
            <div class="field-grid">
                ${renderSelectField('Pattern', `${prefix}.pattern`, targeting.pattern, state.options.targetPatterns)}
                ${renderSelectField('Affinity', `${prefix}.affinity`, targeting.affinity, state.options.targetAffinities)}
                ${renderNumberField('Base Range Units', `${prefix}.baseRangeUnits`, targeting.baseRangeUnits, 'nullable-number')}
                ${renderNumberField('Area Radius Units', `${prefix}.areaRadiusUnits`, targeting.areaRadiusUnits, 'nullable-number')}
                ${renderNumberField('Max Targets', `${prefix}.maxTargets`, targeting.maxTargets, 'number', 1, 99)}
                ${renderCheckboxField('Requires selection', `${prefix}.requiresTargetSelection`, targeting.requiresTargetSelection)}
            </div>
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Spatial note</label>
                    <textarea data-path="${prefix}.note">${escapeHtml(targeting.note || '')}</textarea>
                </div>
            </div>
        </section>`;
}

function renderCadenceSection(title, definition, cadence) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>${escapeHtml(title)}</h3>
            </div>
            <div class="field-grid">
                ${renderNumberField('Base Cooldown Seconds', 'baseTuning.cadence.baseCooldownSeconds', cadence.baseCooldownSeconds, 'nullable-number')}
                ${renderCheckboxField('Affected by CDR', 'baseTuning.cadence.affectedByCooldownReduction', cadence.affectedByCooldownReduction)}
                ${renderCheckboxField('Affected by recovery', 'baseTuning.cadence.affectedBySkillRecoveryRate', cadence.affectedBySkillRecoveryRate)}
            </div>
            ${renderResourceCostsSection('baseTuning.resourceCosts', definition.baseTuning.resourceCosts || [])}
        </section>`;
}

function renderNotesSection(definition) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Pending data y seguridad</h3>
            </div>
            ${renderPendingDataList('pendingData', definition.pendingData || [])}
            ${renderStringListEditor('Security Notes', 'securityNotes', definition.securityNotes || [])}
        </section>`;
}

function renderAscensionsSection(definition) {
    const cards = [];

    for (let level = 2; level <= 10; level += 1) {
        const overrideValue = definition.ascensionOverrides?.[level] || createBlankAscensionOverride(level);
        const meta = state.ascensionMeta[level] || createBlankAscensionMeta(false);
        cards.push(renderAscensionCard(level, overrideValue, meta));
    }

    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Ascensiones 2 a 10</h3>
            </div>
            <div class="ascension-list">${cards.join('')}</div>
        </section>`;
}

function renderAscensionCard(level, overrideValue, meta) {
    return `
        <details class="ascension-card" ${meta.enabled ? 'open' : ''}>
            <summary>Ascension ${level}</summary>
            <div class="ascension-card__header">
                <div class="section-toggle-row">
                    <button type="button" class="toggle-button ${meta.enabled ? 'toggle-button--active' : ''}" data-ascension-enable="${level}">${meta.enabled ? 'Override enabled' : 'Enable override'}</button>
                    ${meta.enabled ? renderAscensionToggle(level, 'magnitudeProfile', 'Magnitude') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'action', 'Action') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'targeting', 'Targeting') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'cadence', 'Cadence') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'resourceCosts', 'Costs') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'effects', 'Effects') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'multiHit', 'Multi-hit') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'protections', 'Protections') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'triggeredActions', 'Triggers') : ''}
                    ${meta.enabled ? renderAscensionToggle(level, 'upgradeCost', 'Materials') : ''}
                </div>
            </div>
            ${meta.enabled ? `
                <div class="field-grid">
                    <div class="field" style="grid-column: 1 / -1;">
                        <label>Ascension note</label>
                        <textarea data-path="ascensionOverrides.${level}.note">${escapeHtml(overrideValue.note || '')}</textarea>
                    </div>
                </div>
                ${meta.magnitudeProfile ? renderAscensionMagnitudeSection(level, overrideValue.magnitudeProfile || createBlankMagnitudeProfile()) : ''}
                ${meta.action ? renderActionSection(`Ascension ${level} action override`, `ascensionOverrides.${level}.action`, overrideValue.action || createBlankAction(), true) : ''}
                ${meta.targeting ? renderTargetingSection(`Ascension ${level} targeting override`, `ascensionOverrides.${level}.targeting`, overrideValue.targeting || createBlankTargeting()) : ''}
                ${meta.cadence ? renderAscensionCadenceSection(level, overrideValue.cadence || createBlankCadence()) : ''}
                ${meta.resourceCosts ? renderResourceCostsSection(`ascensionOverrides.${level}.resourceCosts`, overrideValue.resourceCosts || []) : ''}
                ${meta.effects ? renderAscensionEffectsSection(level, overrideValue) : ''}
                ${meta.multiHit ? renderMultiHitSection(`Ascension ${level} multi-hit override`, `ascensionOverrides.${level}.multiHit`, overrideValue.multiHit || createBlankMultiHit(), `ascension.${level}`) : ''}
                ${meta.protections ? renderProtectionsSection(`Ascension ${level} protections`, `ascensionOverrides.${level}.castProtections`, overrideValue.castProtections || []) : ''}
                ${meta.triggeredActions ? renderTriggeredActionsSection(`Ascension ${level} triggered actions`, `ascensionOverrides.${level}.triggeredActions`, overrideValue.triggeredActions || []) : ''}
                ${meta.upgradeCost ? renderUpgradeCostSection(level, overrideValue.upgradeCost || createBlankUpgradeCost()) : ''}` : '<p class="empty-state">This ascension currently inherits all previous state.</p>'}
        </details>`;
}

function renderAscensionToggle(level, sectionKey, label) {
    const meta = state.ascensionMeta[level] || createBlankAscensionMeta(true);
    return `<button type="button" class="toggle-button ${meta[sectionKey] ? 'toggle-button--active' : ''}" data-ascension-section="${level}:${sectionKey}">${escapeHtml(label)}</button>`;
}

function handleFormInput(event) {
    const target = event.target;

    if (target.matches('[data-path]')) {
        updatePathValue(target.dataset.path, readInputValue(target));
        return;
    }

    if (target.matches('[data-array-toggle]')) {
        toggleArrayValue(target.dataset.arrayToggle, target.dataset.optionValue, target.checked);
        return;
    }

    if (target.matches('[data-meta-key]')) {
        const index = Number(target.dataset.metaIndex);
        state.metadataEntries[index][target.dataset.metaKey] = target.value;
        return;
    }
}

function handleFormClick(event) {
    const addButton = event.target.closest('[data-add-collection]');
    if (addButton) {
        addCollectionItem(addButton.dataset.addCollection);
        renderEditor();
        return;
    }

    const removeButton = event.target.closest('[data-remove-collection]');
    if (removeButton) {
        removeCollectionItem(removeButton.dataset.removeCollection, Number(removeButton.dataset.index));
        renderEditor();
        return;
    }

    const addMetaButton = event.target.closest('[data-action="add-metadata"]');
    if (addMetaButton) {
        state.metadataEntries.push({ key: '', value: '' });
        renderEditor();
        return;
    }

    const removeMetaButton = event.target.closest('[data-action="remove-metadata"]');
    if (removeMetaButton) {
        state.metadataEntries.splice(Number(removeMetaButton.dataset.index), 1);
        renderEditor();
        return;
    }

    const ascensionEnable = event.target.closest('[data-ascension-enable]');
    if (ascensionEnable) {
        toggleAscensionEnabled(Number(ascensionEnable.dataset.ascensionEnable));
        renderEditor();
        return;
    }

    const ascensionSection = event.target.closest('[data-ascension-section]');
    if (ascensionSection) {
        const [level, sectionKey] = ascensionSection.dataset.ascensionSection.split(':');
        toggleAscensionSection(Number(level), sectionKey);
        renderEditor();
        return;
    }

    const multiHitToggle = event.target.closest('[data-toggle-multihit]');
    if (multiHitToggle) {
        toggleMultiHit(multiHitToggle.dataset.toggleMultihit);
        renderEditor();
    }
}

function renderMetadataSection() {
    const rows = state.metadataEntries.length === 0
        ? '<p class="empty-state">No metadata tags configured.</p>'
        : state.metadataEntries.map((entry, index) => `
            <article class="repeater-card">
                <div class="repeater-card__header">
                    <h4>Metadata ${index + 1}</h4>
                    <button type="button" class="danger-button" data-action="remove-metadata" data-index="${index}">Remove</button>
                </div>
                <div class="field-grid">
                    <div class="field">
                        <label>Key</label>
                        <input data-meta-index="${index}" data-meta-key="key" value="${escapeHtml(entry.key || '')}" />
                    </div>
                    <div class="field">
                        <label>Value</label>
                        <input data-meta-index="${index}" data-meta-key="value" value="${escapeHtml(entry.value || '')}" />
                    </div>
                </div>
            </article>`).join('');

    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Metadata</h3>
                <button type="button" class="secondary-button" data-action="add-metadata">Agregar metadata</button>
            </div>
            <div class="repeater-list">${rows}</div>
        </section>`;
}

function renderResourceCostsSection(path, costs) {
    return renderRepeaterSection('Costos de recurso', path, costs, (cost, index) => `
        <article class="repeater-card">
            <div class="repeater-card__header">
                <h4>Costo ${index + 1}</h4>
                <button type="button" class="danger-button" data-remove-collection="${path}" data-index="${index}">Remove</button>
            </div>
            <div class="field-grid">
                ${renderSelectField('Resource', `${path}.${index}.resourceType`, cost.resourceType, state.options.resourceTypes)}
                ${renderNumberField('Amount', `${path}.${index}.amount`, cost.amount, 'nullable-number')}
                ${renderCheckboxField('Abort if insufficient', `${path}.${index}.abortIfInsufficient`, cost.abortIfInsufficient)}
            </div>
        </article>`, 'Agregar costo');
}

function renderConditionSynergySection(path, synergies) {
    return renderRepeaterSection('Sinergias de condición', path, synergies, (synergy, index) => `
        <article class="repeater-card">
            <div class="repeater-card__header">
                <h4>Sinergia ${index + 1}</h4>
                <button type="button" class="danger-button" data-remove-collection="${path}" data-index="${index}">Remove</button>
            </div>
            <div class="field-grid">
                ${renderTextField('Synergy Key', `${path}.${index}.synergyKey`, synergy.synergyKey)}
                ${renderSelectField('Required Condition', `${path}.${index}.requiredTargetCondition`, synergy.requiredTargetCondition, state.options.conditionTypes)}
                ${renderNumberField('Magnitude Multiplier', `${path}.${index}.magnitudeMultiplier`, synergy.magnitudeMultiplier, 'nullable-number')}
                ${renderNumberField('Flat Base Bonus', `${path}.${index}.flatBaseMagnitudeBonus`, synergy.flatBaseMagnitudeBonus, 'nullable-number')}
            </div>
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Note</label>
                    <textarea data-path="${path}.${index}.note">${escapeHtml(synergy.note || '')}</textarea>
                </div>
            </div>
        </article>`, 'Agregar sinergia');
}

function renderEffectsSection(title, path, effects) {
    return renderRepeaterSection(title, path, effects, (effect, index) => renderEffectCard(path, effect, index), 'Agregar efecto');
}

function renderEffectCard(path, effect, index, overrideMode = false) {
    return `
        <article class="repeater-card">
            <div class="repeater-card__header">
                <h4>${overrideMode ? 'Effect Override' : `Effect ${index + 1}`}</h4>
                <button type="button" class="danger-button" data-remove-collection="${path}" data-index="${index}">Remove</button>
            </div>
            <div class="field-grid">
                ${renderTextField('Effect Key', `${path}.${index}.effectKey`, effect.effectKey || '')}
                ${overrideMode ? '' : renderSelectField('Condition', `${path}.${index}.condition`, effect.condition, state.options.conditionTypes)}
                ${renderNumberField('Base Duration', `${path}.${index}.baseDurationSeconds`, effect.baseDurationSeconds, 'nullable-number')}
                ${renderNumberField('Base Apply Chance', `${path}.${index}.baseApplyChance`, effect.baseApplyChance, 'nullable-number')}
                ${renderNumberField('Flat Bonus', `${path}.${index}.applyChanceFlatBonus`, effect.applyChanceFlatBonus, 'nullable-number')}
                ${renderNumberField('Multiplier', `${path}.${index}.applyChanceMultiplier`, effect.applyChanceMultiplier, 'nullable-number')}
            </div>
            ${overrideMode ? '' : renderCheckboxArray('Required target conditions', `${path}.${index}.requiredTargetConditions`, effect.requiredTargetConditions || [], state.options.conditionTypes)}
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Note</label>
                    <textarea data-path="${path}.${index}.note">${escapeHtml(effect.note || '')}</textarea>
                </div>
            </div>
        </article>`;
}

function renderProtectionsSection(title, path, protections) {
    return renderRepeaterSection(title, path, protections, (protection, index) => `
        <article class="repeater-card">
            <div class="repeater-card__header">
                <h4>Protection ${index + 1}</h4>
                <button type="button" class="danger-button" data-remove-collection="${path}" data-index="${index}">Remove</button>
            </div>
            <div class="field-grid">
                ${renderTextField('Grant Key', `${path}.${index}.grantKey`, protection.grantKey || '')}
                ${renderSelectField('Protection Type', `${path}.${index}.protectionType`, protection.protectionType, state.options.protectionTypes)}
                ${renderNumberField('Duration Seconds', `${path}.${index}.baseDurationSeconds`, protection.baseDurationSeconds, 'nullable-number')}
                ${renderSelectField('Refresh Policy', `${path}.${index}.refreshPolicy`, protection.refreshPolicy, state.options.protectionRefreshPolicies)}
                ${renderCheckboxField('Removes negative effects', `${path}.${index}.removesExistingNegativeEffects`, protection.removesExistingNegativeEffects)}
            </div>
            ${renderCheckboxArray('Blocks', `${path}.${index}.blocks`, splitProtectionBlocks(protection.blocks), state.options.protectionBlockTypes, true)}
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Note</label>
                    <textarea data-path="${path}.${index}.note">${escapeHtml(protection.note || '')}</textarea>
                </div>
            </div>
        </article>`, 'Agregar proteccion');
}

function renderTriggeredActionsSection(title, path, actions) {
    return renderRepeaterSection(title, path, actions, (action, index) => `
        <article class="repeater-card">
            <div class="repeater-card__header">
                <h4>Triggered Action ${index + 1}</h4>
                <button type="button" class="danger-button" data-remove-collection="${path}" data-index="${index}">Remove</button>
            </div>
            <div class="field-grid">
                ${renderTextField('Action Key', `${path}.${index}.actionKey`, action.actionKey || '')}
                ${renderSelectField('Trigger Phase', `${path}.${index}.triggerPhase`, action.triggerPhase, state.options.triggerPhases)}
                ${renderSelectField('Target Selector', `${path}.${index}.targetSelector`, action.targetSelector, state.options.triggerTargets)}
            </div>
            ${renderActionSection(`Triggered action payload`, `${path}.${index}.action`, action.action || createBlankAction(), true)}
            ${renderEffectsSection('Triggered effects', `${path}.${index}.effects`, action.effects || [])}
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Note</label>
                    <textarea data-path="${path}.${index}.note">${escapeHtml(action.note || '')}</textarea>
                </div>
            </div>
        </article>`, 'Agregar accion disparada');
}

function renderMultiHitSection(title, path, multiHit, contextKey) {
    const enabled = Boolean(multiHit);
    const profile = multiHit || createBlankMultiHit();

    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>${escapeHtml(title)}</h3>
                <button type="button" class="toggle-button ${enabled ? 'toggle-button--active' : ''}" data-toggle-multihit="${contextKey}">${enabled ? 'Enabled' : 'Enable'}</button>
            </div>
            ${enabled ? `
                <div class="field-grid">
                    ${renderNumberField('Hit Count', `${path}.hitCount`, profile.hitCount, 'number', 1, 99)}
                    ${renderNumberField('Active Duration', `${path}.activeDurationSeconds`, profile.activeDurationSeconds, 'nullable-number')}
                    ${renderSelectField('Distribution', `${path}.distribution`, profile.distribution, state.options.hitDistributionModes)}
                    ${renderCheckboxField('Effects per hit', `${path}.effectsResolvePerHit`, profile.effectsResolvePerHit)}
                </div>
                <div class="field-grid">
                    <div class="field" style="grid-column: 1 / -1;">
                        <label>Note</label>
                        <textarea data-path="${path}.note">${escapeHtml(profile.note || '')}</textarea>
                    </div>
                </div>` : '<p class="empty-state">This skill does not currently declare multi-hit behavior.</p>'}
        </section>`;
}

function renderPendingDataList(path, items) {
    return renderRepeaterSection('Pending data', path, items, (item, index) => `
        <article class="repeater-card">
            <div class="repeater-card__header">
                <h4>Pending datum ${index + 1}</h4>
                <button type="button" class="danger-button" data-remove-collection="${path}" data-index="${index}">Remove</button>
            </div>
            <div class="field-grid">
                ${renderTextField('Key', `${path}.${index}.key`, item.key || '')}
                ${renderCheckboxField('Blocks exact simulation', `${path}.${index}.blocksExactCombatSimulation`, item.blocksExactCombatSimulation)}
            </div>
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Description</label>
                    <textarea data-path="${path}.${index}.description">${escapeHtml(item.description || '')}</textarea>
                </div>
            </div>
        </article>`, 'Agregar pending datum');
}

function renderStringListEditor(title, path, values) {
    return renderRepeaterSection(title, path, values, (value, index) => `
        <article class="repeater-card">
            <div class="repeater-card__header">
                <h4>${escapeHtml(title)} ${index + 1}</h4>
                <button type="button" class="danger-button" data-remove-collection="${path}" data-index="${index}">Remove</button>
            </div>
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Text</label>
                    <textarea data-path="${path}.${index}">${escapeHtml(value || '')}</textarea>
                </div>
            </div>
        </article>`, `Agregar ${title}`);
}

function renderAscensionMagnitudeSection(level, profile) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Ascension ${level} magnitude override</h3>
            </div>
            <div class="field-grid">
                ${renderNumberField('Base Magnitude', `ascensionOverrides.${level}.magnitudeProfile.baseMagnitude`, profile.baseMagnitude, 'nullable-number')}
                ${renderSelectField('Scaling Type', `ascensionOverrides.${level}.magnitudeProfile.scalingType`, profile.scalingType, state.options.scalingTypes)}
                ${renderNumberField('Scaling Coefficient', `ascensionOverrides.${level}.magnitudeProfile.scalingCoefficient`, profile.scalingCoefficient, 'nullable-number')}
                ${renderTextField('Configuration', `ascensionOverrides.${level}.magnitudeProfile.configurationName`, profile.configurationName || '')}
            </div>
        </section>`;
}

function renderAscensionCadenceSection(level, cadence) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Ascension ${level} cadence override</h3>
            </div>
            <div class="field-grid">
                ${renderNumberField('Base Cooldown Seconds', `ascensionOverrides.${level}.cadence.baseCooldownSeconds`, cadence.baseCooldownSeconds, 'nullable-number')}
                ${renderCheckboxField('Affected by CDR', `ascensionOverrides.${level}.cadence.affectedByCooldownReduction`, cadence.affectedByCooldownReduction)}
                ${renderCheckboxField('Affected by recovery', `ascensionOverrides.${level}.cadence.affectedBySkillRecoveryRate`, cadence.affectedBySkillRecoveryRate)}
            </div>
        </section>`;
}

function renderAscensionEffectsSection(level, overrideValue) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Ascension ${level} effect changes</h3>
            </div>
            ${renderEffectsSection('Added effects', `ascensionOverrides.${level}.addedEffects`, overrideValue.addedEffects || [])}
            ${renderRepeaterSection('Effect overrides', `ascensionOverrides.${level}.effectOverrides`, overrideValue.effectOverrides || [], (item, index) => renderEffectCard(`ascensionOverrides.${level}.effectOverrides`, item, index, true), 'Agregar effect override')}
            ${renderRepeaterSection('Removed effect keys', `ascensionOverrides.${level}.removedEffectKeys`, overrideValue.removedEffectKeys || [], (item, index) => `
                <article class="repeater-card">
                    <div class="repeater-card__header">
                        <h4>Removed key ${index + 1}</h4>
                        <button type="button" class="danger-button" data-remove-collection="ascensionOverrides.${level}.removedEffectKeys" data-index="${index}">Remove</button>
                    </div>
                    <div class="field-grid">
                        ${renderTextField('Effect Key', `ascensionOverrides.${level}.removedEffectKeys.${index}`, item || '')}
                    </div>
                </article>`, 'Agregar key removida')}
        </section>`;
}

function renderUpgradeCostSection(level, upgradeCost) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>Ascension ${level} materials</h3>
            </div>
            ${renderRepeaterSection('Materials', `ascensionOverrides.${level}.upgradeCost.materials`, upgradeCost.materials || [], (material, index) => `
                <article class="repeater-card">
                    <div class="repeater-card__header">
                        <h4>Material ${index + 1}</h4>
                        <button type="button" class="danger-button" data-remove-collection="ascensionOverrides.${level}.upgradeCost.materials" data-index="${index}">Remove</button>
                    </div>
                    <div class="field-grid">
                        ${renderSelectField('Material Type', `ascensionOverrides.${level}.upgradeCost.materials.${index}.materialType`, material.materialType, state.options.ascensionMaterialTypes)}
                        ${renderNumberField('Quantity', `ascensionOverrides.${level}.upgradeCost.materials.${index}.quantity`, material.quantity, 'number', 0, 999)}
                        ${renderTextField('Item Key', `ascensionOverrides.${level}.upgradeCost.materials.${index}.itemKey`, material.itemKey || '')}
                    </div>
                    <div class="field-grid">
                        <div class="field" style="grid-column: 1 / -1;">
                            <label>Note</label>
                            <textarea data-path="ascensionOverrides.${level}.upgradeCost.materials.${index}.note">${escapeHtml(material.note || '')}</textarea>
                        </div>
                    </div>
                </article>`, 'Agregar material')}
            <div class="field-grid">
                <div class="field" style="grid-column: 1 / -1;">
                    <label>Pending reason</label>
                    <textarea data-path="ascensionOverrides.${level}.upgradeCost.pendingReason">${escapeHtml(upgradeCost.pendingReason || '')}</textarea>
                </div>
            </div>
        </section>`;
}

function renderRepeaterSection(title, path, items, renderItem, addLabel) {
    return `
        <section class="form-section">
            <div class="form-section__header">
                <h3>${escapeHtml(title)}</h3>
                <button type="button" class="secondary-button" data-add-collection="${path}">${escapeHtml(addLabel || 'Agregar')}</button>
            </div>
            <div class="repeater-list">
                ${items.length === 0 ? '<p class="empty-state">No entries configured.</p>' : items.map((item, index) => renderItem(item, index)).join('')}
            </div>
        </section>`;
}

function renderTextField(label, path, value) {
    return `<div class="field"><label>${escapeHtml(label)}</label><input data-path="${path}" value="${escapeHtml(value ?? '')}" /></div>`;
}

function renderNumberField(label, path, value, valueType = 'nullable-number', min = '', max = '') {
    return `<div class="field"><label>${escapeHtml(label)}</label><input type="number" data-path="${path}" data-value-type="${valueType}" min="${min}" max="${max}" step="0.01" value="${value ?? ''}" /></div>`;
}

function renderSelectField(label, path, currentValue, options) {
    return `<div class="field"><label>${escapeHtml(label)}</label><select data-path="${path}">${options.map((option) => `<option value="${escapeHtml(option.key)}" ${option.key === currentValue ? 'selected' : ''}>${escapeHtml(option.label)}</option>`).join('')}</select></div>`;
}

function renderNullableSelectField(label, path, currentValue, options) {
    return `<div class="field"><label>${escapeHtml(label)}</label><select data-path="${path}" data-nullable="true"><option value="">None</option>${options.map((option) => `<option value="${escapeHtml(option.key)}" ${option.key === currentValue ? 'selected' : ''}>${escapeHtml(option.label)}</option>`).join('')}</select></div>`;
}

function renderCheckboxField(label, path, checked) {
    return `<div class="field"><label><input type="checkbox" data-path="${path}" data-value-type="boolean" ${checked ? 'checked' : ''} /> ${escapeHtml(label)}</label></div>`;
}

function renderCheckboxArray(label, path, values, options, flagMode = false) {
    return `
        <div class="checkbox-group">
            <span>${escapeHtml(label)}</span>
            <div class="checkbox-pill-row">
                ${options.map((option) => {
                    const isChecked = Array.isArray(values) ? values.includes(option.key) : false;
                    return `<label class="checkbox-pill"><input type="checkbox" data-array-toggle="${path}" data-option-value="${escapeHtml(option.key)}" ${flagMode ? 'data-flag-mode="true"' : ''} ${isChecked ? 'checked' : ''} /> ${escapeHtml(option.label)}</label>`;
                }).join('')}
            </div>
        </div>`;
}

function renderDetailPanel() {
    const detail = state.detail;
    const preview = state.preview || detail?.preview;

    if (!state.draft) {
        elements.detailPanel.innerHTML = '<p class="empty-state">Select a skill to inspect its persisted detail.</p>';
        return;
    }

    const definition = state.draft;
    const ascensions = detail?.ascensions || [];
    const publication = detail?.publication;
    elements.detailPanel.innerHTML = `
        <div class="details-list">
            <article><span>Id</span><strong>${escapeHtml(definition.id)}</strong></article>
            <article><span>Class</span><strong>${escapeHtml(definition.classType)}</strong></article>
            <article><span>Slot</span><strong>${escapeHtml(definition.slot)}</strong></article>
            <article><span>Unlock</span><strong>Lv ${escapeHtml(String(definition.unlockLevel))}</strong></article>
            <article><span>Ultimate</span><strong>${definition.isUltimate ? 'Yes' : 'No'}</strong></article>
            <article><span>Publication</span><strong>${escapeHtml(publication?.state || 'Draft')}</strong></article>
            <article><span>Draft Version</span><strong>${escapeHtml(String(publication?.draftVersion || 1))}</strong></article>
            <article><span>Published Version</span><strong>${escapeHtml(publication?.publishedVersion ? String(publication.publishedVersion) : 'None')}</strong></article>
        </div>
        <div class="note-card">
            <h4>Runtime resolution</h4>
            <div class="note-list">
                <p>${escapeHtml(publication?.runtimeResolution || 'No runtime resolution metadata yet.')}</p>
                ${publication?.notes?.map((note) => `<p>${escapeHtml(note)}</p>`).join('') || '<p class="empty-state">No publication notes.</p>'}
            </div>
        </div>
        <div class="note-card">
            <h4>Description</h4>
            <p>${escapeHtml(definition.description || 'No description yet.')}</p>
        </div>
        <div class="note-card">
            <h4>Ascension summary</h4>
            <div class="note-list">
                ${ascensions.length === 0
                    ? '<p class="empty-state">Ascension detail will appear after the skill is persisted.</p>'
                    : ascensions.map((entry) => `<p><strong>A${entry.level}</strong> · ${escapeHtml(entry.highlights.join(' ') || 'No override')}</p>`).join('')}
            </div>
        </div>
        <div class="note-card">
            <h4>Validation issues</h4>
            <div class="note-list">
                ${!preview || preview.validationIssues.length === 0
                    ? '<p class="empty-state">No validation issues currently reported.</p>'
                    : preview.validationIssues.map((issue) => `<p><strong>${escapeHtml(issue.code)}</strong> · ${escapeHtml(issue.message)}</p>`).join('')}
            </div>
        </div>`;
}

function renderPreviewPanel() {
    const preview = state.preview || state.detail?.preview;
    if (!preview) {
        elements.previewPanel.innerHTML = '<p class="empty-state">Run preview to inspect validation, combat translation, runtime publication readiness, and Power Score deltas.</p>';
        return;
    }

    elements.previewPanel.innerHTML = `
        <div class="preview-metrics">
            ${renderMetricCard('Completeness', preview.completenessStatus, `${preview.validationIssueCount} validation issues`)}
            ${renderMetricCard('Save Draft', preview.canSaveDraft ? 'Ready' : 'Blocked', `${preview.runtimeCatalogIssueCount} runtime catalog issues`) }
            ${renderMetricCard('Publish', preview.canPublish ? 'Ready' : 'Blocked', preview.canTranslateToCombat ? 'Combat translation available' : 'Combat translation blocked')}
        </div>
        <div class="preview-card">
            <h4>Runtime publication notes</h4>
            <div class="note-list">
                ${preview.runtimeCatalogNotes.length === 0
                    ? '<p class="empty-state">No runtime catalog notes.</p>'
                    : preview.runtimeCatalogNotes.map((note) => `<p>${escapeHtml(note)}</p>`).join('')}
            </div>
            <div class="note-list">
                ${preview.runtimeCatalogIssues.length === 0
                    ? '<p class="empty-state">No projected runtime catalog issues.</p>'
                    : preview.runtimeCatalogIssues.map((issue) => `<p><strong>${escapeHtml(issue.code)}</strong> · ${escapeHtml(issue.message)}</p>`).join('')}
            </div>
        </div>
        <div class="preview-card">
            <h4>Combat translation</h4>
            ${preview.combatPreviews.map((item) => `
                <div class="metric-grid">
                    ${renderMetricCard(item.label, item.canTranslate ? 'OK' : 'Blocked', `Asc ${item.ascensionLevel}`)}
                    ${renderMetricCard('Scheduled hits', String(item.scheduledEventCount), item.scalingType || 'No scaling')}
                    ${renderMetricCard('Effects', String(item.effectCount), `${item.castProtectionCount} protections`) }
                </div>
                <div class="note-list">${item.notes.length === 0 ? '<p class="empty-state">No combat notes.</p>' : item.notes.map((note) => `<p>${escapeHtml(note)}</p>`).join('')}</div>`).join('')}
        </div>
        <div class="preview-card">
            <h4>Power Score support</h4>
            ${preview.powerScoreImpacts.length === 0
                ? '<p class="empty-state">Power Score preview is not available until the skill validates cleanly.</p>'
                : preview.powerScoreImpacts.map((impact) => `
                    <div class="metric-grid">
                        ${renderMetricCard(impact.label, impact.deltaDisplay, `Level ${impact.referenceLevel}`)}
                        ${renderMetricCard('Baseline', impact.baselineDisplay, `Asc ${impact.ascensionLevel}`)}
                        ${renderMetricCard('Projected', impact.projectedDisplay, `${impact.categories.length} category deltas`) }
                    </div>
                    <div class="note-list">${impact.notes.map((note) => `<p>${escapeHtml(note)}</p>`).join('')}</div>`).join('')}
        </div>
        <div class="note-card">
            <h4>Pending data</h4>
            <div class="note-list">${preview.pendingData.length === 0 ? '<p class="empty-state">No pending data declared.</p>' : preview.pendingData.map((note) => `<p>${escapeHtml(note)}</p>`).join('')}</div>
        </div>`;
}

function renderCompareOptions() {
    const skills = (state.overview?.classes || [])
        .flatMap((item) => item.skills)
        .filter((skill) => skill.recordId !== state.selectedRecordId);
    const options = skills
        .map((skill) => `<option value="${escapeHtml(skill.recordId)}" ${skill.recordId === state.compareTargetRecordId ? 'selected' : ''}>${escapeHtml(skill.classLabel || skill.classKey)} · ${escapeHtml(skill.name)} · ${escapeHtml(skill.slotKey)}</option>`)
        .join('');

    elements.compareSelect.innerHTML = `<option value="">Select a skill</option>${options}`;
}

function renderComparePanel() {
    if (!state.compare) {
        elements.comparePanel.innerHTML = '<p class="empty-state">Pick another skill from the same class view to compare or load an existing persisted record first.</p>';
        return;
    }

    elements.comparePanel.innerHTML = `
        <div class="compare-card">
            <h4>${escapeHtml(state.compare.left.definition.name)} vs ${escapeHtml(state.compare.right.definition.name)}</h4>
            <div class="compare-metrics">
                ${state.compare.metrics.map((metric) => `
                    <article class="metric">
                        <span>${escapeHtml(metric.label)}</span>
                        <strong>${escapeHtml(metric.leftValue)} / ${escapeHtml(metric.rightValue)}</strong>
                        <p>${escapeHtml(metric.note || '')}</p>
                    </article>`).join('')}
            </div>
            <div class="note-list">${state.compare.notes.map((note) => `<p>${escapeHtml(note)}</p>`).join('')}</div>
        </div>`;
}

function updateToolbarState() {
    const publicationState = resolvePublicationState();
    const hasDetail = Boolean(state.detail && state.selectedRecordId);
    const isArchived = publicationState === 'Archived';
    const isPublished = publicationState === 'Published' || publicationState === 'PublishedWithDraft';
    elements.editorModeBadge.textContent = state.isNew ? 'New Draft' : `${publicationState} / ${resolvePreviewStatus()}`;
    elements.archiveButton.disabled = !state.selectedRecordId || state.isBusy;
    elements.publishButton.disabled = !hasDetail || isArchived || state.isBusy;
    elements.unpublishButton.disabled = !hasDetail || !isPublished || isArchived || state.isBusy;
    elements.saveButton.disabled = !state.draft || isArchived || state.isBusy;
    elements.previewButton.disabled = !state.draft || isArchived || state.isBusy;
    elements.resetDraftButton.disabled = !state.draft || state.isBusy;
    elements.compareButton.disabled = !state.selectedRecordId || !state.compareTargetRecordId || state.isBusy;
}

function updatePathValue(path, value) {
    setAtPath(state.draft, path.split('.'), value);
}

function toggleArrayValue(path, optionValue, enabled) {
    const current = getAtPath(state.draft, path.split('.')) || [];
    const next = Array.isArray(current) ? [...current] : [];
    const existingIndex = next.indexOf(optionValue);

    if (enabled && existingIndex === -1) {
        next.push(optionValue);
    }

    if (!enabled && existingIndex >= 0) {
        next.splice(existingIndex, 1);
    }

    setAtPath(state.draft, path.split('.'), next);
}

function addCollectionItem(path) {
    const current = getAtPath(state.draft, path.split('.')) || [];
    current.push(createCollectionItem(path));
    setAtPath(state.draft, path.split('.'), current);
}

function removeCollectionItem(path, index) {
    const current = [...(getAtPath(state.draft, path.split('.')) || [])];
    current.splice(index, 1);
    setAtPath(state.draft, path.split('.'), current);
}

function toggleAscensionEnabled(level) {
    const meta = state.ascensionMeta[level] || createBlankAscensionMeta(false);
    meta.enabled = !meta.enabled;
    state.ascensionMeta[level] = meta;

    if (!meta.enabled) {
        delete state.draft.ascensionOverrides[level];
        state.ascensionMeta[level] = createBlankAscensionMeta(false);
    } else {
        state.draft.ascensionOverrides[level] = state.draft.ascensionOverrides[level] || createBlankAscensionOverride(level);
    }
}

function toggleAscensionSection(level, sectionKey) {
    const meta = state.ascensionMeta[level] || createBlankAscensionMeta(true);
    meta.enabled = true;
    meta[sectionKey] = !meta[sectionKey];
    state.ascensionMeta[level] = meta;
    state.draft.ascensionOverrides[level] = state.draft.ascensionOverrides[level] || createBlankAscensionOverride(level);

    if (meta[sectionKey]) {
        initializeAscensionSection(level, sectionKey);
    }
}

function initializeAscensionSection(level, sectionKey) {
    const overrideValue = state.draft.ascensionOverrides[level] || createBlankAscensionOverride(level);
    switch (sectionKey) {
        case 'magnitudeProfile':
            overrideValue.magnitudeProfile = overrideValue.magnitudeProfile || createBlankMagnitudeProfile();
            break;
        case 'action':
            overrideValue.action = overrideValue.action || createBlankAction();
            break;
        case 'targeting':
            overrideValue.targeting = overrideValue.targeting || createBlankTargeting();
            break;
        case 'cadence':
            overrideValue.cadence = overrideValue.cadence || createBlankCadence();
            break;
        case 'resourceCosts':
            overrideValue.resourceCosts = overrideValue.resourceCosts || [];
            break;
        case 'effects':
            overrideValue.addedEffects = overrideValue.addedEffects || [];
            overrideValue.effectOverrides = overrideValue.effectOverrides || [];
            overrideValue.removedEffectKeys = overrideValue.removedEffectKeys || [];
            break;
        case 'multiHit':
            overrideValue.multiHit = overrideValue.multiHit || createBlankMultiHit();
            break;
        case 'protections':
            overrideValue.castProtections = overrideValue.castProtections || [];
            break;
        case 'triggeredActions':
            overrideValue.triggeredActions = overrideValue.triggeredActions || [];
            break;
        case 'upgradeCost':
            overrideValue.upgradeCost = overrideValue.upgradeCost || createBlankUpgradeCost();
            break;
        default:
            break;
    }

    state.draft.ascensionOverrides[level] = overrideValue;
}

function toggleMultiHit(contextKey) {
    if (contextKey === 'base') {
        state.draft.baseTuning.multiHit = state.draft.baseTuning.multiHit ? null : createBlankMultiHit();
        return;
    }

    const level = Number(contextKey.split('.')[1]);
    const meta = state.ascensionMeta[level] || createBlankAscensionMeta(true);
    meta.enabled = true;
    meta.multiHit = !meta.multiHit;
    state.ascensionMeta[level] = meta;
    state.draft.ascensionOverrides[level] = state.draft.ascensionOverrides[level] || createBlankAscensionOverride(level);

    if (meta.multiHit) {
        state.draft.ascensionOverrides[level].multiHit = state.draft.ascensionOverrides[level].multiHit || createBlankMultiHit();
    }
}

function sanitizeProtectionBlocks(protections) {
    return (protections || []).map((protection) => ({
        ...protection,
        blocks: Array.isArray(protection.blocks) ? protection.blocks.join(', ') : protection.blocks
    }));
}

function sanitizeTriggeredAction(action) {
    return {
        ...action,
        action: {
            ...(action.action || createBlankAction()),
            damageType: normalizeNullableEnum((action.action || {}).damageType),
            damageConditionType: normalizeNullableEnum((action.action || {}).damageConditionType)
        },
        effects: action.effects || []
    };
}

function normalizeNullableEnum(value) {
    return value === '' ? null : value;
}

function sanitizeDefinitionForSubmit() {
    const definition = normalizeDefinition(deepClone(state.draft));
    definition.metadata = entriesToMetadata(state.metadataEntries);
    definition.baseTuning.multiHit = getBaseMultiHitEnabled() ? definition.baseTuning.multiHit : null;
    definition.baseTuning.castProtections = sanitizeProtectionBlocks(definition.baseTuning.castProtections || []);
    definition.baseTuning.triggeredActions = (definition.baseTuning.triggeredActions || []).map(sanitizeTriggeredAction);
    definition.baseTuning.action.damageType = normalizeNullableEnum(definition.baseTuning.action.damageType);
    definition.baseTuning.action.damageConditionType = normalizeNullableEnum(definition.baseTuning.action.damageConditionType);
    definition.ascensionOverrides = buildSanitizedAscensionOverrides(definition.ascensionOverrides || {});
    return definition;
}

function buildSanitizedAscensionOverrides(overrides) {
    const result = {};

    Object.keys(state.ascensionMeta).forEach((levelKey) => {
        const level = Number(levelKey);
        const meta = state.ascensionMeta[level];
        if (!meta?.enabled) {
            return;
        }

        const source = overrides[level] || createBlankAscensionOverride(level);
        const sanitized = { ascensionLevel: level };

        if (source.note) sanitized.note = source.note;
        if (meta.magnitudeProfile) sanitized.magnitudeProfile = source.magnitudeProfile || createBlankMagnitudeProfile();
        if (meta.action) {
            sanitized.action = source.action || createBlankAction();
            sanitized.action.damageType = normalizeNullableEnum(sanitized.action.damageType);
            sanitized.action.damageConditionType = normalizeNullableEnum(sanitized.action.damageConditionType);
        }
        if (meta.targeting) sanitized.targeting = source.targeting || createBlankTargeting();
        if (meta.cadence) sanitized.cadence = source.cadence || createBlankCadence();
        if (meta.resourceCosts) sanitized.resourceCosts = source.resourceCosts || [];
        if (meta.effects) {
            sanitized.addedEffects = source.addedEffects || [];
            sanitized.effectOverrides = source.effectOverrides || [];
            sanitized.removedEffectKeys = source.removedEffectKeys || [];
        }
        if (meta.multiHit) sanitized.multiHit = source.multiHit || createBlankMultiHit();
        if (meta.protections) sanitized.castProtections = sanitizeProtectionBlocks(source.castProtections || []);
        if (meta.triggeredActions) sanitized.triggeredActions = (source.triggeredActions || []).map(sanitizeTriggeredAction);
        if (meta.upgradeCost) sanitized.upgradeCost = source.upgradeCost || createBlankUpgradeCost();

        if (Object.keys(sanitized).length > 1 || sanitized.note) {
            result[level] = sanitized;
        }
    });

    return result;
}

function getBaseMultiHitEnabled() {
    return Boolean(state.draft?.baseTuning?.multiHit);
}

function deriveAscensionMeta(overrides) {
    const meta = {};
    for (let level = 2; level <= 10; level += 1) {
        const overrideValue = overrides?.[level];
        meta[level] = createBlankAscensionMeta(Boolean(overrideValue));
        if (!overrideValue) {
            continue;
        }

        meta[level].magnitudeProfile = Boolean(overrideValue.magnitudeProfile);
        meta[level].action = Boolean(overrideValue.action);
        meta[level].targeting = Boolean(overrideValue.targeting);
        meta[level].cadence = Boolean(overrideValue.cadence);
        meta[level].resourceCosts = Array.isArray(overrideValue.resourceCosts);
        meta[level].effects = Array.isArray(overrideValue.addedEffects) || Array.isArray(overrideValue.effectOverrides) || Array.isArray(overrideValue.removedEffectKeys);
        meta[level].multiHit = Boolean(overrideValue.multiHit);
        meta[level].protections = Array.isArray(overrideValue.castProtections);
        meta[level].triggeredActions = Array.isArray(overrideValue.triggeredActions);
        meta[level].upgradeCost = Boolean(overrideValue.upgradeCost);
    }

    return meta;
}

function createBlankAscensionMeta(enabled) {
    return {
        enabled,
        magnitudeProfile: false,
        action: false,
        targeting: false,
        cadence: false,
        resourceCosts: false,
        effects: false,
        multiHit: false,
        protections: false,
        triggeredActions: false,
        upgradeCost: false
    };
}

function createBlankSkillDefinition() {
    return normalizeDefinition({
        id: '',
        name: '',
        description: '',
        classType: 'Sorcerer',
        slot: 'Slot01',
        isUltimate: false,
        unlockLevel: 1,
        baseTuning: {
            action: createBlankAction(),
            targeting: createBlankTargeting(),
            cadence: createBlankCadence(),
            resourceCosts: [],
            effects: [],
            multiHit: null,
            castProtections: [],
            triggeredActions: []
        },
        ascensionOverrides: {},
        elements: [],
        roles: [],
        notes: '',
        metadata: {},
        pendingData: [],
        securityNotes: []
    });
}

function createBlankAction() {
    return {
        actionType: 'Damage',
        magnitudeProfile: createBlankMagnitudeProfile(),
        damageType: 'Magical',
        targetResourceType: 'Hp',
        requiresHitCheck: true,
        canCrit: true,
        damageConditionType: null,
        conditionSynergies: []
    };
}

function createBlankMagnitudeProfile() {
    return {
        baseMagnitude: 0,
        scalingType: 'MagicAttack',
        scalingCoefficient: 1,
        configurationName: ''
    };
}

function createBlankTargeting() {
    return {
        pattern: 'SingleTarget',
        affinity: 'Enemy',
        baseRangeUnits: 0,
        areaRadiusUnits: null,
        maxTargets: 1,
        requiresTargetSelection: true,
        note: ''
    };
}

function createBlankCadence() {
    return {
        baseCooldownSeconds: 0,
        affectedByCooldownReduction: true,
        affectedBySkillRecoveryRate: true
    };
}

function createBlankMultiHit() {
    return {
        hitCount: 2,
        activeDurationSeconds: 1,
        distribution: 'EvenlyDistributed',
        effectsResolvePerHit: true,
        note: ''
    };
}

function createBlankConditionEffect() {
    return {
        effectKey: '',
        condition: 'Heat',
        baseDurationSeconds: null,
        baseApplyChance: null,
        applyChanceFlatBonus: 0,
        applyChanceMultiplier: 1,
        requiredTargetConditions: [],
        note: ''
    };
}

function createBlankConditionEffectOverride() {
    return {
        effectKey: '',
        baseDurationSeconds: null,
        baseApplyChance: null,
        applyChanceFlatBonus: null,
        applyChanceMultiplier: null,
        note: ''
    };
}

function createBlankProtection() {
    return {
        grantKey: '',
        protectionType: 'Invulnerability',
        blocks: ['Damage'],
        baseDurationSeconds: 1,
        refreshPolicy: 'IgnoreIfAlreadyActive',
        removesExistingNegativeEffects: false,
        note: ''
    };
}

function createBlankTriggeredAction() {
    return {
        actionKey: '',
        triggerPhase: 'OnCast',
        action: createBlankAction(),
        targetSelector: 'SelectedTarget',
        effects: [],
        note: ''
    };
}

function createBlankSynergy() {
    return {
        synergyKey: '',
        requiredTargetCondition: 'Heat',
        magnitudeMultiplier: 1,
        flatBaseMagnitudeBonus: 0,
        note: ''
    };
}

function createBlankPendingDatum() {
    return {
        key: '',
        description: '',
        blocksExactCombatSimulation: false
    };
}

function createBlankAscensionOverride(level) {
    return {
        ascensionLevel: level,
        note: '',
        magnitudeProfile: null,
        action: null,
        targeting: null,
        cadence: null,
        resourceCosts: [],
        effectOverrides: [],
        addedEffects: [],
        removedEffectKeys: [],
        multiHit: null,
        castProtections: [],
        triggeredActions: [],
        upgradeCost: null
    };
}

function createBlankUpgradeCost() {
    return {
        materials: [],
        pendingReason: ''
    };
}

function createBlankMaterial() {
    return {
        materialType: 'UniversalRefinedBook',
        quantity: 0,
        itemKey: '',
        note: ''
    };
}

function createCollectionItem(path) {
    const key = path.split('.').slice(-1)[0];
    switch (key) {
        case 'resourceCosts':
            return { resourceType: 'Mana', amount: 0, abortIfInsufficient: true };
        case 'conditionSynergies':
            return createBlankSynergy();
        case 'effects':
        case 'addedEffects':
            return createBlankConditionEffect();
        case 'effectOverrides':
            return createBlankConditionEffectOverride();
        case 'removedEffectKeys':
            return '';
        case 'castProtections':
            return createBlankProtection();
        case 'triggeredActions':
            return createBlankTriggeredAction();
        case 'pendingData':
            return createBlankPendingDatum();
        case 'securityNotes':
            return '';
        case 'materials':
            return createBlankMaterial();
        default:
            return {};
    }
}

function normalizeDefinition(definition) {
    definition.baseTuning = definition.baseTuning || {};
    definition.baseTuning.action = normalizeAction(definition.baseTuning.action || createBlankAction());
    definition.baseTuning.targeting = Object.assign(createBlankTargeting(), definition.baseTuning.targeting || {});
    definition.baseTuning.cadence = Object.assign(createBlankCadence(), definition.baseTuning.cadence || {});
    definition.baseTuning.resourceCosts = definition.baseTuning.resourceCosts || [];
    definition.baseTuning.effects = definition.baseTuning.effects || [];
    definition.baseTuning.castProtections = definition.baseTuning.castProtections || [];
    definition.baseTuning.triggeredActions = (definition.baseTuning.triggeredActions || []).map(normalizeTriggeredAction);
    definition.baseTuning.multiHit = definition.baseTuning.multiHit ? Object.assign(createBlankMultiHit(), definition.baseTuning.multiHit) : null;
    definition.ascensionOverrides = definition.ascensionOverrides || {};
    definition.elements = definition.elements || [];
    definition.roles = definition.roles || [];
    definition.metadata = definition.metadata || {};
    definition.pendingData = definition.pendingData || [];
    definition.securityNotes = definition.securityNotes || [];
    return definition;
}

function normalizeAction(action) {
    action.magnitudeProfile = Object.assign(createBlankMagnitudeProfile(), action.magnitudeProfile || {});
    action.conditionSynergies = action.conditionSynergies || [];
    return Object.assign(createBlankAction(), action || {});
}

function normalizeTriggeredAction(action) {
    action.action = normalizeAction(action.action || createBlankAction());
    action.effects = action.effects || [];
    return Object.assign(createBlankTriggeredAction(), action || {});
}

function metadataToEntries(metadata) {
    return Object.entries(metadata || {}).map(([key, value]) => ({ key, value }));
}

function entriesToMetadata(entries) {
    return entries.reduce((accumulator, entry) => {
        if (entry.key) {
            accumulator[entry.key] = entry.value || '';
        }
        return accumulator;
    }, {});
}

function readInputValue(target) {
    const valueType = target.dataset.valueType || (target.type === 'checkbox' ? 'boolean' : 'string');
    if (valueType === 'boolean') {
        return target.checked;
    }

    if (valueType === 'number') {
        return target.value === '' ? 0 : Number(target.value);
    }

    if (valueType === 'nullable-number') {
        return target.value === '' ? null : Number(target.value);
    }

    if (target.dataset.nullable === 'true' && target.value === '') {
        return null;
    }

    return target.value;
}

function getAtPath(object, segments) {
    return segments.reduce((current, segment) => current?.[segment], object);
}

function setAtPath(object, segments, value) {
    let current = object;
    for (let index = 0; index < segments.length - 1; index += 1) {
        const segment = segments[index];
        if (current[segment] == null) {
            current[segment] = Number.isInteger(Number(segments[index + 1])) ? [] : {};
        }
        current = current[segment];
    }
    current[segments[segments.length - 1]] = value;
}

function deepClone(value) {
    return JSON.parse(JSON.stringify(value));
}

function getSelectedClass() {
    return state.overview?.classes?.find((item) => item.classKey === state.selectedClassKey) || null;
}

function resolvePreviewStatus() {
    return state.preview?.completenessStatus || state.detail?.preview?.completenessStatus || 'Draft';
}

function resolvePublicationState() {
    return state.detail?.publication?.state || 'Draft';
}

function renderMetricCard(label, value, hint) {
    return `<article class="metric"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong><p>${escapeHtml(hint || '')}</p></article>`;
}

function renderMiniMetric(label, value) {
    return `<article class="metric"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></article>`;
}

function splitProtectionBlocks(value) {
    if (!value) {
        return [];
    }

    if (Array.isArray(value)) {
        return value;
    }

    return String(value)
        .split(',')
        .map((item) => item.trim())
        .filter(Boolean);
}

function escapeHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}

function setStatus(message) {
    elements.adminStatus.textContent = message;
}

function setBusy(isBusy) {
    state.isBusy = isBusy;
    updateToolbarState();
}

async function fetchJson(url, options = {}) {
    const response = await fetch(url, options);
    if (!response.ok) {
        let message = `Request failed with status ${response.status}.`;
        try {
            const payload = await response.json();
            message = payload.message || message;
        } catch {
            // ignore JSON parse issues
        }
        throw new Error(message);
    }

    return response.json();
}











