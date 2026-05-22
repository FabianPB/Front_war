// ═══════════════════════════════════════════════════════════════════
// WAR — Mundo Online Multijugador (Bloque 2 — Frontend completo)
// ═══════════════════════════════════════════════════════════════════

const WORLD_SIZE = 100;
const MOVE_INTERVAL_MS = 250;
const NEARBY_POLL_MS = 2000;
const SNAPSHOT_POLL_MS = 5000;
const VIEW_RANGE_X = 40;          // World units visible horizontally
const VIEW_RANGE_Y = 30;          // World units visible vertically
const COMBAT_RANGE = 20;          // Max range to select/attack target
const MAX_LOG_ENTRIES = 20;
const SKILLS_PER_PAGE = 6;

const CLASS_COLORS = {
    Sorcerer: '#818cf8', Juramentada: '#f59e0b',
    Lancero: '#2DD4BF', Bruiser: '#ef4444', Warrior: '#ef4444'
};

// ── State ──
const S = {
    connection: null,
    myPlayer: null,
    allPlayers: new Map(),
    nearbyPlayers: [],
    targetPlayerId: null,
    selectedClass: null,
    chatMessages: [],
    combatLog: [],
    keysDown: new Set(),
    skillPage: 0,
    moveInterval: null,
    nearbyInterval: null,
    snapshotInterval: null,
    isTypingChat: false,
    skillCatalog: null,
    gamePaused: false,
    canvasW: 0,
    canvasH: 0,
    // Interpolation
    playerPositions: new Map()  // playerId → {x, y, tx, ty}
};

// ── DOM ──
const $ = id => document.getElementById(id);
const els = {
    loginScreen:     $('login-screen'),
    gameScreen:      $('game-screen'),
    inputName:       $('input-name'),
    btnJoin:         $('btn-join'),
    loginError:      $('login-error'),
    skillPreview:    $('skill-preview'),
    skillPreviewTitle: $('skill-preview-title'),
    skillPreviewList: $('skill-preview-list'),
    // HUD
    hudName:         $('hud-name'),
    hudClassLevel:   $('hud-class-level'),
    hudHpFill:       $('hud-hp-fill'),
    hudHpText:       $('hud-hp-text'),
    hudMpFill:       $('hud-mp-fill'),
    hudMpText:       $('hud-mp-text'),
    hudConditions:   $('hud-conditions'),
    onlineCount:     $('online-count'),
    coordsDisplay:   $('coords-display'),
    // Canvas
    canvas:          $('world-canvas'),
    // Target
    targetBar:       $('target-bar'),
    targetName:      $('target-name'),
    targetClass:     $('target-class'),
    targetHpFill:    $('target-hp-fill'),
    targetHpText:    $('target-hp-text'),
    // Sidebar
    nearbyCount:     $('nearby-count'),
    nearbyList:      $('nearby-list'),
    chatLog:         $('chat-log'),
    chatInput:       $('chat-input'),
    // Skills
    skillSlots:      $('skill-slots'),
    btnSkillPrev:    $('btn-skill-prev'),
    btnSkillNext:    $('btn-skill-next'),
    skillPageLabel:  $('skill-page-label'),
    btnUltimate:     $('btn-ultimate'),
    // Combat log
    combatLogList:   $('combat-log-list'),
    // Context menu
    contextMenu:     $('context-menu'),
    // Overlays
    controlsOverlay: $('controls-overlay')
};

// ═══════════════════════════════════════════════════════════════════
// SKILL CATALOG (client-side for preview)
// ═══════════════════════════════════════════════════════════════════
async function loadSkillPreview(className) {
    // Fetch from server or use the data returned on join
    // For now, we'll show a loading state and fill it after join
    els.skillPreviewTitle.textContent = `Habilidades — ${className}`;
    els.skillPreviewList.innerHTML = '<p style="color:var(--ink-muted);font-size:.85rem">Las habilidades se cargarán al entrar al mundo.</p>';
    els.skillPreview.hidden = false;
}

function renderSkillPreviewFromState() {
    if (!S.myPlayer || !S.myPlayer.skills) return;
    els.skillPreviewList.innerHTML = S.myPlayer.skills.map((s, i) => {
        const isUlt = i === S.myPlayer.skills.length - 1;
        return `<div class="skill-preview-item ${isUlt ? 'skill-preview-item--ult' : ''}">
            <span class="skill-preview-item__name">${isUlt ? '★ ' : ''}${esc(s.name)}</span>
            <span class="skill-preview-item__meta">Maná: ${fmt(s.manaCost)} · CD: ${fmt(s.baseCooldownSeconds)}s · ${esc(s.damageType)}</span>
        </div>`;
    }).join('');
}

// ═══════════════════════════════════════════════════════════════════
// SIGNALR
// ═══════════════════════════════════════════════════════════════════
function createConnection() {
    S.connection = new signalR.HubConnectionBuilder()
        .withUrl(window.location.origin + '/game')
        .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    S.connection.on('PlayerJoined', p => {
        S.allPlayers.set(p.playerId, p);
        initPlayerInterp(p);
        addSystemMsg(`${p.displayName} (${p.className}) se unió.`);
        updateOnlineCount();
    });

    S.connection.on('PlayerLeft', d => {
        S.allPlayers.delete(d.playerId);
        S.playerPositions.delete(d.playerId);
        if (S.targetPlayerId === d.playerId) deselectTarget();
        addSystemMsg(`${d.displayName} se fue.`);
        updateOnlineCount();
    });

    S.connection.on('PlayerMoved', d => {
        const p = S.allPlayers.get(d.playerId);
        if (p) { p.x = d.x; p.y = d.y; updateInterp(d.playerId, d.x, d.y); }
    });

    S.connection.on('MoveResult', r => {
        if (!S.myPlayer) return;
        S.myPlayer.x = r.x; S.myPlayer.y = r.y;
        S.nearbyPlayers = r.nearbyPlayers || [];
        updateCoords(); renderNearby();
    });

    S.connection.on('PlayerStateUpdate', state => {
        if (!S.myPlayer) return;
        if (state.playerId === S.myPlayer.playerId) {
            Object.assign(S.myPlayer, state);
            updateHud(); renderSkills();
        }
        // Update in allPlayers too
        const p = S.allPlayers.get(state.playerId);
        if (p) { p.currentHp = state.currentHp; p.maxHp = state.maxHp; }
        updateTargetBar();
    });

    S.connection.on('TargetStateUpdate', p => {
        const existing = S.allPlayers.get(p.playerId);
        if (existing) { Object.assign(existing, p); }
        updateTargetBar();
    });

    S.connection.on('CombatResult', r => {
        addCombatLog(r);
        updateTargetBar();
    });

    S.connection.on('ChatMessage', msg => {
        addChatMsg(msg.displayName, msg.className, msg.message);
    });

    S.connection.on('Error', msg => {
        addCombatLogEntry('system', msg);
    });

    S.connection.on('SkillUpgraded', d => {
        addCombatLogEntry('system', `Ascensión subida a nivel ${d.ascensionLevel}.`);
    });
    S.connection.on('SkillDowngraded', d => {
        addCombatLogEntry('system', `Ascensión bajada a nivel ${d.ascensionLevel}.`);
    });

    // ── Social notifications ──
    S.connection.on('SocialNotification', n => {
        switch (n.type) {
            case 'FriendRequestSent':
                addSystemMsg('Solicitud de amistad enviada.');
                break;
            case 'FriendRequestReceived':
                addSystemMsg(`${n.senderName} (${n.senderClassName} Lv.${n.senderLevel}) te envió una solicitud de amistad.`);
                // Auto-accept for demo simplicity
                showFriendRequestDialog(n);
                break;
            case 'FriendRequestResponse':
                addSystemMsg(`Solicitud de amistad ${n.action}.`);
                break;
            case 'PlayerBlocked':
                addSystemMsg(`${n.targetName} ha sido bloqueado.`);
                break;
            case 'PlayerUnblocked':
                addSystemMsg('Jugador desbloqueado.');
                break;
        }
    });

    S.connection.on('SocialError', msg => {
        addSystemMsg(`⚠ ${msg}`);
    });

    S.connection.onreconnecting(() => addSystemMsg('Reconectando...'));
    S.connection.onreconnected(() => { addSystemMsg('Reconectado.'); rejoin(); });
    S.connection.onclose(() => { addSystemMsg('Desconectado.'); stopPolling(); });
}

