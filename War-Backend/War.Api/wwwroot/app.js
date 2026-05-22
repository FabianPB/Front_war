const state = {
    roster: null,
    commandIndex: new Map(),
    history: [],
    selectedCombatantKey: null,
    isBusy: false
};

const elements = {
    combatantACard: document.getElementById('combatant-a-card'),
    combatantBCard: document.getElementById('combatant-b-card'),
    commandList: document.getElementById('command-list'),
    pageStatus: document.getElementById('page-status'),
    resetButton: document.getElementById('reset-button'),
    logList: document.getElementById('log-list'),
    summaryBadge: document.getElementById('summary-badge'),
    infoOverlay: document.getElementById('info-overlay'),
    closeButton: document.getElementById('close-button'),
    panelTitle: document.getElementById('panel-title'),
    progressCard: document.getElementById('progress-card'),
    comboCard: document.getElementById('combo-card'),
    resourcesCard: document.getElementById('resources-card'),
    powerScoreCard: document.getElementById('power-score-card'),
    statsCard: document.getElementById('stats-card'),
    notesCard: document.getElementById('notes-card'),
    victoryOverlay: document.getElementById('victory-overlay'),
    victoryTitle: document.getElementById('victory-title'),
    victoryMessage: document.getElementById('victory-message'),
    victoryReset: document.getElementById('victory-reset'),
    configPanel: document.getElementById('config-panel')
};

document.addEventListener('DOMContentLoaded', () => {
    wireInteractions();
    loadRoster();
});

function wireInteractions() {
    elements.resetButton.addEventListener('click', () => void resetDemo());
    elements.victoryReset.addEventListener('click', () => {
        elements.victoryOverlay.hidden = true;
        void resetDemo();
    });

    elements.commandList.addEventListener('click', (event) => {
        const button = event.target.closest('[data-command-key]');
        if (!button) return;
        const command = state.commandIndex.get(button.dataset.commandKey);
        if (!command) return;
        void executeCommand(command);
    });

    document.addEventListener('click', (event) => {
        const infoButton = event.target.closest('[data-info-key]');
        if (infoButton) openPanel(infoButton.dataset.infoKey);

        const configButton = event.target.closest('[data-config-key]');
        if (configButton) void applyConfig(configButton.dataset.configKey);
    });

    elements.closeButton.addEventListener('click', closePanel);
    elements.infoOverlay.addEventListener('mousedown', (event) => {
        if (event.target === elements.infoOverlay) closePanel();
    });
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && !elements.infoOverlay.hidden) closePanel();
    });
}

// ── API Calls ────────────────────────────────────────────────

async function loadRoster() {
    setBusy(true);
    setStatus('Cargando combatientes de demo desde el backend...');
    try {
        const roster = await fetchJson('/api/characters/demo/combatants');
        applyRoster(roster);
        state.history = [];
        renderHistory();
        setStatus('Combatientes cargados. Elige una acción para resolver combate.');
    } catch (error) {
        console.error(error);
        setStatus('No se pudo cargar el roster de demo. Verifica la API y la base de datos.');
    } finally {
        setBusy(false);
    }
}

async function executeCommand(command) {
    if (state.isBusy || !command.isAvailable) return;
    setBusy(true);
    setStatus(`Resolviendo ${command.label}...`);
    try {
        const execution = await fetchJson('/api/combat/demo/execute', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
            body: JSON.stringify({
                attackerId: command.attackerId,
                targetId: command.targetId,
                actionType: command.actionType,
                skillId: command.skillId
            })
        });
        state.history.unshift(execution);
        state.history = state.history.slice(0, 20);
        applyRoster(execution.combatants);
        renderHistory();
        setStatus(`${execution.actionLabel} resuelto. Snapshots y log actualizados.`);

        // Check for victory
        if (execution.summary.targetDefeated) {
            showVictory(execution);
        }
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'No se pudo ejecutar la acción de combate.');
    } finally {
        setBusy(false);
    }
}

async function resetDemo() {
    if (state.isBusy) return;
    setBusy(true);
    setStatus('Restableciendo combatientes...');
    try {
        const roster = await fetchJson('/api/combat/demo/reset', {
            method: 'POST',
            headers: { Accept: 'application/json' }
        });
        applyRoster(roster);
        state.history = [];
        renderHistory();
        elements.victoryOverlay.hidden = true;
        setStatus('Demo restablecida. HP 100%, MP 100%, CDs a 0, efectos limpios, logs limpios.');
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'No se pudo restablecer el demo.');
    } finally {
        setBusy(false);
    }
}

