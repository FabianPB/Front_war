using War.Core.Combat;
using War.Core.Resources;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Api.Application.GameWorld;

/// <summary>
/// Motor de resolución de combate del mundo online.
///
/// ═══════════════════════════════════════════════════════════════
/// CONTRATO
/// ═══════════════════════════════════════════════════════════════
/// Esta clase asume que el caller (GameHub) YA validó la acción
/// mediante CombatValidationPipeline.Validate() y que el target
/// está dentro de rango. Por lo tanto NO re-valida lockout, rate
/// limit, hard CC, silence, GCD, cooldown ni mana.
///
/// El único check defensivo que mantiene es "target vivo" porque
/// el target puede haber muerto entre la validación y la ejecución.
///
/// ═══════════════════════════════════════════════════════════════
/// MODELO DE DAÑO
/// ═══════════════════════════════════════════════════════════════
/// El daño se calcula por fases numeradas y explícitas. Cada
/// modificador (crit, skill, basic, pvp, elemental) es ADITIVO y
/// se aplica como un bono sobre el daño base. Las reducciones del
/// objetivo solo afectan al bono correspondiente a su fuente
/// (p.ej. CritDamageTakenReduction solo reduce el bono de crit),
/// y el daño final nunca puede caer por debajo del daño base antes
/// de la mitigación de defensa.
///
/// Fórmula consolidada:
///   baseDamage = SkillBase + ScalingCoef × AttackerStat
///                (para basics: AttackStat × 0.80 × ComboMultiplier)
///
///   increasePct = 0
///     += SkillDamageIncrease        (si skill)
///     += BasicAttackDamageIncrease  (si basic)
///     += PvPDamageIncrease          (siempre — es PvP)
///     += CritDamage + CritDamageIncrease (si wasCritical)
///     += ElementDamageIncrease      (por cada elemento del skill)
///
///   reductionPct = 0
///     += SkillDamageIncrease × SkillDamageReduction           (si skill)
///     += BasicAttackDamageIncrease × BasicAttackDamageReduction (si basic)
///     += PvPDamageIncrease × PvPDamageReduction
///     += (CritDamage + CritDamageIncrease) × CritDamageTakenReduction (si crit)
///     += ElementDamageIncrease × ElementDamageReduction (por elemento)
///
///   netBonusPct = max(0, increasePct - reductionPct)
///   inflatedDamage = baseDamage × (1 + netBonusPct/100)
///
///   Defensa (diminishing returns):
///     effectiveDef = max(0, defense - penetration)
///     mitFactor = 1 - effectiveDef / (effectiveDef + 300)
///     finalDamage = max(1, round(inflatedDamage × mitFactor))
///
/// Este modelo garantiza que:
///   1. El daño nunca baja del baseDamage * mitigación.
///   2. Cada reducción solo neutraliza su propio incremento.
///   3. CritDamageTakenReduction no tiene efecto si no hubo crit.
///   4. SkillDamageReduction no tiene efecto en básicos.
///   5. BasicAttackDamageReduction no tiene efecto en skills.
/// </summary>
public sealed class OnlineCombatService
{
    private static readonly Random Rng = new();

    // ──────────────────────────────────────────────────────────────
    // RESULT DTO
    // ──────────────────────────────────────────────────────────────

    public sealed record OnlineCombatResult(
        string AttackerPlayerId,
        string TargetPlayerId,
        string ActionName,
        string ActionType,          // "BasicAttack" | "Skill"
        string Outcome,             // "Hit" | "Miss" | "Blocked" | "CriticalHit" | "Heal"
        decimal Damage,
        decimal Healing,
        bool WasCritical,
        bool WasMiss,
        bool TargetDefeated,
        decimal TargetRemainingHp,
        decimal TargetMaxHp,
        decimal AttackerRemainingMana,
        decimal AttackerRemainingHp,        // NEW: para futuro lifesteal/reflect
        decimal AttackerMaxHp,              // NEW
        IReadOnlyList<string> AppliedEffects,
        IReadOnlyList<string> Notes);

    // ──────────────────────────────────────────────────────────────
    // BASIC ATTACK
    // ──────────────────────────────────────────────────────────────