async function rejoin() {
    if (!S.myPlayer) return;
    try {
        const r = await S.connection.invoke('JoinGame', S.myPlayer.displayName, S.myPlayer.className, S.myPlayer.level, S.myPlayer.ascensionLevel, S.myPlayer.gender || null);
        applyJoinResult(r);
    } catch(e) { console.error('Rejoin failed:', e); }
}

// ═══════════════════════════════════════════════════════════════════
// JOIN
// ═══════════════════════════════════════════════════════════════════
async function joinWorld() {
    const name = els.inputName.value.trim();
    if (name.length < 3) { showLoginError('El nombre debe tener al menos 3 caracteres.'); return; }
    if (!S.selectedClass) { showLoginError('Selecciona una clase.'); return; }

    els.btnJoin.disabled = true;
    els.btnJoin.textContent = 'Conectando...';
    hideLoginError();

    try {
        createConnection();
        await S.connection.start();
        const result = await S.connection.invoke('JoinGame', name, S.selectedClass, 30, 5, S.selectedGender || null);
        applyJoinResult(result);

        els.loginScreen.hidden = true;
        els.gameScreen.hidden = false;
        resizeCanvas();
        startPolling();
        startMovementLoop();
        startRenderLoop();
        addSystemMsg(`¡Bienvenido, ${result.player.displayName}!`);
        setTimeout(() => Tutorial.start(), 800);
    } catch(e) {
        console.error('Join failed:', e);
        showLoginError(e.message || 'No se pudo conectar.');
        els.btnJoin.disabled = false;
        els.btnJoin.textContent = 'ENTRAR AL MUNDO';
    }
}

function applyJoinResult(result) {
    S.myPlayer = result.player;
    S.allPlayers.clear();
    S.playerPositions.clear();
    for (const p of result.worldSnapshot.players) {
        S.allPlayers.set(p.playerId, p);
        initPlayerInterp(p);
    }
    updateHud(); updateOnlineCount(); updateCoords();
    renderSkills(); renderSkillPreviewFromState();
}

// ═══════════════════════════════════════════════════════════════════
// MOVEMENT
// ═══════════════════════════════════════════════════════════════════
function startMovementLoop() {
    S.moveInterval = setInterval(() => {
        if (!S.connection || S.connection.state !== 'Connected' || S.isTypingChat || S.gamePaused) return;
        const dirs = [];
        if (S.keysDown.has('w') || S.keysDown.has('arrowup')) dirs.push('up');
        if (S.keysDown.has('s') || S.keysDown.has('arrowdown')) dirs.push('down');
        if (S.keysDown.has('a') || S.keysDown.has('arrowleft')) dirs.push('left');
        if (S.keysDown.has('d') || S.keysDown.has('arrowright')) dirs.push('right');
        // Send each direction (diagonal = 2 moves)
        for (const d of dirs) S.connection.invoke('Move', d).catch(() => {});
    }, MOVE_INTERVAL_MS);
}

// ═══════════════════════════════════════════════════════════════════
// POLLING
// ═══════════════════════════════════════════════════════════════════
function startPolling() {
    S.nearbyInterval = setInterval(async () => {
        if (!S.connection || S.connection.state !== 'Connected') return;
        try {
            const nearby = await S.connection.invoke('GetNearbyPlayers', null);
            S.nearbyPlayers = nearby; renderNearby();
        } catch {}
    }, NEARBY_POLL_MS);

    S.snapshotInterval = setInterval(async () => {
        if (!S.connection || S.connection.state !== 'Connected') return;
        try {
            const snap = await S.connection.invoke('GetWorldSnapshot');
            // Preserve target even if not in snapshot (may be far but still valid)
            const savedTarget = S.targetPlayerId;
            const savedTargetData = savedTarget ? S.allPlayers.get(savedTarget) : null;
            S.allPlayers.clear();
            for (const p of snap.players) {
                S.allPlayers.set(p.playerId, p);
                updateInterp(p.playerId, p.x, p.y);
            }
            // Restore target if it was lost from snapshot
            if (savedTarget && savedTargetData && !S.allPlayers.has(savedTarget)) {
                S.allPlayers.set(savedTarget, savedTargetData);
            }
            updateOnlineCount();
        } catch {}
    }, SNAPSHOT_POLL_MS);
}

function stopPolling() {
    clearInterval(S.nearbyInterval);
    clearInterval(S.snapshotInterval);
    clearInterval(S.moveInterval);
}

// ═══════════════════════════════════════════════════════════════════
// CANVAS RENDERING
// ═══════════════════════════════════════════════════════════════════
function resizeCanvas() {
    const area = els.canvas.parentElement;
    const maxW = area.clientWidth - 8;
    const maxH = area.clientHeight - 8;
    const size = Math.min(maxW, maxH);
    const s = Math.max(200, size);
    els.canvas.width = s; els.canvas.height = s;
    S.canvasW = s; S.canvasH = s;
}

function startRenderLoop() {
    const ctx = els.canvas.getContext('2d');
    function frame() { renderWorld(ctx); requestAnimationFrame(frame); }
    requestAnimationFrame(frame);
}

function worldToScreen(wx, wy) {
    if (!S.myPlayer) return [0, 0];
    const cx = S.myPlayer.x, cy = S.myPlayer.y;
    const sx = ((wx - cx) / VIEW_RANGE_X + 0.5) * S.canvasW;
    const sy = ((wy - cy) / VIEW_RANGE_Y + 0.5) * S.canvasH;
    return [sx, sy];
}

function screenToWorld(sx, sy) {
    if (!S.myPlayer) return [0, 0];
    const cx = S.myPlayer.x, cy = S.myPlayer.y;
    const wx = (sx / S.canvasW - 0.5) * VIEW_RANGE_X + cx;
    const wy = (sy / S.canvasH - 0.5) * VIEW_RANGE_Y + cy;
    return [wx, wy];
}

function renderWorld(ctx) {
    const w = S.canvasW, h = S.canvasH;
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = '#080c14'; ctx.fillRect(0, 0, w, h);
    if (!S.myPlayer) return;

    // Tick position interpolation for smooth movement
    tickInterpolation();

    // Grid
    drawGrid(ctx, w, h);

    // World boundary
    drawWorldBounds(ctx, w, h);

    // Other players
    for (const [id, p] of S.allPlayers) {
        if (S.myPlayer && id === S.myPlayer.playerId) continue;
        const interp = S.playerPositions.get(id);
        const px = interp ? interp.x : p.x;
        const py = interp ? interp.y : p.y;
        drawOtherPlayer(ctx, px, py, p);
    }

    // Self
    drawSelf(ctx);
}

function drawGrid(ctx, w, h) {
    ctx.strokeStyle = 'rgba(255,255,255,.04)'; ctx.lineWidth = 1;
    for (let gx = 0; gx <= WORLD_SIZE; gx += 5) {
        const [sx] = worldToScreen(gx, 0);
        if (sx >= 0 && sx <= w) { ctx.beginPath(); ctx.moveTo(sx, 0); ctx.lineTo(sx, h); ctx.stroke(); }
    }
    for (let gy = 0; gy <= WORLD_SIZE; gy += 5) {
        const [, sy] = worldToScreen(0, gy);
        if (sy >= 0 && sy <= h) { ctx.beginPath(); ctx.moveTo(0, sy); ctx.lineTo(w, sy); ctx.stroke(); }
    }
}

function drawWorldBounds(ctx, w, h) {
    const [x0, y0] = worldToScreen(0, 0);
    const [x1, y1] = worldToScreen(WORLD_SIZE, WORLD_SIZE);
    ctx.strokeStyle = 'rgba(190,96,57,.2)'; ctx.lineWidth = 2;
    ctx.strokeRect(x0, y0, x1 - x0, y1 - y0);
}