async function applyConfig(combatantKey) {
    if (state.isBusy) return;
    setBusy(true);
    const classType = document.getElementById(`config-${combatantKey}-class`).value;
    const level = parseInt(document.getElementById(`config-${combatantKey}-level`).value) || 24;
    const ascension = parseInt(document.getElementById(`config-${combatantKey}-ascension`).value) || 1;
    setStatus(`Reconfigurando combatiente ${combatantKey.toUpperCase()} a ${classType} nivel ${level}...`);
    try {
        const roster = await fetchJson('/api/combat/demo/configure', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
            body: JSON.stringify({ combatantKey: combatantKey, classType, level, ascensionLevel: ascension })
        });
        applyRoster(roster);
        state.history = [];
        renderHistory();
        elements.victoryOverlay.hidden = true;
        setStatus(`Combatiente ${combatantKey.toUpperCase()} reconfigurado a ${classType} nivel ${level}, ascensión ${ascension}.`);
    } catch (error) {
        console.error(error);
        setStatus(error.message || 'No se pudo reconfigurar el combatiente.');
    } finally {
        setBusy(false);
    }
}

// ── State Management ─────────────────────────────────────────

function applyRoster(roster) {
    state.roster = roster;
    indexCommands(roster);
    renderCombatants(roster);
    renderCommands(roster);
    if (state.selectedCombatantKey) renderPanelForKey(state.selectedCombatantKey);
}

function indexCommands(roster) {
    state.commandIndex = new Map();
    for (const combatant of [roster.combatantA, roster.combatantB]) {
        for (const command of combatant.commands || []) {
            state.commandIndex.set(command.commandKey, command);
        }
    }
}

// ── Victory ──────────────────────────────────────────────────

function showVictory(execution) {
    const winner = execution.combatants.combatantA.snapshot.isDefeated
        ? execution.combatants.combatantB.snapshot.name
        : execution.combatants.combatantA.snapshot.name;
    const loser = execution.combatants.combatantA.snapshot.isDefeated
        ? execution.combatants.combatantA.snapshot.name
        : execution.combatants.combatantB.snapshot.name;
    elements.victoryTitle.textContent = '¡Victoria!';
    elements.victoryMessage.textContent = `${winner} ha derrotado a ${loser}. Daño total: ${execution.summary.totalDamageDisplay}. Curación total: ${execution.summary.totalHealingDisplay}.`;
    elements.victoryOverlay.hidden = false;
}

// ── Render: Combatant Cards ──────────────────────────────────

function renderCombatants(roster) {
    elements.combatantACard.innerHTML = renderCombatantCard(roster.combatantA);
    elements.combatantBCard.innerHTML = renderCombatantCard(roster.combatantB);
}

function renderCombatantCard(combatant) {
    const snapshot = combatant.snapshot;
    const hp = findResource(snapshot.resources, 'hp');
    const mana = findResource(snapshot.resources, 'mana');
    const ult = findResource(snapshot.resources, 'ultimateCharge');
    const conditions = combatant.activeConditions || [];
    const protections = combatant.activeProtections || [];
    const isStunned = conditions.some(c =>
        c.condition === 'Stun' || c.condition === 'Freeze' || c.condition === 'Paralyze');

    let shellClass = 'combatant-card__shell';
    if (snapshot.isDefeated) shellClass += ' combatant-card__shell--defeated';
    else if (isStunned) shellClass += ' combatant-card__shell--stunned';

    const statusChip = snapshot.isDefeated
        ? '<span class="status-chip status-chip--danger">Derrotado</span>'
        : '<span class="status-chip status-chip--ready">Listo</span>';

    return `
        <div class="${shellClass}">
            <div class="combatant-card__header">
                <div>
                    <p class="eyebrow">${escapeHtml(combatant.label)}</p>
                    <h2>${escapeHtml(snapshot.name)}</h2>
                    <p class="combatant-subtitle">${escapeHtml(snapshot.classLabel)} · Nivel ${snapshot.progress.level}</p>
                </div>
                <div class="combatant-card__controls">
                    ${statusChip}
                    <button class="info-button" type="button" aria-label="Ver detalles de ${escapeHtml(snapshot.name)}" data-info-key="${escapeHtml(combatant.key)}">i</button>
                </div>
            </div>

            ${renderResourceBars(hp, mana, ult)}
            ${renderConditionBadges(conditions)}
            ${renderProtectionBadges(protections)}
            ${isStunned ? '<div class="stun-indicator">⚠ NO PUEDE ACTUAR — Aturdido/Congelado/Paralizado</div>' : ''}
        </div>`;
}