    public OnlineCombatResult ExecuteBasicAttack(OnlinePlayer attacker, OnlinePlayer target)
    {
        // Guard defensivo: target ya muerto. El pipeline no chequea el HP del
        // target (no es una propiedad del atacante), así que aquí es la primera
        // oportunidad de detectar que alguien atacó a un cadáver.
        if (target.CurrentHp <= 0)
            return CreateFailResult(attacker, target, "Ataque Básico", "BasicAttack",
                "Blocked", "El objetivo ya está derrotado.");

        // ── Phase 1 — Combo bookkeeping (pre-hit, solo lee y calcula) ──
        var now = DateTime.UtcNow;
        var timeSinceLastAttack = (now - attacker.LastBasicAttackTime).TotalSeconds;
        if (timeSinceLastAttack > CombatTimingConstants.ComboWindowSeconds)
        {
            attacker.ComboStep = 0;
        }

        // Multiplicador del combo: 1.015^step (step 0 = 1.000, step 5 = 1.077)
        var comboMultiplier = 1.0m;
        for (var i = 0; i < attacker.ComboStep; i++)
        {
            comboMultiplier *= CombatTimingConstants.ComboStageMultiplier;
        }
        var comboStageDisplay = attacker.ComboStep + 1; // 1-based para el label

        // ── Phase 2 — Clasificación ──
        var isPhysical = attacker.ClassType is ClassType.Lancero or ClassType.Bruiser;
        var attackStat = isPhysical
            ? GetStat(attacker, StatType.PhysicalAttack)
            : GetStat(attacker, StatType.MagicAttack);

        // ── Phase 3 — Daño base ──
        // baseDamage = AttackStat × coef × comboMultiplier
        var baseDamage = attackStat * CombatFormulaConstants.BasicAttackBaseCoef * comboMultiplier;

        var actionName = $"Ataque Básico (Combo {comboStageDisplay}/{CombatTimingConstants.ComboStageCount})";

        // Resolver contra el objetivo. El combo avanza independientemente del
        // resultado (hit/miss/blocked) porque con los cast times y la ventana
        // de continuación, exigir hit para avanzar haría virtualmente imposible
        // completar el combo completo. Un miss es parte del ritmo, no reinicia.
        var result = ResolveDamageAction(
            attacker, target,
            actionName, "BasicAttack",
            baseDamage, isPhysical,
            skillDef: null,
            isBasicAttack: true,
            elements: Array.Empty<CombatConditionType>(),
            conditionApps: new List<ConditionApplication>());

        // ── Phase 4 — Combo advance (siempre que se haya intentado el ataque) ──
        attacker.ComboStep = (attacker.ComboStep + 1) % CombatTimingConstants.ComboStageCount;

        return result;
    }

    // ──────────────────────────────────────────────────────────────
    // SKILL ATTACK (damage path)
    // ──────────────────────────────────────────────────────────────

    public OnlineCombatResult ExecuteSkill(OnlinePlayer attacker, OnlinePlayer target, OnlinePlayerSkill skill)
    {
        // Target vivo
        if (target.CurrentHp <= 0 && !IsHealingSkill(skill))
            return CreateFailResult(attacker, target, skill.Name, "Skill",
                "Blocked", "El objetivo ya está derrotado.");

        // ── Phase 1 — Resource deduction + cooldown con CDR ──
        DeductManaAndSetCooldown(attacker, skill);

        // ── Phase 2 — Clasificación ──
        var skillDef = GetSkillDefinition(skill.SkillId);
        var isPhysical = skill.DamageType == "Physical";
        var isHealing = skillDef?.BaseTuning.Action.ActionType == SkillActionType.Heal;
        var elements = ExtractElements(skillDef);

        // ── Phase 3 — Base magnitude ──
        var baseMagnitude = CalculateBaseMagnitude(attacker, skill, skillDef, isPhysical);

        // ── Phase 4 — Build condition applications (desde el catálogo) ──
        var conditionApps = BuildConditionApplications(skillDef, attacker.AscensionLevel);

        // ── Phase 5 — Dispatch ──
        if (isHealing)
        {
            // La curación es manejada por el caller vía ExecuteSkillHealing(attacker, targets, skill)
            // cuando resuelve múltiples targets de grupo. Este camino es la llamada a un
            // único target (legacy). Se redirige a ResolveHealingAction con ese único target.
            return ResolveHealingAction(attacker, target, skill.Name, baseMagnitude);
        }

        return ResolveDamageAction(
            attacker, target,
            skill.Name, "Skill",
            baseMagnitude, isPhysical,
            skillDef: skillDef,
            isBasicAttack: false,
            elements: elements,
            conditionApps: conditionApps);
    }