function drawOtherPlayer(ctx, wx, wy, p) {
    const [sx, sy] = worldToScreen(wx, wy);
    if (sx < -20 || sx > S.canvasW + 20 || sy < -20 || sy > S.canvasH + 20) return;

    const color = CLASS_COLORS[p.className] || '#888';
    const isTarget = S.targetPlayerId === p.playerId;
    const r = isTarget ? 7 : 5;

    // Selection ring
    if (isTarget) {
        ctx.beginPath(); ctx.arc(sx, sy, r + 5, 0, Math.PI * 2);
        ctx.strokeStyle = '#fff'; ctx.lineWidth = 2; ctx.stroke();
    }

    // Glow
    ctx.beginPath(); ctx.arc(sx, sy, r + 3, 0, Math.PI * 2);
    ctx.fillStyle = hexAlpha(color, .18); ctx.fill();

    // Dot (yellow for others)
    ctx.beginPath(); ctx.arc(sx, sy, r, 0, Math.PI * 2);
    ctx.fillStyle = '#fbbf24'; ctx.fill();

    // HP bar
    if (p.maxHp > 0) {
        const hpR = Math.max(0, Math.min(1, Number(p.currentHp) / Number(p.maxHp)));
        const bw = 28, bh = 3, bx = sx - bw / 2, by = sy - r - 9;
        ctx.fillStyle = 'rgba(220,38,38,.35)'; ctx.fillRect(bx, by, bw, bh);
        ctx.fillStyle = hpR > .6 ? '#2DD4BF' : hpR > .3 ? '#eab308' : '#dc2626';
        ctx.fillRect(bx, by, bw * hpR, bh);
    }

    // Name
    ctx.fillStyle = isTarget ? '#fff' : '#c9d1d9';
    ctx.font = isTarget ? 'bold 10px sans-serif' : '9px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(p.displayName, sx, sy - r - 13);
}

function drawSelf(ctx) {
    if (!S.myPlayer) return;
    const [sx, sy] = worldToScreen(S.myPlayer.x, S.myPlayer.y);
    const color = CLASS_COLORS[S.myPlayer.className] || '#be6039';

    // Visibility radius
    const visR = (15 / VIEW_RANGE_X) * S.canvasW;
    ctx.beginPath(); ctx.arc(sx, sy, visR, 0, Math.PI * 2);
    ctx.strokeStyle = 'rgba(190,96,57,.1)'; ctx.lineWidth = 1; ctx.stroke();
    ctx.fillStyle = 'rgba(190,96,57,.02)'; ctx.fill();

    // Self dot (red, larger)
    ctx.beginPath(); ctx.arc(sx, sy, 8, 0, Math.PI * 2);
    ctx.fillStyle = '#dc2626'; ctx.fill();
    ctx.beginPath(); ctx.arc(sx, sy, 8, 0, Math.PI * 2);
    ctx.strokeStyle = '#fff'; ctx.lineWidth = 2; ctx.stroke();

    // HP bar
    if (S.myPlayer.maxHp > 0) {
        const hpR = Math.max(0, Math.min(1, Number(S.myPlayer.currentHp) / Number(S.myPlayer.maxHp)));
        const bw = 32, bh = 4, bx = sx - bw / 2, by = sy - 18;
        ctx.fillStyle = 'rgba(220,38,38,.35)'; ctx.fillRect(bx, by, bw, bh);
        ctx.fillStyle = hpR > .6 ? '#2DD4BF' : hpR > .3 ? '#eab308' : '#dc2626';
        ctx.fillRect(bx, by, bw * hpR, bh);
    }

    // Name + level
    ctx.fillStyle = '#fff'; ctx.font = 'bold 11px sans-serif'; ctx.textAlign = 'center';
    ctx.fillText(`${S.myPlayer.displayName} [${S.myPlayer.level}]`, sx, sy - 22);
}

// Smooth position interpolation
function initPlayerInterp(p) {
    S.playerPositions.set(p.playerId, { x: p.x, y: p.y, tx: p.x, ty: p.y });
}
function updateInterp(id, tx, ty) {
    const interp = S.playerPositions.get(id);
    if (interp) { interp.tx = tx; interp.ty = ty; }
    else { S.playerPositions.set(id, { x: tx, y: ty, tx, ty }); }
}
// Call in render loop for smooth movement
function tickInterpolation() {
    for (const [id, interp] of S.playerPositions) {
        interp.x += (interp.tx - interp.x) * 0.3;
        interp.y += (interp.ty - interp.y) * 0.3;
    }
}

// ═══════════════════════════════════════════════════════════════════
// HUD UPDATES
// ═══════════════════════════════════════════════════════════════════
function updateHud() {
    if (!S.myPlayer) return;
    const p = S.myPlayer;
    els.hudName.textContent = p.displayName;
    els.hudClassLevel.textContent = `${p.className} · Lv.${p.level}`;

    const hpPct = p.maxHp > 0 ? Number(p.currentHp) / Number(p.maxHp) * 100 : 0;
    const mpPct = p.maxMana > 0 ? Number(p.currentMana) / Number(p.maxMana) * 100 : 0;
    els.hudHpFill.style.width = `${Math.min(100, hpPct)}%`;
    els.hudHpFill.style.background = hpPct > 60 ? 'var(--hp-high)' : hpPct > 30 ? 'var(--hp-mid)' : 'var(--hp-low)';
    els.hudHpText.textContent = `${fmt(p.currentHp)} / ${fmt(p.maxHp)}`;
    els.hudMpFill.style.width = `${Math.min(100, mpPct)}%`;
    els.hudMpText.textContent = `${fmt(p.currentMana)} / ${fmt(p.maxMana)}`;

    // Conditions
    els.hudConditions.innerHTML = (p.conditions || []).map(c => {
        const cls = c.category === 'CrowdControl' ? 'condition-chip--cc' : 'condition-chip--state';
        return `<span class="condition-chip ${cls}">${esc(c.conditionType)} ${c.remainingSeconds.toFixed(0)}s</span>`;
    }).join('');
}

function updateOnlineCount() { els.onlineCount.textContent = `${S.allPlayers.size} online`; }
function updateCoords() {
    if (!S.myPlayer) return;
    els.coordsDisplay.textContent = `${Math.round(S.myPlayer.x)}, ${Math.round(S.myPlayer.y)}`;
}

// ═══════════════════════════════════════════════════════════════════
// TARGET SYSTEM
// ═══════════════════════════════════════════════════════════════════
function selectTarget(playerId) {
    if (playerId === S.myPlayer?.playerId) return;
    S.targetPlayerId = playerId;
    updateTargetBar();
}

function deselectTarget() {
    S.targetPlayerId = null;
    els.targetBar.hidden = true;
}

async function autoTarget() {
    if (!S.connection) return;
    try {
        const nearest = await S.connection.invoke('FindNearestTarget');
        if (nearest) {
            selectTarget(nearest.playerId);
            addCombatLogEntry('system', `Auto-target: ${nearest.displayName}`);
        } else {
            addCombatLogEntry('system', 'No hay jugadores cerca.');
        }
    } catch(e) { console.error('Auto-target failed:', e); }
}

function updateTargetBar() {
    if (!S.targetPlayerId) { els.targetBar.hidden = true; return; }
    const t = S.allPlayers.get(S.targetPlayerId);
    if (!t) { els.targetBar.hidden = true; return; } // Keep target selected, just hide bar until visible again
    els.targetBar.hidden = false;
    els.targetName.textContent = t.displayName;
    els.targetClass.textContent = `${t.className} Lv.${t.level}`;
    const hpPct = t.maxHp > 0 ? Number(t.currentHp) / Number(t.maxHp) * 100 : 0;
    els.targetHpFill.style.width = `${Math.min(100, hpPct)}%`;
    els.targetHpFill.style.background = hpPct > 60 ? 'var(--hp-high)' : hpPct > 30 ? 'var(--hp-mid)' : 'var(--hp-low)';
    els.targetHpText.textContent = `${fmt(t.currentHp)} / ${fmt(t.maxHp)}`;
}

function findPlayerAtScreen(sx, sy) {
    for (const [id, p] of S.allPlayers) {
        if (S.myPlayer && id === S.myPlayer.playerId) continue;
        const interp = S.playerPositions.get(id);
        const px = interp ? interp.x : p.x;
        const py = interp ? interp.y : p.y;
        const [psx, psy] = worldToScreen(px, py);
        const dist = Math.sqrt((sx - psx) ** 2 + (sy - psy) ** 2);
        if (dist < 15) return p; // Click tolerance 15px
    }
    return null;
}