function renderResourceBars(hp, mana, ult) {
    let bars = '<div class="resource-bars">';

    if (hp) {
        const hpPercent = hp.maximum > 0 ? (hp.current / hp.maximum) * 100 : 0;
        const hpClass = hpPercent > 60 ? 'resource-bar__fill--hp-high'
            : hpPercent > 30 ? 'resource-bar__fill--hp-mid'
            : 'resource-bar__fill--hp-low';
        bars += renderSingleBar('HP', hp.current, hp.maximum, hp.displayValue, hpClass, hpPercent);
    }

    if (mana) {
        const manaPercent = mana.maximum > 0 ? (mana.current / mana.maximum) * 100 : 0;
        bars += renderSingleBar('Mana', mana.current, mana.maximum, mana.displayValue, 'resource-bar__fill--mana', manaPercent);
    }

    if (ult) {
        const ultPercent = ult.maximum > 0 ? (ult.current / ult.maximum) * 100 : 0;
        bars += renderSingleBar('Ultimate', ult.current, ult.maximum, ult.displayValue, 'resource-bar__fill--ult', ultPercent);
    }

    bars += '</div>';
    return bars;
}

function renderSingleBar(label, current, maximum, displayValue, fillClass, percent) {
    return `
        <div class="resource-bar">
            <div class="resource-bar__header">
                <span class="resource-bar__label">${escapeHtml(label)}</span>
                <span class="resource-bar__value">${escapeHtml(displayValue)}</span>
            </div>
            <div class="resource-bar__track">
                <div class="resource-bar__fill ${fillClass}" style="width:${clampPercentage(percent)}%"></div>
            </div>
        </div>`;
}

function renderConditionBadges(conditions) {
    if (!conditions || conditions.length === 0) return '';
    return `<div class="condition-list">${conditions.map(c => {
        const badgeClass = c.category === 'CrowdControl' ? 'condition-badge--cc' : 'condition-badge--state';
        return `<span class="condition-badge ${badgeClass}">${escapeHtml(c.displayLabel)} <span class="condition-badge__turns">${c.remainingTurns}t</span></span>`;
    }).join('')}</div>`;
}

function renderProtectionBadges(protections) {
    if (!protections || protections.length === 0) return '';
    return `<div class="protection-list">${protections.map(p =>
        `<span class="protection-badge">🛡 ${escapeHtml(p.type)} <span class="condition-badge__turns">${p.remainingTurns}t</span></span>`
    ).join('')}</div>`;
}

// ── Render: Commands ─────────────────────────────────────────

function renderCommands(roster) {
    const groups = [roster.combatantA, roster.combatantB];
    const rosterNotes = dedupeStrings(roster.notes || []);
    elements.commandList.innerHTML = `
        <div class="command-groups">
            ${groups.map(renderCommandGroup).join('')}
        </div>
        <div class="command-notes">
            <h3>Notas del demo</h3>
            ${rosterNotes.length === 0
                ? '<p class="empty-state">Sin notas de roster.</p>'
                : `<div class="note-list note-list--compact">${rosterNotes.map(renderNoteItem).join('')}</div>`}
        </div>`;
}

function renderCommandGroup(combatant) {
    return `
        <section class="command-group">
            <div class="command-group__header">
                <p class="eyebrow">${escapeHtml(combatant.label)}</p>
                <h3>${escapeHtml(combatant.snapshot.name)}</h3>
            </div>
            <div class="command-group__list">
                ${(combatant.commands || []).map(renderCommandButton).join('')}
            </div>
        </section>`;
}

function renderCommandButton(command) {
    const disabled = state.isBusy || !command.isAvailable;
    const classes = ['command-button'];
    if (command.isPrimary) classes.push('command-button--primary');

    let metaHtml = '';
    if (command.skillMetadata) {
        const meta = command.skillMetadata;
        const costPart = meta.manaCost > 0
            ? `<span class="command-button__cost">💧 ${formatNumber(meta.manaCost)} MP</span>` : '';
        const cdClass = meta.isOnCooldown ? 'command-button__cooldown--active' : '';
        const cdText = meta.isOnCooldown
            ? `⏳ CD: ${meta.remainingCooldownTurns}/${meta.cooldownTurns}t`
            : `⏱ CD: ${meta.cooldownTurns}t`;
        const cdPart = meta.cooldownTurns > 0
            ? `<span class="command-button__cooldown ${cdClass}">${cdText}</span>` : '';
        metaHtml = `<div class="command-button__meta">${costPart}${cdPart}</div>`;
    }

    return `
        <button
            class="${classes.join(' ')}"
            type="button"
            data-command-key="${escapeHtml(command.commandKey)}"
            ${disabled ? 'disabled' : ''}>
            <span class="command-button__eyebrow">${escapeHtml(command.actionType === 'Skill' ? 'Habilidad' : 'Ataque básico')}</span>
            <strong class="command-button__title">${escapeHtml(command.label)}</strong>
            ${metaHtml}
            ${disabled && command.disabledReason
                ? `<span class="command-button__disabled">${escapeHtml(command.disabledReason)}</span>` : ''}
        </button>`;
}