    /// <summary>
    /// Variante de ExecuteSkill para skills con afinidad Ally: resuelve el
    /// efecto sobre CADA miembro del grupo (o el caster solo si no hay grupo).
    /// Usado por el hub cuando detecta skill heal/buff con pattern Area o Self.
    /// </summary>
    public IReadOnlyList<OnlineCombatResult> ExecuteSkillOnMultipleTargets(
        OnlinePlayer attacker,
        IReadOnlyList<OnlinePlayer> targets,
        OnlinePlayerSkill skill)
    {
        // Deducción única de mana/cooldown (no por cada target)
        DeductManaAndSetCooldown(attacker, skill);

        var skillDef = GetSkillDefinition(skill.SkillId);
        var isPhysical = skill.DamageType == "Physical";
        var isHealing = skillDef?.BaseTuning.Action.ActionType == SkillActionType.Heal;
        var elements = ExtractElements(skillDef);
        var baseMagnitude = CalculateBaseMagnitude(attacker, skill, skillDef, isPhysical);
        var conditionApps = BuildConditionApplications(skillDef, attacker.AscensionLevel);

        var results = new List<OnlineCombatResult>(targets.Count);
        foreach (var t in targets)
        {
            if (isHealing)
            {
                if (t.CurrentHp <= 0) continue; // no resucitar con heal directo
                results.Add(ResolveHealingActionInternal(attacker, t, skill.Name, baseMagnitude,
                    deductedAlready: true));
            }
            else
            {
                if (t.CurrentHp <= 0) continue;
                results.Add(ResolveDamageAction(attacker, t, skill.Name, "Skill",
                    baseMagnitude, isPhysical,
                    skillDef: skillDef,
                    isBasicAttack: false,
                    elements: elements,
                    conditionApps: conditionApps));
            }
        }

        // Si no hubo target válido, al menos devolver un resultado "sin efecto" sobre el caster
        if (results.Count == 0)
        {
            results.Add(CreateFailResult(attacker, attacker, skill.Name, "Skill",
                "Blocked", "Sin objetivos válidos para la habilidad."));
        }

        return results;
    }

    // ──────────────────────────────────────────────────────────────
    // DAMAGE RESOLUTION — pipeline por fases
    // ──────────────────────────────────────────────────────────────