// ═══════════════════════════════════════════════════════════════════
// SKILL BAR
// ═══════════════════════════════════════════════════════════════════
function renderSkills() {
    if (!S.myPlayer || !S.myPlayer.skills) return;
    const skills = S.myPlayer.skills;
    const regularSkills = skills.slice(0, -1); // All except ultimate
    const ultimate = skills.length > 0 ? skills[skills.length - 1] : null;
    const totalPages = Math.ceil(regularSkills.length / SKILLS_PER_PAGE);
    S.skillPage = Math.min(S.skillPage, Math.max(0, totalPages - 1));

    const start = S.skillPage * SKILLS_PER_PAGE;
    const pageSkills = regularSkills.slice(start, start + SKILLS_PER_PAGE);

    els.skillSlots.innerHTML = pageSkills.map((s, i) => {
        const globalIdx = start + i;
        const isOnCd = s.isOnCooldown || s.remainingCooldown > 0;
        const noMana = S.myPlayer.currentMana < s.manaCost;
        const disabled = isOnCd || noMana || Number(S.myPlayer.currentHp) <= 0;
        const cdClass = isOnCd ? ' skill-btn--on-cd' : '';

        return `<button class="skill-btn${cdClass}" type="button" data-skill-index="${globalIdx}" ${disabled ? 'disabled' : ''}>
            <span class="skill-btn__key">${i + 1}</span>
            <span class="skill-btn__name">${esc(s.name)}</span>
            <span class="skill-btn__cost">${fmt(s.manaCost)} MP</span>
            ${isOnCd ? `<span class="skill-btn__cd">${Math.ceil(s.remainingCooldown)}s</span>` : ''}
            <button class="skill-btn__upgrade" type="button" data-upgrade-index="${globalIdx}" title="Subir ascensión">↑</button>
            <button class="skill-btn__downgrade" type="button" data-downgrade-index="${globalIdx}" title="Bajar ascensión">↓</button>
        </button>`;
    }).join('');

    // Ultimate
    if (ultimate) {
        const uIdx = skills.length - 1;
        const isOnCd = ultimate.isOnCooldown || ultimate.remainingCooldown > 0;
        const noMana = S.myPlayer.currentMana < ultimate.manaCost;
        const disabled = isOnCd || noMana || Number(S.myPlayer.currentHp) <= 0;
        els.btnUltimate.disabled = disabled;
        els.btnUltimate.className = `skill-btn skill-btn--ultimate${isOnCd ? ' skill-btn--on-cd' : ''}`;
        els.btnUltimate.innerHTML = `<span class="skill-btn__key">R</span>
            <span class="skill-btn__name">${esc(ultimate.name)}</span>
            <span class="skill-btn__cost">${fmt(ultimate.manaCost)} MP</span>
            ${isOnCd ? `<span class="skill-btn__cd">${Math.ceil(ultimate.remainingCooldown)}s</span>` : ''}`;
        els.btnUltimate.dataset.skillIndex = uIdx;
    }

    // Nav
    els.btnSkillPrev.disabled = S.skillPage === 0;
    els.btnSkillNext.disabled = S.skillPage >= totalPages - 1;
    els.skillPageLabel.textContent = `${start + 1}-${Math.min(start + SKILLS_PER_PAGE, regularSkills.length)}`;
}

function useSkill(skillIndex) {
    if (!S.connection || !S.myPlayer) return;
    if (isOnGcd()) return; // Client-side GCD check
    if (!S.targetPlayerId) {
        // Check if it's a healing skill — target self
        const skill = S.myPlayer.skills?.[skillIndex];
        if (skill && skill.damageType === 'None') {
            triggerGcd();
            S.connection.invoke('UseSkill', skillIndex, S.myPlayer.playerId).catch(() => {});
            return;
        }
        // Auto-target nearest
        S.connection.invoke('FindNearestTarget').then(t => {
            if (t) { S.targetPlayerId = t.playerId; updateTargetBar(t); useSkill(skillIndex); }
            else { addCombatLogEntry('system', 'Selecciona un objetivo primero.'); }
        }).catch(() => {});
        return;
    }
    triggerGcd();
    S.connection.invoke('UseSkill', skillIndex, S.targetPlayerId).catch(() => {});
}

// ── GCD (Global Cooldown) visual tracking ──
let gcdUntil = 0;
const GCD_MS = 800;

function isOnGcd() { return Date.now() < gcdUntil; }

function triggerGcd() {
    gcdUntil = Date.now() + GCD_MS;
    // Visually disable all skill buttons and basic attack during GCD
    document.querySelectorAll('.skill-btn, #btn-basic-attack').forEach(btn => {
        btn.classList.add('gcd-active');
    });
    setTimeout(() => {
        document.querySelectorAll('.skill-btn, #btn-basic-attack').forEach(btn => {
            btn.classList.remove('gcd-active');
        });
    }, GCD_MS);
}

function basicAttack() {
    if (!S.connection || !S.myPlayer) return;
    if (!S.targetPlayerId) {
        // Auto-target nearest
        S.connection.invoke('FindNearestTarget').then(t => {
            if (t) { S.targetPlayerId = t.playerId; updateTargetBar(t); basicAttack(); }
            else { addCombatLogEntry('system', 'Selecciona un objetivo primero.'); }
        }).catch(() => {});
        return;
    }
    if (isOnGcd()) return; // Client-side UX feedback only; server validates too
    triggerGcd();
    S.connection.invoke('BasicAttack', S.targetPlayerId).catch(() => {});
}

// ═══════════════════════════════════════════════════════════════════
// COMBAT LOG
// ═══════════════════════════════════════════════════════════════════
function addCombatLog(r) {
    let cssClass = 'log-msg--normal';
    let text = '';

    if (r.wasMiss) {
        cssClass = 'log-msg--miss';
        text = `MISS — ${r.actionName}`;
    } else if (r.outcome === 'Blocked') {
        cssClass = 'log-msg--blocked';
        text = r.notes?.join(' ') || r.actionName;
    } else if (r.outcome === 'Heal') {
        cssClass = 'log-msg--heal';
        text = `${r.actionName}: +${fmt(r.healing)} HP`;
    } else if (r.wasCritical) {
        cssClass = 'log-msg--critical';
        text = `${r.actionName}: ${fmt(r.damage)} daño <span class="critical-badge">CRÍTICO</span>`;
    } else if (r.targetDefeated) {
        cssClass = 'log-msg--defeat';
        text = `${r.actionName}: ${fmt(r.damage)} daño — ¡DERROTA!`;
    } else {
        text = `${r.actionName}: ${fmt(r.damage)} daño`;
    }

    if (r.appliedEffects?.length > 0) {
        text += ` [${r.appliedEffects.join(', ')}]`;
    }

    addCombatLogEntry(cssClass, text);
}

function addCombatLogEntry(cssClass, html) {
    S.combatLog.push({ cssClass, html });
    if (S.combatLog.length > MAX_LOG_ENTRIES) S.combatLog.shift();
    els.combatLogList.innerHTML = S.combatLog.map(e =>
        `<div class="log-msg ${e.cssClass}">${e.html}</div>`
    ).join('');
    els.combatLogList.scrollTop = els.combatLogList.scrollHeight;
}

// ═══════════════════════════════════════════════════════════════════
// NEARBY LIST
// ═══════════════════════════════════════════════════════════════════
function renderNearby() {
    const nearby = S.nearbyPlayers;
    els.nearbyCount.textContent = nearby.length;
    if (nearby.length === 0) { els.nearbyList.innerHTML = '<p class="empty-hint">Nadie cerca.</p>'; return; }
    els.nearbyList.innerHTML = nearby.map(p => {
        const hpPct = p.maxHp > 0 ? Math.round(Number(p.currentHp) / Number(p.maxHp) * 100) : 0;
        return `<div class="nearby-player" data-player-id="${esc(p.playerId)}">
            <div><span class="nearby-player__name">${esc(p.displayName)}</span>
            <span class="nearby-player__class">${esc(p.className)} Lv.${p.level}</span></div>
            <span class="nearby-player__hp">${hpPct}%</span>
        </div>`;
    }).join('');
}

// ═══════════════════════════════════════════════════════════════════
// CHAT
// ═══════════════════════════════════════════════════════════════════
function addChatMsg(name, className, text) {
    const color = CLASS_COLORS[className] || '#be6039';
    S.chatMessages.push({ name, text, color, isSystem: false });
    if (S.chatMessages.length > 80) S.chatMessages.shift();
    renderChat();
}
function addSystemMsg(text) {
    S.chatMessages.push({ name: '', text, color: '', isSystem: true });
    if (S.chatMessages.length > 80) S.chatMessages.shift();
    renderChat();
}
function renderChat() {
    els.chatLog.innerHTML = S.chatMessages.map(m => {
        if (m.isSystem) return `<div class="chat-msg chat-msg--system">${esc(m.text)}</div>`;
        return `<div class="chat-msg"><span class="chat-msg__name" style="color:${m.color}">${esc(m.name)}:</span> ${esc(m.text)}</div>`;
    }).join('');
    els.chatLog.scrollTop = els.chatLog.scrollHeight;
}
function sendChat() {
    const text = els.chatInput.value.trim();
    if (!text || !S.connection) return;
    S.connection.invoke('SendChatMessage', text).catch(() => {});
    els.chatInput.value = '';
}