// ── Render: Combat Log ───────────────────────────────────────

function renderHistory() {
    if (state.history.length === 0) {
        elements.summaryBadge.textContent = 'Esperando acción';
        elements.logList.innerHTML = '<article class="log-empty">Ejecuta una acción para ver la resolución del combate y los cambios de recursos persistidos.</article>';
        return;
    }
    const latest = state.history[0];
    elements.summaryBadge.textContent = `${latest.summary.outcomeLabel} · ${latest.summary.totalDamageDisplay} daño`;
    elements.logList.innerHTML = state.history.map(renderExecutionBlock).join('');
}

function renderExecutionBlock(execution) {
    const notes = dedupeStrings(execution.notes || []);
    return `
        <article class="execution-block">
            <header class="execution-block__header">
                <div>
                    <p class="eyebrow">${escapeHtml(execution.actionLabel)}</p>
                    <h3>${escapeHtml(execution.summary.outcomeLabel)}</h3>
                </div>
                <div class="execution-summary-grid">
                    ${renderMiniStat('Daño', execution.summary.totalDamageDisplay)}
                    ${renderMiniStat('Curación', execution.summary.totalHealingDisplay)}
                    ${renderMiniStat('Eventos', String(execution.summary.eventCount))}
                    ${renderMiniStat('HP objetivo', execution.summary.lastTargetHpDisplay)}
                </div>
            </header>
            ${notes.length === 0 ? '' : `<div class="note-list note-list--compact">${notes.map(renderNoteItem).join('')}</div>`}
            <div class="entry-list">
                ${(execution.logEntries || []).map(renderLogEntry).join('')}
            </div>
        </article>`;
}

function renderLogEntry(entry) {
    // Determine log entry CSS class based on outcome
    let entryClass = 'log-entry';
    if (entry.wasCritical) entryClass += ' log-entry--critical';
    else if (entry.healing > 0 && entry.damage === 0) entryClass += ' log-entry--heal';
    else if (entry.outcome === 'Miss') entryClass += ' log-entry--miss';
    else if (entry.outcome === 'Blocked') entryClass += ' log-entry--blocked';
    if (entry.targetDefeated) entryClass += ' log-entry--defeat';

    const tags = [entry.phase];
    if (entry.outcome !== 'Info') tags.push(entry.outcome);
    if (entry.comboStage && entry.comboLength) tags.push(`Combo ${entry.comboStage}/${entry.comboLength}`);
    if (entry.comboContinued) tags.push('Combo Continue');
    if (entry.comboResetBeforeExecution) tags.push('Combo Reset');
    if (entry.wasHit) tags.push('Hit');
    if (entry.wasCritical) tags.push('Crit');
    if (entry.targetDefeated) tags.push('Derrota');

    // Build summary with visual indicators
    let summaryHtml = escapeHtml(entry.summary);
    if (entry.wasCritical) {
        summaryHtml += '<span class="critical-label">CRÍTICO</span>';
    }
    if (entry.outcome === 'Miss') {
        summaryHtml = '<strong style="color:#9ca3af;font-size:1.2rem">MISS</strong> — ' + summaryHtml;
    }

    return `
        <article class="${entryClass}">
            <div class="log-entry__header">
                <div class="tag-row">${tags.map(renderTag).join('')}</div>
                <span class="log-entry__sequence">#${entry.sequence}</span>
            </div>
            <p class="log-entry__summary">${summaryHtml}</p>
            <div class="log-entry__metrics">
                ${renderMiniStat('Daño', entry.damageDisplay)}
                ${renderMiniStat('Curación', entry.healingDisplay)}
                ${renderMiniStat('HP objetivo', entry.targetRemainingHpDisplay || 'Sin cambio')}
            </div>
            ${entry.appliedEffects && entry.appliedEffects.length > 0
                ? `<div class="effect-row">${entry.appliedEffects.map(e => `<span class="effect-pill">${escapeHtml(e)}</span>`).join('')}</div>`
                : ''}
            ${entry.resourceChanges && entry.resourceChanges.length > 0
                ? `<div class="resource-change-list">${entry.resourceChanges.map(renderResourceChange).join('')}</div>`
                : ''}
        </article>`;
}