    private OnlineCombatResult ResolveDamageAction(
        OnlinePlayer attacker, OnlinePlayer target,
        string actionName, string actionType,
        decimal baseDamage, bool isPhysical,
        SkillDefinition? skillDef,
        bool isBasicAttack,
        IReadOnlyList<CombatConditionType> elements,
        List<ConditionApplication> conditionApps)
    {
        var notes = new List<string>();
        var appliedEffects = new List<string>();

        // ══════════════════════════════════════════════════════════
        // PHASE A — Hit check (Accuracy vs Evasion, con Blindness)
        // ══════════════════════════════════════════════════════════
        // Si el atacante está cegado, su precisión efectiva se trata como 0.
        // Esto mantiene el baseHitChance (85%) pero elimina cualquier bonus
        // de precisión que tuviera, haciendo que el evade del target sea
        // proporcionalmente más efectivo.
        var isBlinded = attacker.Conditions.Any(c => c.ConditionType == "Blind");
        var accuracy = isBlinded ? 0m : GetStat(attacker, StatType.Accuracy);
        var evasion = GetStat(target, StatType.Evasion);

        var hitChance = CombatFormulaConstants.BaseHitChance
                        + (double)(accuracy - evasion) / CombatFormulaConstants.AccuracyEvasionDivisor;
        hitChance = Math.Clamp(hitChance, CombatFormulaConstants.MinHitChance, CombatFormulaConstants.MaxHitChance);

        if (Rng.NextDouble() > hitChance)
        {
            // Miss: el combo NO avanza (lo decide ExecuteBasicAttack post-return)
            return new OnlineCombatResult(
                attacker.PlayerId, target.PlayerId, actionName, actionType,
                "Miss", 0, 0, false, true, false,
                target.CurrentHp, target.MaxHp,
                attacker.CurrentMana, attacker.CurrentHp, attacker.MaxHp,
                Array.Empty<string>(), new[] { "El ataque falló." });
        }

        // ══════════════════════════════════════════════════════════
        // PHASE B — Critical check (CritChance vs CriticalEvasion)
        // ══════════════════════════════════════════════════════════
        var critChance = (double)GetStat(attacker, StatType.CritChance) / 100.0;
        var critEvasion = (double)GetStat(target, StatType.CriticalEvasion) / 100.0;
        var effectiveCritChance = Math.Clamp(
            critChance - critEvasion,
            CombatFormulaConstants.MinCritChance,
            CombatFormulaConstants.MaxCritChance);
        var wasCritical = Rng.NextDouble() < effectiveCritChance;

        // ══════════════════════════════════════════════════════════
        // PHASE C — Damage modifiers (aditivos, con reducciones
        //           aplicadas a cada bono de forma aislada)
        // ══════════════════════════════════════════════════════════
        //
        // Cada "fuente" tiene un bono % y una reducción %. La reducción
        // solo puede neutralizar su propio bono — nunca el daño base.
        //
        // netBonus de cada fuente = bono * (1 - reducción/100)
        //
        // Luego todos los netBonus se suman y se aplican al daño base.

        decimal totalNetBonusPct = 0m;

        // ── Source: Skill vs Basic ──
        if (isBasicAttack)
        {
            var basicInc = GetStat(attacker, StatType.BasicAttackDamageIncrease);
            var basicRed = GetStat(target, StatType.BasicAttackDamageReduction);
            totalNetBonusPct += ApplySourceReduction(basicInc, basicRed);
        }
        else
        {
            var skillInc = GetStat(attacker, StatType.SkillDamageIncrease);
            var skillRed = GetStat(target, StatType.SkillDamageReduction);
            totalNetBonusPct += ApplySourceReduction(skillInc, skillRed);
        }

        // ── Source: PvP (siempre — es PvP multiplayer) ──
        var pvpInc = GetStat(attacker, StatType.PvPDamageIncrease);
        var pvpRed = GetStat(target, StatType.PvPDamageReduction);
        totalNetBonusPct += ApplySourceReduction(pvpInc, pvpRed);

        // ── Source: Critical (SOLO si el hit fue crítico) ──
        if (wasCritical)
        {
            var critDamage = GetStat(attacker, StatType.CritDamage);
            var critIncrease = GetStat(attacker, StatType.CritDamageIncrease);
            var critTotalInc = critDamage + critIncrease;
            var critRed = GetStat(target, StatType.CritDamageTakenReduction);
            totalNetBonusPct += ApplySourceReduction(critTotalInc, critRed);
            notes.Add("¡Golpe crítico!");
        }

        // ── Source: Elementos de la skill ──
        foreach (var elem in elements)
        {
            var (incStat, redStat) = GetElementStats(elem);
            if (incStat is null) continue;
            var elemInc = GetStat(attacker, incStat.Value);
            var elemRed = GetStat(target, redStat!.Value);
            totalNetBonusPct += ApplySourceReduction(elemInc, elemRed);
        }

        // ── Calcular daño inflado (sin sinergia todavía) ──
        // netBonusPct se asegura no-negativo dentro de ApplySourceReduction.
        var inflatedDamage = baseDamage * (1m + totalNetBonusPct / 100m);

        // ══════════════════════════════════════════════════════════
        // PHASE D — Rolls de aplicación de condiciones
        // ══════════════════════════════════════════════════════════
        //
        // Para cada efecto que la skill intenta aplicar, se tira primero
        // la probabilidad de aplicación. Si pasa, se tira el evade del
        // target. Solo los efectos que pasan AMBOS rolls entran en la
        // "lista comprometida" que será realmente aplicada.
        //
        // La separación entre Estado y CC es importante: solo los Estados
        // participan en sinergias y colapsos. Los CC son independientes.
        var committedStates = new List<ConditionApplication>();
        var committedCcs = new List<ConditionApplication>();
        foreach (var app in conditionApps)
        {
            if (Rng.NextDouble() >= app.Chance) continue; // roll de aplicación falló
            var evadeChance = GetConditionEvadeChance(target, app.ConditionType);
            if (Rng.NextDouble() < evadeChance) continue; // target evadió

            if (app.IsCrowdControl) committedCcs.Add(app);
            else committedStates.Add(app);
        }

        // ══════════════════════════════════════════════════════════
        // PHASE E — Detección de sinergia de estado
        // ══════════════════════════════════════════════════════════
        //
        // Regla del sistema (no del catálogo):
        //   · Solo hay sinergias de DOS estados.
        //   · Se disparan cuando un estado nuevo llega a un objetivo
        //     que ya tiene un estado DISTINTO.
        //   · Al disparar: se aplica multiplicador multiplicativo al daño
        //     de ESTA skill, y TODOS los estados del objetivo se limpian
        //     (pero los CC persisten). El estado nuevo NO se aplica
        //     (fue consumido por la sinergia).
        //   · Como máximo una sinergia por skill (break en el primer match).
        //
        // Esto garantiza que nunca haya 3 estados al mismo tiempo.
        bool synergyTriggered = false;
        decimal synergyMultiplier = 1m;
        string? synergyExistingState = null;
        string? synergyIncomingState = null;

        foreach (var newState in committedStates)
        {
            // Buscar cualquier estado EXISTENTE en el target que sea distinto del nuevo.
            var existing = target.Conditions
                .FirstOrDefault(c => c.Category == "State" && c.ConditionType != newState.ConditionType);

            if (existing is null) continue;

            var mult = CombatFormulaConstants.GetStateSynergyMultiplier(
                existing.ConditionType, newState.ConditionType);
            if (mult <= 1m) continue; // par no sinérgico (defensivo)

            // ¡Sinergia detectada!
            synergyTriggered = true;
            synergyMultiplier = mult;
            synergyExistingState = existing.ConditionType;
            synergyIncomingState = newState.ConditionType;
            break; // solo una sinergia por skill
        }

        if (synergyTriggered)
        {
            inflatedDamage *= synergyMultiplier;
            notes.Add($"¡Sinergia {GetConditionSpanishLabel(synergyExistingState!)} + {GetConditionSpanishLabel(synergyIncomingState!)}! (×{synergyMultiplier:F2})");
        }

        // ══════════════════════════════════════════════════════════
        // PHASE F — Defense mitigation (diminishing returns)
        // ══════════════════════════════════════════════════════════
        var defense = isPhysical
            ? GetStat(target, StatType.Defense)
            : GetStat(target, StatType.MagicResistance);
        var penetration = isPhysical
            ? GetStat(attacker, StatType.DefensePenetration)
            : GetStat(attacker, StatType.MagicPenetration);

        var effectiveDefense = Math.Max(0, defense - penetration);
        var reductionFactor = 1m - effectiveDefense / (effectiveDefense + CombatFormulaConstants.DefenseSoftCap);
        var finalDamage = Math.Max(
            CombatFormulaConstants.MinDamage,
            Math.Round(inflatedDamage * reductionFactor, 0));

        // ══════════════════════════════════════════════════════════
        // PHASE G — Apply damage
        // ══════════════════════════════════════════════════════════
        target.CurrentHp = Math.Max(0, target.CurrentHp - finalDamage);
        var targetDefeated = target.CurrentHp <= 0;
        if (targetDefeated)
            notes.Add($"{target.DisplayName} ha sido derrotado.");

        // ══════════════════════════════════════════════════════════
        // PHASE H — Aplicación efectiva de condiciones
        // ══════════════════════════════════════════════════════════
        //
        // Dos caminos:
        //
        //   A) Sinergia disparada:
        //      · Se limpian TODOS los estados existentes del target
        //        (Heat, Cold, Electrified, Poison). Los CC persisten.
        //      · Los estados nuevos comprometidos NO se aplican (consumidos).
        //      · Los CC comprometidos SÍ se aplican con normalidad.
        //
        //   B) Sin sinergia: flujo normal, se aplican todos los efectos
        //      comprometidos (estados y CC).
        if (synergyTriggered)
        {
            // Limpiar todos los Estados del target (Heat/Cold/Electrified/Poison).
            // Los CC (Stun/Freeze/Paralyze/Blind/Weaken) se mantienen intactos.
            target.Conditions.RemoveAll(c => c.Category == "State");
            appliedEffects.Add($"Estados limpiados por sinergia");

            // Aplicar solo los CC comprometidos (no los estados, consumidos).
            foreach (var app in committedCcs)
            {
                ApplyCondition(app, target, appliedEffects);
            }
        }
        else
        {
            // Flujo normal: aplicar todos los efectos comprometidos.
            foreach (var app in committedStates)
            {
                ApplyCondition(app, target, appliedEffects);
            }
            foreach (var app in committedCcs)
            {
                ApplyCondition(app, target, appliedEffects);
            }
        }

        // ══════════════════════════════════════════════════════════
        // PHASE I — Assemble result
        // ══════════════════════════════════════════════════════════
        var outcome = wasCritical ? "CriticalHit" : "Hit";

        return new OnlineCombatResult(
            attacker.PlayerId, target.PlayerId, actionName, actionType,
            outcome, finalDamage, 0, wasCritical, false, targetDefeated,
            target.CurrentHp, target.MaxHp,
            attacker.CurrentMana, attacker.CurrentHp, attacker.MaxHp,
            appliedEffects.AsReadOnly(), notes.AsReadOnly());
    }