// ═══════════════════════════════════════════════════════════════════
// CONTEXT MENU
// ═══════════════════════════════════════════════════════════════════
function showContextMenu(x, y, playerId) {
    els.contextMenu.style.left = x + 'px';
    els.contextMenu.style.top = y + 'px';
    els.contextMenu.hidden = false;
    els.contextMenu.dataset.playerId = playerId;
}
function hideContextMenu() { els.contextMenu.hidden = true; }

// ═══════════════════════════════════════════════════════════════════
// SOCIAL FUNCTIONS
// ═══════════════════════════════════════════════════════════════════
async function viewPlayerProfile(playerId) {
    if (!S.connection) return;
    try {
        const profile = await S.connection.invoke('GetPlayerProfile', playerId);
        if (!profile) { addSystemMsg('Perfil no disponible.'); return; }
        addSystemMsg(`═══ PERFIL ═══`);
        addSystemMsg(`${profile.displayName} · ${profile.className} Lv.${profile.level} · Ascensión: ${profile.ascensionLevel}`);
        addSystemMsg(`HP: ${fmt(profile.currentHp)}/${fmt(profile.maxHp)} · Habilidades: ${profile.skillCount}`);
        if (profile.skills?.length > 0) {
            const skillList = profile.skills.map(s => s.name).join(', ');
            addSystemMsg(`Skills: ${skillList}`);
        }
        if (profile.conditions?.length > 0) {
            const condList = profile.conditions.map(c => `${c.conditionType} (${Math.ceil(c.remainingSeconds)}s)`).join(', ');
            addSystemMsg(`Condiciones: ${condList}`);
        }
        addSystemMsg(`═══════════════`);
    } catch (e) {
        addSystemMsg('Error al cargar perfil.');
    }
}

function showFriendRequestDialog(notification) {
    // Show inline in chat with accept/reject buttons
    const div = document.createElement('div');
    div.className = 'chat-msg chat-msg--system';
    div.innerHTML = `
        <span>📨 ${esc(notification.senderName)} quiere ser tu amigo.</span>
        <button class="btn-accept-friend" data-request-sender="${esc(notification.senderPlayerId)}" style="margin-left:.5rem;padding:.15rem .4rem;font-size:.75rem;background:var(--accent);color:#fff;border:none;border-radius:4px;cursor:pointer">Aceptar</button>
        <button class="btn-reject-friend" data-request-sender="${esc(notification.senderPlayerId)}" style="margin-left:.3rem;padding:.15rem .4rem;font-size:.75rem;background:#444;color:#fff;border:none;border-radius:4px;cursor:pointer">Rechazar</button>`;
    els.chatLog.appendChild(div);
    els.chatLog.scrollTop = els.chatLog.scrollHeight;
}

// ═══════════════════════════════════════════════════════════════════
// SKILLS REVIEW PANEL
// ═══════════════════════════════════════════════════════════════════
async function openSkillsReview() {
    if (!S.connection || !S.myPlayer) return;
    S.gamePaused = true;
    try {
        S.skillCatalog = await S.connection.invoke('GetSkillCatalog');
        if (!S.skillCatalog) { addCombatLogEntry('system', 'No se pudo cargar el catálogo.'); S.gamePaused = false; return; }

        $('sr-class-name').textContent = S.skillCatalog.className;
        renderSkillReviewList();
        $('sr-skill-detail').innerHTML = '<p class="empty-hint">Selecciona una habilidad para ver sus detalles.</p>';
        $('skills-review-overlay').hidden = false;
    } catch(e) {
        console.error('Skills catalog failed:', e);
        S.gamePaused = false;
    }
}

function closeSkillsReview() {
    $('skills-review-overlay').hidden = true;
    S.gamePaused = false;
}

function renderSkillReviewList() {
    if (!S.skillCatalog) return;
    const list = $('sr-skill-list');
    list.innerHTML = S.skillCatalog.skills.map((s, i) => {
        const ultClass = s.isUltimate ? ' sr-skill-item--ult' : '';
        return `<div class="sr-skill-item${ultClass}" data-sr-index="${i}">
            <span class="sr-skill-item__slot">${s.isUltimate ? '★' : (i + 1)}</span>
            <div>
                <div class="sr-skill-item__name">${esc(s.name)}</div>
                <div class="sr-skill-item__type">${esc(s.damageType)} · ${esc(s.targetPattern)}</div>
            </div>
        </div>`;
    }).join('');
}

function showSkillDetail(index) {
    if (!S.skillCatalog) return;
    const s = S.skillCatalog.skills[index];
    if (!s) return;

    // Mark active in list
    document.querySelectorAll('.sr-skill-item').forEach(el => el.classList.remove('active'));
    document.querySelector(`[data-sr-index="${index}"]`)?.classList.add('active');

    // Tags
    let tags = '';
    if (s.isUltimate) tags += '<span class="sr-tag sr-tag--ult">★ ULTIMATE</span>';
    (s.elements || []).forEach(e => { tags += `<span class="sr-tag sr-tag--element">${esc(e)}</span>`; });
    (s.roles || []).forEach(r => { tags += `<span class="sr-tag sr-tag--role">${esc(r)}</span>`; });

    // Stats
    const isHeal = s.actionType === 'Heal';
    const dmgLabel = isHeal ? 'Curación base' : 'Daño base';
    const dmgColor = isHeal ? 'var(--heal)' : (s.damageType === 'Magical' ? 'var(--mana)' : 'var(--accent)');

    let scalingInfo = '';
    if (s.scalingStat !== 'None') {
        scalingInfo = `<div class="sr-stat-card"><div class="sr-stat-card__label">Fórmula</div><div class="sr-stat-card__value" style="font-size:.8rem">${fmt(s.baseMagnitude)} + ${s.scalingCoefficient.toFixed(2)} × ${s.scalingStat}</div></div>`;
    }

    let multiHitInfo = '';
    if (s.multiHit) {
        multiHitInfo = `<div class="sr-stat-card"><div class="sr-stat-card__label">Multi-golpe</div><div class="sr-stat-card__value">${s.multiHit.hitCount}× en ${s.multiHit.duration}s</div></div>`;
    }

    // Effects
    let effectsHtml = '';
    if (s.effects && s.effects.length > 0) {
        effectsHtml = `<div class="sr-effects"><h4>Efectos de condición</h4>${s.effects.map(e =>
            `<div class="sr-effect-item"><strong>${esc(e.condition)}</strong> — Duración: ${e.duration ?? '?'}s · Probabilidad ×${e.chance}</div>`
        ).join('')}</div>`;
    }

    // Ascension timeline
    let ascensionHtml = '';
    if (s.ascensions && s.ascensions.length > 0) {
        const playerAsc = S.skillCatalog.playerAscension;
        const nodes = s.ascensions.map(a => {
            const status = a.level <= playerAsc ? 'unlocked' : (a.level === playerAsc + 1 ? 'current' : '');
            const changesList = a.changes.map(c => `<li>${esc(c)}</li>`).join('');
            let upgradeBtn = '';
            if (a.level === playerAsc + 1) {
                upgradeBtn = `<button class="sr-upgrade-btn" data-sr-upgrade="${index}">↑ Subir a Ascensión ${a.level}</button>`;
            }
            let downgradeBtn = '';
            if (a.level === playerAsc && playerAsc > 0) {
                downgradeBtn = `<button class="sr-downgrade-btn" data-sr-downgrade="${index}">↓ Bajar de Ascensión ${a.level}</button>`;
            }
            return `<div class="sr-ascension-node ${status}">
                <div class="sr-ascension-node__level">Ascensión ${a.level}</div>
                <div class="sr-ascension-node__changes"><ul>${changesList}</ul></div>
                ${upgradeBtn}${downgradeBtn}
            </div>`;
        }).join('');

        ascensionHtml = `<div style="margin-top:1rem"><div class="sr-ascension-title">⟡ Línea de Ascensiones <span style="font-size:.75rem;color:var(--ink-muted)">(Actual: ${playerAsc})</span></div><div class="sr-ascension-timeline">${nodes}</div></div>`;
    }

    $('sr-skill-detail').innerHTML = `
        <h2 class="sr-detail-title">${s.isUltimate ? '★ ' : ''}${esc(s.name)}</h2>
        <p class="sr-detail-desc">${esc(s.description)}</p>
        <div class="sr-detail-tags">${tags}</div>
        <div class="sr-stats-grid">
            <div class="sr-stat-card"><div class="sr-stat-card__label">${dmgLabel}</div><div class="sr-stat-card__value" style="color:${dmgColor}">${fmt(s.computedDamage)}</div></div>
            <div class="sr-stat-card"><div class="sr-stat-card__label">Tipo de daño</div><div class="sr-stat-card__value">${esc(s.damageType)}</div></div>
            <div class="sr-stat-card"><div class="sr-stat-card__label">Rango</div><div class="sr-stat-card__value">${s.range}m</div></div>
            <div class="sr-stat-card"><div class="sr-stat-card__label">Enfriamiento</div><div class="sr-stat-card__value">${s.cooldown}s</div></div>
            <div class="sr-stat-card"><div class="sr-stat-card__label">Costo de maná</div><div class="sr-stat-card__value" style="color:var(--mana)">${fmt(s.manaCost)}</div></div>
            <div class="sr-stat-card"><div class="sr-stat-card__label">Patrón</div><div class="sr-stat-card__value">${esc(s.targetPattern)}</div></div>
            ${s.areaRadius ? `<div class="sr-stat-card"><div class="sr-stat-card__label">Radio de área</div><div class="sr-stat-card__value">${s.areaRadius}m</div></div>` : ''}
            ${s.maxTargets > 1 ? `<div class="sr-stat-card"><div class="sr-stat-card__label">Máx. objetivos</div><div class="sr-stat-card__value">${s.maxTargets}</div></div>` : ''}
            ${scalingInfo}
            ${multiHitInfo}
            <div class="sr-stat-card"><div class="sr-stat-card__label">Puede Criticar</div><div class="sr-stat-card__value">${s.canCrit ? 'Sí' : 'No'}</div></div>
        </div>
        ${effectsHtml}
        ${ascensionHtml}
    `;
}