function renderTag(value) {
    return `<span class="tag">${escapeHtml(value)}</span>`;
}

function renderResourceChange(change) {
    return `
        <article class="resource-change">
            <span class="resource-change__label">${escapeHtml(change.entityRole)} · ${escapeHtml(change.resourceLabel)}</span>
            <strong class="resource-change__value">${escapeHtml(change.deltaDisplay)} → ${escapeHtml(change.newValueDisplay)}</strong>
            <span class="resource-change__reason">${escapeHtml(change.reasonLabel)}</span>
        </article>`;
}

// ── Render: Info Panel ───────────────────────────────────────

function openPanel(combatantKey) {
    state.selectedCombatantKey = combatantKey;
    renderPanelForKey(combatantKey);
    elements.infoOverlay.hidden = false;
    document.body.style.overflow = 'hidden';
}

function closePanel() {
    state.selectedCombatantKey = null;
    elements.infoOverlay.hidden = true;
    document.body.style.overflow = '';
}

function renderPanelForKey(combatantKey) {
    const combatant = getCombatant(combatantKey);
    if (!combatant) { closePanel(); return; }
    const snapshot = combatant.snapshot;
    elements.panelTitle.textContent = `${snapshot.name} — Snapshot`;
    elements.progressCard.innerHTML = renderProgressCard(snapshot);
    elements.comboCard.innerHTML = renderComboCard(snapshot);
    elements.resourcesCard.innerHTML = renderResourcesCard(snapshot);
    elements.powerScoreCard.innerHTML = renderPowerScore(snapshot.powerScore);
    elements.statsCard.innerHTML = `
        <h3>Stats finales</h3>
        <div class="stat-section-grid">
            ${(snapshot.statSections || []).map(renderStatSection).join('')}
        </div>`;
    elements.notesCard.innerHTML = renderNotesCard(snapshot, combatant);
}

function renderProgressCard(snapshot) {
    return `
        <h3>Progreso</h3>
        <div class="mini-grid">
            ${renderMiniStat('Clase', snapshot.classLabel)}
            ${renderMiniStat('Nivel', String(snapshot.progress.level))}
            ${renderMiniStat('XP actual', formatInteger(snapshot.progress.currentXp))}
            ${renderMiniStat('XP al sig.', formatInteger(snapshot.progress.xpToNextLevel))}
            ${renderMiniStat('XP total', formatInteger(snapshot.progress.totalXp))}
            ${renderMiniStat('Progreso', `${clampPercentage(snapshot.progress.progressRatio * 100).toFixed(2)}%`)}
        </div>`;
}

function renderComboCard(snapshot) {
    const combo = snapshot.basicCombo;
    if (!combo) return `<h3>Combo básico</h3><p class="empty-state">Sin estado de combo disponible.</p>`;
    return `
        <h3>Combo básico</h3>
        <div class="mini-grid">
            ${renderMiniStat('Sig. etapa', `${combo.nextStage}/${combo.comboLength}`)}
            ${renderMiniStat('Última completada', combo.lastCompletedStage > 0 ? `${combo.lastCompletedStage}/${combo.comboLength}` : 'Ninguna')}
            ${renderMiniStat('Ventana', `${combo.continuationWindowSeconds}s`)}
            ${renderMiniStat('Cast', `${combo.castTimeSeconds}s`)}
            ${renderMiniStat('Ventana activa', combo.isContinuationWindowActive ? 'Sí' : 'No')}
            ${renderMiniStat('Restante', combo.windowRemainingSeconds != null ? `${formatNumber(combo.windowRemainingSeconds)}s` : 'Expirada')}
        </div>`;
}

function renderResourcesCard(snapshot) {
    return `
        <h3>Recursos</h3>
        <div class="resource-list">
            ${(snapshot.resources || []).map(renderResourcePill).join('')}
        </div>`;
}