    /// <summary>
    /// Aplica una condición pre-comprometida al target. Se asume que los
    /// rolls de aplicación y evasión YA pasaron antes de llamar a este
    /// helper. Aplica Tenacity a la duración si corresponde, y reemplaza
    /// cualquier condición existente del mismo tipo.
    /// </summary>
    private static void ApplyCondition(
        ConditionApplication app, OnlinePlayer target, List<string> appliedEffects)
    {
        var duration = app.BaseDurationSeconds;
        if (app.IsCrowdControl && app.DurationAffectedByTenacity)
        {
            var tenacityPct = Math.Clamp(
                (decimal)GetStat(target, StatType.Tenacity) / 100m,
                0m, CombatFormulaConstants.MaxTenacityReductionPct);
            duration = (float)((decimal)duration * (1m - tenacityPct));
        }

        var condition = new OnlinePlayerCondition(
            app.ConditionType,
            app.IsCrowdControl ? "CrowdControl" : "State",
            duration,
            DateTime.UtcNow);

        // Reemplazar condición existente del mismo tipo (refresh)
        target.Conditions.RemoveAll(c => c.ConditionType == app.ConditionType);
        target.Conditions.Add(condition);
        appliedEffects.Add(GetConditionSpanishLabel(app.ConditionType));
    }