// ═══════════════════════════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════════════════════════
function esc(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;'); }
function fmt(v) { return new Intl.NumberFormat('es-ES', { maximumFractionDigits: 0 }).format(Number(v || 0)); }
function hexAlpha(hex, a) {
    hex = hex.replace('#', '');
    return `rgba(${parseInt(hex.substring(0,2),16)},${parseInt(hex.substring(2,4),16)},${parseInt(hex.substring(4,6),16)},${a})`;
}
function showLoginError(msg) { els.loginError.textContent = msg; els.loginError.hidden = false; }
function hideLoginError() { els.loginError.hidden = true; }

// ═══════════════════════════════════════════════════════════════════
// EVENT BINDINGS
// ═══════════════════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', () => {
    // ── Class selection ──
    function selectClassCard(card) {
        document.querySelectorAll('.class-card').forEach(c => c.classList.remove('selected'));
        card.classList.add('selected');
        S.selectedClass = card.dataset.class;
        els.btnJoin.disabled = false;
        els.btnJoin.textContent = 'ENTRAR AL MUNDO';
        loadSkillPreview(S.selectedClass);
    }
    document.querySelectorAll('.class-card').forEach(card => {
        card.addEventListener('click', () => selectClassCard(card));
        // Touch fallback for mobile browsers where click doesn't fire on complex buttons
        card.addEventListener('touchend', e => {
            e.preventDefault();
            selectClassCard(card);
        }, { passive: false });
    });

    // ── Join ──
    els.btnJoin.addEventListener('click', joinWorld);
    els.btnJoin.addEventListener('touchend', e => { e.preventDefault(); joinWorld(); }, { passive: false });
    els.inputName.addEventListener('keydown', e => { if (e.key === 'Enter' && S.selectedClass) joinWorld(); });

    // ── Keyboard ──
    document.addEventListener('keydown', e => {
        // Don't capture keys when typing in any input or textarea
        const tag = document.activeElement?.tagName;
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
        const key = e.key.toLowerCase();

        // Movement
        if (['w','a','s','d','arrowup','arrowdown','arrowleft','arrowright'].includes(key)) {
            e.preventDefault(); S.keysDown.add(key);
        }

        // F = basic attack
        if (key === 'f') { e.preventDefault(); basicAttack(); }

        // Skills 1-6
        if (key >= '1' && key <= '6') {
            e.preventDefault();
            const idx = S.skillPage * SKILLS_PER_PAGE + (parseInt(key) - 1);
            useSkill(idx);
        }

        // Ultimate
        if (key === 'r') {
            e.preventDefault();
            if (S.myPlayer?.skills?.length > 0) useSkill(S.myPlayer.skills.length - 1);
        }

        // Tab = auto-target
        if (key === 'tab') {
            e.preventDefault();
            autoTarget();
        }

        // Escape
        if (key === 'escape') {
            if (!$('skills-review-overlay').hidden) { closeSkillsReview(); return; }
            deselectTarget();
            hideContextMenu();
            els.controlsOverlay.hidden = true;
        }
    });

    document.addEventListener('keyup', e => { S.keysDown.delete(e.key.toLowerCase()); });

    // ── Canvas click (select target) — works on desktop ──
    els.canvas.addEventListener('click', e => {
        const rect = els.canvas.getBoundingClientRect();
        const sx = (e.clientX - rect.left) * (S.canvasW / rect.width);
        const sy = (e.clientY - rect.top) * (S.canvasH / rect.height);
        const player = findPlayerAtScreen(sx, sy);
        if (player) selectTarget(player.playerId);
        else deselectTarget();
        hideContextMenu();
    });

    // ── Canvas touch support (mobile browsers) ──
    let _touchStartTime = 0;
    let _touchStartPos = null;
    els.canvas.addEventListener('touchstart', e => {
        e.preventDefault();
        const t = e.touches[0];
        _touchStartTime = Date.now();
        _touchStartPos = { x: t.clientX, y: t.clientY };
    }, { passive: false });

    els.canvas.addEventListener('touchend', e => {
        e.preventDefault();
        if (!_touchStartPos) return;
        const t = e.changedTouches[0];
        const dt = Date.now() - _touchStartTime;
        const dx = t.clientX - _touchStartPos.x;
        const dy = t.clientY - _touchStartPos.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        const rect = els.canvas.getBoundingClientRect();
        const sx = (t.clientX - rect.left) * (S.canvasW / rect.width);
        const sy = (t.clientY - rect.top) * (S.canvasH / rect.height);
        const player = findPlayerAtScreen(sx, sy);

        if (dt > 500 && dist < 20) {
            // Long press → context menu
            if (player) {
                selectTarget(player.playerId);
                showContextMenu(t.clientX, t.clientY, player.playerId);
            }
        } else if (dist < 20) {
            // Tap → select/deselect target
            if (player) selectTarget(player.playerId);
            else deselectTarget();
            hideContextMenu();
        }
        _touchStartPos = null;
    }, { passive: false });

    // ── Canvas right-click (context menu) — desktop ──
    els.canvas.addEventListener('contextmenu', e => {
        e.preventDefault();
        const rect = els.canvas.getBoundingClientRect();
        const sx = (e.clientX - rect.left) * (S.canvasW / rect.width);
        const sy = (e.clientY - rect.top) * (S.canvasH / rect.height);
        const player = findPlayerAtScreen(sx, sy);
        if (player) {
            selectTarget(player.playerId);
            showContextMenu(e.clientX, e.clientY, player.playerId);
        }
    });

    // ── Web Joystick (touch-capable browsers) ──
    const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
    if (isTouchDevice) {
        const joyEl = $('web-joystick');
        const joyPad = $('web-joystick-pad');
        if (joyEl && joyPad) {
            joyEl.hidden = false;
            let joyInterval = null;
            const DEAD = 15, RADIUS = 40;

            joyEl.addEventListener('touchstart', e => { e.preventDefault(); }, { passive: false });
            joyEl.addEventListener('touchmove', e => {
                e.preventDefault();
                const t = e.touches[0];
                const rect = joyEl.getBoundingClientRect();
                const cx = rect.left + rect.width / 2;
                const cy = rect.top + rect.height / 2;
                let dx = t.clientX - cx, dy = t.clientY - cy;
                const dist = Math.sqrt(dx * dx + dy * dy);
                if (dist > RADIUS) { dx = dx / dist * RADIUS; dy = dy / dist * RADIUS; }
                joyPad.style.transform = `translate(${dx}px, ${dy}px)`;

                S.keysDown.clear();
                if (dist > DEAD) {
                    if (Math.abs(dx) > Math.abs(dy)) {
                        S.keysDown.add(dx > 0 ? 'd' : 'a');
                    } else {
                        S.keysDown.add(dy > 0 ? 's' : 'w');
                    }
                }
            }, { passive: false });

            const endJoy = e => {
                e.preventDefault();
                joyPad.style.transform = '';
                S.keysDown.clear();
            };
            joyEl.addEventListener('touchend', endJoy, { passive: false });
            joyEl.addEventListener('touchcancel', endJoy, { passive: false });
        }
    }

    // ── Context menu actions ──
    els.contextMenu.addEventListener('click', e => {
        const item = e.target.closest('[data-action]');
        if (!item) return;
        const action = item.dataset.action;
        const playerId = els.contextMenu.dataset.playerId;
        hideContextMenu();

        switch (action) {
            case 'chat':
                els.chatInput.focus();
                els.chatInput.placeholder = `Mensaje para ${S.allPlayers.get(playerId)?.displayName || 'jugador'}...`;
                break;
            case 'profile':
                viewPlayerProfile(playerId);
                break;
            case 'friend':
                if (S.connection) S.connection.invoke('SendFriendRequest', playerId).catch(() => {});
                break;
            case 'block':
                if (S.connection) S.connection.invoke('BlockPlayer', playerId).catch(() => {});
                break;
        }
    });

    // ── Deselect target ──
    $('btn-deselect-target')?.addEventListener('click', deselectTarget);
    $('btn-auto-target')?.addEventListener('click', autoTarget);

    // ── Nearby list click = select target ──
    els.nearbyList.addEventListener('click', e => {
        const item = e.target.closest('[data-player-id]');
        if (item) selectTarget(item.dataset.playerId);
    });

    // ── Skill clicks (click + touch delegation) ──
    function handleSkillAction(e) {
        const upgradeBtn = e.target.closest('[data-upgrade-index]');
        if (upgradeBtn) {
            e.stopPropagation();
            const idx = parseInt(upgradeBtn.dataset.upgradeIndex);
            if (S.connection) S.connection.invoke('UpgradeSkill', idx).catch(() => {});
            return;
        }
        const downgradeBtn = e.target.closest('[data-downgrade-index]');
        if (downgradeBtn) {
            e.stopPropagation();
            const idx = parseInt(downgradeBtn.dataset.downgradeIndex);
            if (S.connection) S.connection.invoke('DowngradeSkill', idx).catch(() => {});
            return;
        }
        const btn = e.target.closest('[data-skill-index]');
        if (btn && !btn.disabled) useSkill(parseInt(btn.dataset.skillIndex));
    }
    els.skillSlots.addEventListener('click', handleSkillAction);
    els.skillSlots.addEventListener('touchend', e => { e.preventDefault(); handleSkillAction(e); }, { passive: false });

    function handleUltimate() { if (S.myPlayer?.skills?.length > 0) useSkill(S.myPlayer.skills.length - 1); }
    els.btnUltimate.addEventListener('click', handleUltimate);
    els.btnUltimate.addEventListener('touchend', e => { e.preventDefault(); handleUltimate(); }, { passive: false });

    // Basic attack button
    const basicAtkBtn = $('btn-basic-attack');
    basicAtkBtn?.addEventListener('click', () => basicAttack());
    basicAtkBtn?.addEventListener('touchend', e => { e.preventDefault(); basicAttack(); }, { passive: false });

    // Skill page nav
    els.btnSkillPrev.addEventListener('click', () => { S.skillPage = Math.max(0, S.skillPage - 1); renderSkills(); });
    els.btnSkillNext.addEventListener('click', () => { S.skillPage++; renderSkills(); });

    // ── Reset self ──
    const resetBtn = $('btn-reset-self');
    resetBtn?.addEventListener('click', () => {
        if (S.connection) S.connection.invoke('ResetSelf').catch(() => {});
    });
    resetBtn?.addEventListener('touchend', e => {
        e.preventDefault();
        if (S.connection) S.connection.invoke('ResetSelf').catch(() => {});
    }, { passive: false });

    // ── Chat ──
    $('btn-send-chat')?.addEventListener('click', sendChat);
    els.chatInput.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); sendChat(); } });
    els.chatInput.addEventListener('focus', () => { S.isTypingChat = true; S.keysDown.clear(); });
    els.chatInput.addEventListener('blur', () => { S.isTypingChat = false; });

    // ── Chat toggle ──
    $('btn-toggle-chat')?.addEventListener('click', () => {
        const chatLog = els.chatLog;
        const chatInput = document.querySelector('.chat-input-row');
        const isHidden = chatLog.style.display === 'none';
        chatLog.style.display = isHidden ? '' : 'none';
        chatInput.style.display = isHidden ? '' : 'none';
        $('btn-toggle-chat').textContent = isHidden ? '✕' : '💬';
        $('btn-toggle-chat').title = isHidden ? 'Cerrar chat' : 'Abrir chat';
    });

    // ── Friend request accept/reject (delegated) ──
    els.chatLog.addEventListener('click', async e => {
        const acceptBtn = e.target.closest('.btn-accept-friend');
        const rejectBtn = e.target.closest('.btn-reject-friend');
        if (!S.connection) return;
        if (acceptBtn) {
            // For demo: get pending requests and accept the first from this sender
            try {
                const pending = await S.connection.invoke('GetPendingFriendRequests');
                const senderId = acceptBtn.dataset.requestSender;
                const req = pending.find(r => r.senderCharacterId === senderId);
                if (req) {
                    await S.connection.invoke('RespondFriendRequest', req.requestId, true);
                    addSystemMsg('Solicitud de amistad aceptada.');
                }
            } catch {}
            acceptBtn.parentElement.remove();
        }
        if (rejectBtn) {
            try {
                const pending = await S.connection.invoke('GetPendingFriendRequests');
                const senderId = rejectBtn.dataset.requestSender;
                const req = pending.find(r => r.senderCharacterId === senderId);
                if (req) {
                    await S.connection.invoke('RespondFriendRequest', req.requestId, false);
                    addSystemMsg('Solicitud de amistad rechazada.');
                }
            } catch {}
            rejectBtn.parentElement.remove();
        }
    });

    // ── Skills Review ──
    $('btn-review-skills')?.addEventListener('click', openSkillsReview);
    $('btn-close-skills-review')?.addEventListener('click', closeSkillsReview);
    $('sr-skill-list')?.addEventListener('click', e => {
        const item = e.target.closest('[data-sr-index]');
        if (item) showSkillDetail(parseInt(item.dataset.srIndex));
    });
    $('sr-skill-detail')?.addEventListener('click', e => {
        const upgradeBtn = e.target.closest('[data-sr-upgrade]');
        if (upgradeBtn && S.connection) {
            const idx = parseInt(upgradeBtn.dataset.srUpgrade);
            S.connection.invoke('UpgradeSkill', idx).catch(() => {});
            setTimeout(() => openSkillsReview(), 500);
            return;
        }
        const downgradeBtn = e.target.closest('[data-sr-downgrade]');
        if (downgradeBtn && S.connection) {
            const idx = parseInt(downgradeBtn.dataset.srDowngrade);
            S.connection.invoke('DowngradeSkill', idx).catch(() => {});
            setTimeout(() => openSkillsReview(), 500);
        }
    });

    // ── Controls help ──
    $('btn-controls-help')?.addEventListener('click', () => { els.controlsOverlay.hidden = false; });
    $('btn-close-controls')?.addEventListener('click', () => { els.controlsOverlay.hidden = true; });

    // ── Click outside to close ──
    document.addEventListener('click', e => {
        if (!els.contextMenu.hidden && !els.contextMenu.contains(e.target)) hideContextMenu();
    });

    // ── Resize ──
    window.addEventListener('resize', () => { if (!els.gameScreen.hidden) resizeCanvas(); });
});