function renderPowerScore(powerScore) {
    if (!powerScore) return `<h3>Power Score</h3><p class="empty-state">Power Score no disponible.</p>`;
    return `
        <h3>Power Score</h3>
        <div class="mini-grid">
            ${renderMiniStat('Total', powerScore.totalDisplay)}
            ${renderMiniStat('Modo', 'Referencial')}
        </div>
        <div class="score-category-list">${(powerScore.categories || []).map(renderScoreCategory).join('')}</div>
        <h3>Top contribuyentes</h3>
        ${(powerScore.topStats || []).length === 0
            ? '<p class="empty-state">Sin contribuciones positivas reportadas.</p>'
            : `<div class="score-top-list">${powerScore.topStats.map(renderTopStat).join('')}</div>`}`;
}

function renderMiniStat(label, value) {
    return `<article class="mini-stat"><span class="mini-stat__label">${escapeHtml(label)}</span><strong class="mini-stat__value">${escapeHtml(value)}</strong></article>`;
}

function renderResourcePill(resource) {
    return `<article class="resource-pill"><span class="resource-pill__label">${escapeHtml(resource.label)}</span><strong class="resource-pill__value">${escapeHtml(resource.displayValue)}</strong></article>`;
}

function renderScoreCategory(category) {
    return `<article class="score-row"><span class="score-row__label">${escapeHtml(category.label)}</span><strong class="score-row__value">${escapeHtml(category.contributionDisplay)}</strong><span class="summary-tile__hint">${escapeHtml(category.shareDisplay)} del total</span></article>`;
}

function renderTopStat(stat) {
    return `<article class="score-row"><span class="score-row__label">${escapeHtml(stat.label)}</span><strong class="score-row__value">${escapeHtml(stat.contributionDisplay)}</strong><span class="summary-tile__hint">Valor: ${escapeHtml(stat.statValueDisplay)}</span></article>`;
}

function renderStatSection(section) {
    return `<section class="stat-section"><h4>${escapeHtml(section.label)}</h4><div class="stat-list">${(section.stats || []).map(renderStatRow).join('')}</div></section>`;
}

function renderStatRow(stat) {
    return `<div class="${stat.isZero ? 'stat-row stat-row--muted' : 'stat-row'}"><span class="stat-row__label">${escapeHtml(stat.label)}</span><strong class="stat-row__value">${escapeHtml(stat.displayValue)}</strong></div>`;
}

function renderNotesCard(snapshot, combatant) {
    const notes = dedupeStrings([...(combatant.notes || []), ...(snapshot.notes || []), ...((snapshot.powerScore && snapshot.powerScore.notes) || [])]);
    return `<h3>Notas</h3>${notes.length === 0 ? '<p class="empty-state">Sin notas adicionales.</p>' : `<div class="note-list">${notes.map(renderNoteItem).join('')}</div>`}`;
}

function renderNoteItem(note) {
    return `<article class="note-item">${escapeHtml(note)}</article>`;
}

// ── Helpers ──────────────────────────────────────────────────

function getCombatant(key) {
    if (!state.roster) return null;
    return [state.roster.combatantA, state.roster.combatantB].find(c => c.key === key) || null;
}

function findResource(resources, key) {
    return (resources || []).find(r => r.key === key) || null;
}

function setBusy(isBusy) {
    state.isBusy = isBusy;
    elements.resetButton.disabled = isBusy;
    document.querySelectorAll('.config-apply-button').forEach(b => b.disabled = isBusy);
    if (state.roster) renderCommands(state.roster);
}

function setStatus(message) {
    elements.pageStatus.textContent = message;
}

async function fetchJson(url, options) {
    const response = await fetch(url, {
        ...(options || {}),
        headers: { Accept: 'application/json', ...((options && options.headers) || {}) }
    });
    if (!response.ok) {
        const message = await readErrorMessage(response);
        throw new Error(message || `Request to ${url} failed with status ${response.status}.`);
    }
    return response.json();
}

async function readErrorMessage(response) {
    try {
        const payload = await response.json();
        if (payload && typeof payload.message === 'string') return payload.message;
    } catch { return null; }
    return null;
}

function dedupeStrings(values) {
    return [...new Set((values || []).filter(v => typeof v === 'string' && v.trim().length > 0))];
}

function escapeHtml(value) {
    return String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#39;');
}

function formatInteger(value) {
    return new Intl.NumberFormat('es-ES', { maximumFractionDigits: 0 }).format(Number(value || 0));
}

function formatNumber(value) {
    return new Intl.NumberFormat('es-ES', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(Number(value || 0));
}

function clampPercentage(value) {
    return Math.min(100, Math.max(0, Number(value || 0)));
}