    // ──────────────────────────────────────────────────────────────
    // HEALING
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Curación sobre un único target explícito. Sí, el target puede
    /// ser distinto del caster — ya no está hardcoded. Usado para el
    /// camino legacy (una skill heal sin grupo).
    /// </summary>
    public OnlineCombatResult ResolveHealingAction(
        OnlinePlayer caster, OnlinePlayer target,
        string actionName, decimal baseHealing)
    {
        // No se hace deducción de recursos aquí porque ExecuteSkill ya lo hizo.
        return ResolveHealingActionInternal(caster, target, actionName, baseHealing, deductedAlready: true);
    }

    private OnlineCombatResult ResolveHealingActionInternal(
        OnlinePlayer caster, OnlinePlayer target,
        string actionName, decimal baseHealing,
        bool deductedAlready)
    {
        // Efectividad de la curación del que cura.
        var healingEffectiveness = GetStat(caster, StatType.HealingEffectiveness);

        // Bonus del target al recibir curas (solo si no es auto-cura).
        decimal healingReceivedBonus = 0m;
        if (target.PlayerId != caster.PlayerId)
        {
            healingReceivedBonus = GetStat(target, StatType.HealingReceived);
        }

        // Ambos modifiers son aditivos entre sí (no multiplicativos)
        var totalBonusPct = healingEffectiveness + healingReceivedBonus;
        var finalHealing = Math.Round(baseHealing * (1m + totalBonusPct / 100m), 0);

        var previousHp = target.CurrentHp;
        target.CurrentHp = Math.Min(target.MaxHp, target.CurrentHp + finalHealing);
        var actualHealing = target.CurrentHp - previousHp;

        var note = target.PlayerId == caster.PlayerId
            ? $"Auto-curación: +{actualHealing:F0} HP"
            : $"Curación a {target.DisplayName}: +{actualHealing:F0} HP";

        return new OnlineCombatResult(
            caster.PlayerId, target.PlayerId, actionName, "Skill",
            "Heal", 0, actualHealing, false, false, false,
            target.CurrentHp, target.MaxHp,
            caster.CurrentMana, caster.CurrentHp, caster.MaxHp,
            Array.Empty<string>(),
            new[] { note });
    }

    // ──────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Aplica la reducción del objetivo al incremento de la fuente y devuelve
    /// el neto resultante (nunca negativo). Formalización del modelo:
    ///
    ///     netBonus = max(0, increase - increase × (reduction/100))
    ///              = max(0, increase × (1 - reduction/100))
    ///
    /// Por ejemplo: increase = 50, reduction = 60 → net = 20
    ///              increase = 50, reduction = 150 → net = 0 (no negativo)
    /// </summary>
    private static decimal ApplySourceReduction(decimal increasePct, decimal reductionPct)
    {
        if (increasePct <= 0m) return 0m;
        var reducedFactor = Math.Max(0m, 1m - reductionPct / 100m);
        return increasePct * reducedFactor;
    }

    private static (StatType? increase, StatType? reduction) GetElementStats(CombatConditionType element)
    {
        return element switch
        {
            CombatConditionType.Heat => (StatType.HeatDamageIncrease, StatType.HeatDamageReduction),
            CombatConditionType.Cold => (StatType.ColdDamageIncrease, StatType.ColdDamageReduction),
            CombatConditionType.Electrified => (StatType.ElectrifiedDamageIncrease, StatType.ElectrifiedDamageReduction),
            CombatConditionType.Poison => (StatType.PoisonDamageIncrease, StatType.PoisonDamageReduction),
            _ => (null, null)
        };
    }

    private static IReadOnlyList<CombatConditionType> ExtractElements(SkillDefinition? skillDef)
    {
        if (skillDef?.BaseTuning.Effects is null || skillDef.BaseTuning.Effects.Count == 0)
            return Array.Empty<CombatConditionType>();

        var set = new HashSet<CombatConditionType>();
        foreach (var effect in skillDef.BaseTuning.Effects)
        {
            // Solo los elementos "State" cuentan como daño elemental para modifiers
            var cat = CombatConditionCatalog.Get(effect.Condition).Category;
            if (cat == CombatConditionCategory.State) set.Add(effect.Condition);
        }
        return set.ToArray();
    }

    /// <summary>
    /// Deduce mana y fija el cooldown del skill aplicando CooldownReduction.
    /// </summary>
    private static void DeductManaAndSetCooldown(OnlinePlayer attacker, OnlinePlayerSkill skill)
    {
        attacker.CurrentMana = Math.Max(0, attacker.CurrentMana - skill.ManaCost);

        if (skill.BaseCooldownSeconds > 0)
        {
            var cdrPct = Math.Clamp(
                GetStat(attacker, StatType.CooldownReduction) / 100m,
                0m, CombatFormulaConstants.MaxCooldownReductionPct);
            var finalCd = (float)(skill.BaseCooldownSeconds * (1m - cdrPct));
            if (finalCd < CombatFormulaConstants.MinSkillCooldownSeconds)
                finalCd = CombatFormulaConstants.MinSkillCooldownSeconds;
            attacker.Cooldowns[skill.SkillId] = finalCd;
        }
    }