// ═══════════════════════════════════════════════════════════════════
// TUTORIAL SYSTEM
// ═══════════════════════════════════════════════════════════════════
const Tutorial = (() => {
    let currentStep = -1;
    let overlay = null;
    let tooltip = null;
    let active = false;
    const isMobile = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    const advanceText = isMobile ? 'Toca para continuar' : 'Clic o presiona cualquier tecla para continuar';

    const steps = [
        {
            target: '#world-canvas',
            text: 'Este es el campo de batalla. Tu personaje es el punto rojo. Los demás jugadores son los puntos amarillos.',
            sub: isMobile ? 'Desliza en el joystick para moverte.' : 'Usa las teclas W A S D o las flechas para moverte.',
        },
        {
            target: isMobile ? null : '#world-canvas',
            text: isMobile ? null : 'W = arriba, S = abajo, A = izquierda, D = derecha.',
            sub: isMobile ? null : 'También puedes usar las flechas del teclado.',
            skip: isMobile,
        },
        {
            target: '#skill-slots',
            text: 'Estas son tus habilidades. Cada una tiene un costo de maná y un tiempo de recarga.',
            sub: isMobile ? 'Toca una habilidad para usarla contra tu objetivo.' : 'Presiona las teclas 1 a 6 para activarlas, o haz clic directamente.',
        },
        {
            target: '.skill-page-nav',
            text: 'Tienes más habilidades. Usa estas flechas para ver las demás.',
            sub: 'Navega entre páginas de 6 habilidades.',
        },
        {
            target: '.skill-ultimate-slot',
            text: 'Esta es tu habilidad definitiva. Es la más poderosa de tu clase.',
            sub: isMobile ? 'Tócala para activarla.' : 'Presiónala con la tecla R.',
        },
        {
            target: '#world-canvas',
            text: 'Haz clic en un jugador amarillo para seleccionarlo como objetivo. Tus ataques se dirigirán a él.',
            sub: isMobile ? 'Toca un punto amarillo en el mapa.' : 'También puedes usar Tab para auto-target al más cercano.',
        },
        {
            target: '.hud-bars',
            text: 'Arriba puedes ver tu vida y tu maná. Las habilidades consumen maná. Si llegas a 0 de vida, pierdes.',
            sub: 'La barra verde es tu HP, la morada es tu Maná.',
        },
        {
            target: '#skill-slots',
            text: 'Puedes subir o bajar el nivel de ascensión de tus habilidades con los botones ↑ y ↓.',
            sub: 'Cada ascensión mejora los valores de la habilidad.',
        },
        {
            target: '#btn-review-skills',
            text: 'Aquí puedes ver la descripción completa de cada habilidad: daño, efectos, costos y más.',
            sub: 'También puedes subir/bajar ascensiones desde el catálogo.',
        },
        {
            target: '#target-bar',
            text: 'Al seleccionar a otro jugador puedes chatear, enviar solicitud de amistad, ver su perfil o bloquearlo.',
            sub: isMobile ? 'Mantén presionado sobre un jugador para ver las opciones.' : 'Haz clic derecho sobre un jugador para ver el menú contextual.',
            forceShow: true,
        },
        {
            target: '.combat-log',
            text: 'Aquí verás el resultado de cada acción: daño infligido, ataques esquivados, golpes críticos y curaciones.',
            sub: 'Los colores indican el tipo: dorado=crítico, verde=curación, gris=normal.',
        },
        {
            target: '#btn-reset-self',
            text: 'Si quieres empezar de nuevo, este botón restaura toda tu vida, maná y habilidades al instante.',
            sub: 'Útil para probar diferentes estrategias.',
        },
    ];

    function createOverlay() {
        overlay = document.createElement('div');
        overlay.className = 'tutorial-overlay';
        overlay.innerHTML = '<svg class="tutorial-mask" width="100%" height="100%" style="pointer-events:none"><defs><mask id="tutorial-hole"><rect width="100%" height="100%" fill="white"/><rect id="tutorial-hole-rect" fill="black" rx="8"/></mask></defs><rect width="100%" height="100%" fill="rgba(0,0,0,0.75)" mask="url(#tutorial-hole)"/></svg>';
        tooltip = document.createElement('div');
        tooltip.className = 'tutorial-tooltip';
        overlay.appendChild(tooltip);
        document.body.appendChild(overlay);
    }

    function removeOverlay() {
        if (overlay) { overlay.remove(); overlay = null; tooltip = null; }
    }

    function showWelcome() {
        createOverlay();
        const holeRect = document.getElementById('tutorial-hole-rect');
        if (holeRect) { holeRect.setAttribute('width', '0'); holeRect.setAttribute('height', '0'); }
        tooltip.innerHTML = `
            <div class="tutorial-welcome">
                <h2>¡Bienvenido a WAR!</h2>
                <p>Este tutorial te guiará por los controles del juego. Puedes omitirlo si ya conoces la demo.</p>
                <div class="tutorial-welcome-btns">
                    <button class="tutorial-btn tutorial-btn--start" id="tutorial-start">Iniciar Tutorial</button>
                    <button class="tutorial-btn tutorial-btn--skip" id="tutorial-skip">Omitir</button>
                </div>
            </div>`;
        tooltip.style.cssText = 'top:50%;left:50%;transform:translate(-50%,-50%)';
        document.getElementById('tutorial-start').onclick = () => { currentStep = -1; nextStep(); };
        document.getElementById('tutorial-skip').onclick = close;
    }

    function nextStep() {
        currentStep++;
        while (currentStep < steps.length && steps[currentStep].skip) { currentStep++; }
        if (currentStep >= steps.length) { showFinal(); return; }
        renderStep(steps[currentStep]);
    }

    function renderStep(step) {
        const el = step.target ? document.querySelector(step.target) : null;
        const holeRect = document.getElementById('tutorial-hole-rect');

        if (el && el.offsetParent !== null) {
            const rect = el.getBoundingClientRect();
            const pad = 6;
            if (holeRect) {
                holeRect.setAttribute('x', rect.left - pad);
                holeRect.setAttribute('y', rect.top - pad);
                holeRect.setAttribute('width', rect.width + pad * 2);
                holeRect.setAttribute('height', rect.height + pad * 2);
            }
            // Position tooltip — clamp within viewport
            const below = rect.bottom + 10;
            const above = rect.top - 10;
            const leftPos = Math.min(Math.max(10, rect.left), window.innerWidth - 330);
            if (below + 100 < window.innerHeight) {
                tooltip.style.cssText = `top:${Math.min(below, window.innerHeight - 140)}px;left:${leftPos}px;transform:none`;
            } else {
                tooltip.style.cssText = `bottom:${Math.max(10, window.innerHeight - above)}px;left:${leftPos}px;transform:none`;
            }
        } else {
            if (holeRect) { holeRect.setAttribute('width', '0'); holeRect.setAttribute('height', '0'); }
            tooltip.style.cssText = 'top:50%;left:50%;transform:translate(-50%,-50%);max-width:360px';
        }

        tooltip.innerHTML = `
            <div class="tutorial-step">
                <div class="tutorial-step__count">Paso ${currentStep + 1} / ${steps.filter(s => !s.skip).length}</div>
                <p class="tutorial-step__text">${step.text}</p>
                ${step.sub ? `<p class="tutorial-step__sub">${step.sub}</p>` : ''}
                <p class="tutorial-step__advance">${advanceText}</p>
            </div>`;
    }

    function showFinal() {
        const holeRect = document.getElementById('tutorial-hole-rect');
        if (holeRect) { holeRect.setAttribute('width', '0'); holeRect.setAttribute('height', '0'); }
        tooltip.style.cssText = 'top:50%;left:50%;transform:translate(-50%,-50%)';
        tooltip.innerHTML = `
            <div class="tutorial-welcome">
                <h2>¡Listo!</h2>
                <p>Ya conoces todo lo que necesitas. ¡Buena suerte en el campo de batalla!</p>
                <button class="tutorial-btn tutorial-btn--start" id="tutorial-finish">Comenzar a jugar</button>
            </div>`;
        // Use setTimeout to avoid the current click propagation
        setTimeout(() => {
            const finishBtn = document.getElementById('tutorial-finish');
            if (finishBtn) finishBtn.onclick = close;
            // Also close on any overlay click/touch (backup for mobile)
            if (overlay) {
                overlay.onclick = close;
                overlay.ontouchend = e => { e.preventDefault(); close(); };
            }
        }, 100);
    }

    function close() {
        active = false;
        removeOverlay();
        sessionStorage.setItem('war-tutorial-done', '1');
    }

    function handleAdvance(e) {
        if (!active) return;
        // Don't advance on welcome/final screen buttons — let their onclick handle it
        if (e.target.closest('.tutorial-btn')) return;
        // Don't advance if on welcome screen (step -1) or final screen
        if (currentStep < 0 || currentStep >= steps.length) return;
        e.preventDefault();
        e.stopPropagation();
        nextStep();
    }

    function start() {
        if (sessionStorage.getItem('war-tutorial-done')) return;
        active = true;
        createOverlay();
        overlay.addEventListener('click', handleAdvance);
        overlay.addEventListener('touchend', e => {
            // Touch support for mobile browsers
            handleAdvance(e);
        }, { passive: false });
        document.addEventListener('keydown', function tutorialKey(e) {
            if (!active) { document.removeEventListener('keydown', tutorialKey); return; }
            e.preventDefault();
            e.stopPropagation();
            // If on welcome screen, don't advance on key
            if (currentStep < 0) return;
            nextStep();
        }, true);
        showWelcome();
    }

    return { start, isActive: () => active };
})();