    /// <summary>
    /// Calcula la magnitud base del daño/curación de una skill usando
    /// escalado dual (primario + secundario). Cada clase define su propia
    /// proporción de stats que alimentan el daño:
    ///
    ///   Sorcerer:    90% MagicAttack + 10% PhysicalAttack
    ///   Juramentada: 60% MagicAttack + 40% PhysicalAttack
    ///   Lancero:     40% MagicAttack + 60% PhysicalAttack
    ///   Bruiser:     10% MagicAttack + 90% PhysicalAttack
    ///
    /// El resultado es: BaseMagnitude + (PrimaryCoef × PrimaryStat)
    ///                                + (SecondaryCoef × SecondaryStat)
    /// </summary>
    private static decimal CalculateBaseMagnitude(
        OnlinePlayer attacker, OnlinePlayerSkill skill,
        SkillDefinition? skillDef, bool isPhysical)
    {
        if (skillDef is not null)
        {
            var magnitude = skillDef.BaseTuning.Action.MagnitudeProfile;

            // Stat primaria
            var primaryStat = magnitude.ScalingType switch
            {
                SkillScalingType.PhysicalAttack => GetStat(attacker, StatType.PhysicalAttack),
                SkillScalingType.MagicAttack => GetStat(attacker, StatType.MagicAttack),
                _ => 0m
            };

            // Stat secundaria (dual scaling)
            var secondaryStat = magnitude.SecondaryScalingType switch
            {
                SkillScalingType.PhysicalAttack => GetStat(attacker, StatType.PhysicalAttack),
                SkillScalingType.MagicAttack => GetStat(attacker, StatType.MagicAttack),
                _ => 0m
            };

            return magnitude.BaseMagnitude
                   + magnitude.ScalingCoefficient * primaryStat
                   + magnitude.SecondaryScalingCoefficient * secondaryStat;
        }

        // Fallback si la skill no está en el catálogo
        var attackStat = isPhysical
            ? GetStat(attacker, StatType.PhysicalAttack)
            : GetStat(attacker, StatType.MagicAttack);
        return attackStat * CombatFormulaConstants.DefaultSkillFallbackCoef;
    }

    private static bool IsHealingSkill(OnlinePlayerSkill skill)
    {
        var def = GetSkillDefinition(skill.SkillId);
        return def?.BaseTuning.Action.ActionType == SkillActionType.Heal;
    }

    private static decimal GetStat(OnlinePlayer player, StatType stat)
    {
        var key = stat.ToString();
        return player.Stats.TryGetValue(key, out var value) ? value : 0m;
    }

    private static SkillDefinition? GetSkillDefinition(string skillId)
    {
        try
        {
            return SkillCatalogRegistry.Current.ClassCatalogs
                .SelectMany(c => c.Skills)
                .FirstOrDefault(s => string.Equals(s.Id, skillId, StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    /// <summary>
    /// Fusiona los efectos base de la skill con los overrides de ascensión
    /// acumulados hasta el nivel de ascensión del jugador.
    ///
    /// Pipeline de fusión por nivel (1..ascensionLevel):
    ///   1. Base effects (skillDef.BaseTuning.Effects)
    ///   2. RemovedEffectKeys → quitar del diccionario por EffectKey
    ///   3. AddedEffects → añadir nuevos al diccionario
    ///   4. EffectOverrides → actualizar campos de los existentes (duration, chance, etc.)
    /// </summary>
    private static List<ConditionApplication> BuildConditionApplications(
        SkillDefinition? skillDef, int ascensionLevel)
    {
        var apps = new List<ConditionApplication>();
        if (skillDef is null) return apps;

        // Diccionario indexado por EffectKey para permitir overrides/removals por clave.
        var effectsByKey = new Dictionary<string, SkillConditionEffectDefinition>();

        // Paso 0: efectos base
        if (skillDef.BaseTuning.Effects is { Count: > 0 } baseEffects)
        {
            foreach (var effect in baseEffects)
            {
                effectsByKey[effect.EffectKey] = effect;
            }
        }

        // Pasos 1..N: aplicar overrides hasta el nivel de ascensión del jugador
        if (skillDef.AscensionOverrides is not null)
        {
            for (var level = 1; level <= ascensionLevel; level++)
            {
                if (!skillDef.AscensionOverrides.TryGetValue(level, out var ovr) || ovr is null)
                    continue;

                // Removals primero
                if (ovr.RemovedEffectKeys is { Count: > 0 })
                {
                    foreach (var key in ovr.RemovedEffectKeys)
                        effectsByKey.Remove(key);
                }

                // Added effects — nuevos desbloqueados en este nivel
                if (ovr.AddedEffects is { Count: > 0 })
                {
                    foreach (var eff in ovr.AddedEffects)
                        effectsByKey[eff.EffectKey] = eff;
                }

                // Effect overrides — mergean campos específicos de efectos existentes
                if (ovr.EffectOverrides is { Count: > 0 })
                {
                    foreach (var patch in ovr.EffectOverrides)
                    {
                        if (!effectsByKey.TryGetValue(patch.EffectKey, out var current)) continue;
                        effectsByKey[patch.EffectKey] = MergeEffectOverride(current, patch);
                    }
                }
            }
        }

        // Construir las ConditionApplication a partir del diccionario final
        foreach (var effect in effectsByKey.Values)
        {
            var conditionDef = CombatConditionCatalog.Get(effect.Condition);
            var isCC = conditionDef.Category == CombatConditionCategory.CrowdControl;

            // Probabilidad base del efecto
            double baseChance;
            if (effect.BaseApplyChance.HasValue)
            {
                baseChance = (double)effect.BaseApplyChance.Value;
            }
            else
            {
                // Sin base chance explícito: valor sensato por default
                baseChance = 0.30;
            }

            // ApplyChanceMultiplier y flat bonus
            var effectiveChance = baseChance * (double)effect.ApplyChanceMultiplier
                                  + (double)effect.ApplyChanceFlatBonus;

            effectiveChance = Math.Clamp(
                effectiveChance,
                CombatFormulaConstants.MinConditionApplyChance,
                CombatFormulaConstants.MaxConditionApplyChance);

            var duration = (float)(effect.BaseDurationSeconds ?? 0m);
            if (duration <= 0)
                duration = isCC
                    ? CombatFormulaConstants.DefaultConditionDurationCrowdControl
                    : CombatFormulaConstants.DefaultConditionDurationState;

            apps.Add(new ConditionApplication(
                conditionDef.Type.ToString(),
                isCC,
                conditionDef.DurationAffectedByTenacity,
                effectiveChance,
                duration));
        }

        return apps;
    }

    /// <summary>
    /// Combina un efecto existente con un override parcial, conservando los
    /// valores del efecto original cuando el override no especifica el campo.
    /// </summary>
    private static SkillConditionEffectDefinition MergeEffectOverride(
        SkillConditionEffectDefinition current,
        SkillConditionEffectOverride patch)
    {
        return current with
        {
            BaseDurationSeconds = patch.BaseDurationSeconds ?? current.BaseDurationSeconds,
            BaseApplyChance = patch.BaseApplyChance ?? current.BaseApplyChance,
            ApplyChanceFlatBonus = patch.ApplyChanceFlatBonus ?? current.ApplyChanceFlatBonus,
            ApplyChanceMultiplier = patch.ApplyChanceMultiplier ?? current.ApplyChanceMultiplier
        };
    }

    private static double GetConditionEvadeChance(OnlinePlayer target, string conditionType)
    {
        var evadeStat = conditionType switch
        {
            "Heat" => StatType.HeatEvadeChance,
            "Cold" => StatType.ColdEvadeChance,
            "Electrified" => StatType.ElectrifiedEvadeChance,
            "Poison" => StatType.PoisonEvadeChance,
            "Weaken" => StatType.WeakenEvadeChance,
            "Blind" => StatType.BlindEvadeChance,
            "Stun" => StatType.StunEvadeChance,
            "Freeze" => StatType.FreezeEvadeChance,
            "Paralyze" => StatType.ParalyzeEvadeChance,
            _ => StatType.Tenacity
        };
        return (double)GetStat(target, evadeStat) / 100.0;
    }

    private static OnlineCombatResult CreateFailResult(
        OnlinePlayer attacker, OnlinePlayer target,
        string actionName, string actionType,
        string outcome, string reason)
    {
        return new OnlineCombatResult(
            attacker.PlayerId, target.PlayerId, actionName, actionType,
            outcome, 0, 0, false, false, false,
            target.CurrentHp, target.MaxHp,
            attacker.CurrentMana, attacker.CurrentHp, attacker.MaxHp,
            Array.Empty<string>(), new[] { reason });
    }

    private static string GetConditionSpanishLabel(string conditionType)
    {
        return conditionType switch
        {
            "Heat" => "Calor",
            "Cold" => "Frío",
            "Electrified" => "Electrificado",
            "Poison" => "Veneno",
            "Weaken" => "Debilitado",
            "Blind" => "Ceguera",
            "Stun" => "Aturdido",
            "Freeze" => "Congelado",
            "Paralyze" => "Paralizado",
            _ => conditionType
        };
    }

    // ── Modelo interno de aplicación de condición ──
    private sealed record ConditionApplication(
        string ConditionType,
        bool IsCrowdControl,
        bool DurationAffectedByTenacity,
        double Chance,
        float BaseDurationSeconds);
}
