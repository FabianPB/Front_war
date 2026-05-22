namespace War.Api.Application.GameWorld;

/// <summary>
/// Constantes de las fórmulas de combate. Centraliza todos los números
/// mágicos del motor de resolución de daño y curación para que puedan
/// ajustarse desde un solo archivo.
///
/// Separado de CombatTimingConstants (timing del pipeline) porque estos
/// valores afectan la fórmula matemática, no el rate limiting/timing.
/// </summary>
public static class CombatFormulaConstants
{
    // ── Hit check ──
    /// <summary>Probabilidad base de acertar (85%).</summary>
    public const double BaseHitChance = 0.85;
    /// <summary>Probabilidad mínima de acertar (10%).</summary>
    public const double MinHitChance = 0.10;
    /// <summary>Probabilidad máxima de acertar (98%).</summary>
    public const double MaxHitChance = 0.98;
    /// <summary>Divisor para normalizar (Accuracy - Evasion) al rango 0–1.</summary>
    public const double AccuracyEvasionDivisor = 1000.0;

    // ── Cast times (segundos) — SECRETO del sistema, no se broadcast ──
    /// <summary>Cast time default de una skill regular (no ultimate).</summary>
    public const decimal DefaultSkillCastTimeSeconds = 0.30m;
    /// <summary>Cast time default de una skill ultimate (más lenta).</summary>
    public const decimal UltimateCastTimeSeconds = 0.50m;

    // ── Critical hit (ADITIVO, no multiplicativo) ──
    /// <summary>Probabilidad mínima de crítico (1%).</summary>
    public const double MinCritChance = 0.01;
    /// <summary>Probabilidad máxima de crítico (60%).</summary>
    public const double MaxCritChance = 0.60;

    // ── Defensa (diminishing returns) ──
    /// <summary>Constante de ablandamiento de defensa. damage × (1 - def/(def+300)).</summary>
    public const decimal DefenseSoftCap = 300m;

    // ── Daño ──
    /// <summary>Daño mínimo garantizado (ningún hit pega menos de esto).</summary>
    public const decimal MinDamage = 1m;
    /// <summary>Coeficiente del ataque básico: daño base = AttackStat × coef.</summary>
    public const decimal BasicAttackBaseCoef = 0.80m;
    /// <summary>Coeficiente fallback cuando una skill no tiene definición en el catálogo.</summary>
    public const decimal DefaultSkillFallbackCoef = 1.20m;

    // ── Cooldown reduction ──
    /// <summary>Porcentaje máximo de CDR permitido (70%). Clamp a la stat CooldownReduction.</summary>
    public const decimal MaxCooldownReductionPct = 0.70m;
    /// <summary>Cooldown mínimo de una skill tras aplicar CDR (evita cooldowns de 0s).</summary>
    public const float MinSkillCooldownSeconds = 0.5f;

    // ── Tenacity ──
    /// <summary>Porcentaje máximo de reducción de duración de CC por Tenacity (70%).</summary>
    public const decimal MaxTenacityReductionPct = 0.70m;

    // ── Condiciones ──
    /// <summary>Probabilidad mínima de aplicación de condiciones (5%).</summary>
    public const double MinConditionApplyChance = 0.05;
    /// <summary>Probabilidad máxima de aplicación de condiciones (80%).</summary>
    public const double MaxConditionApplyChance = 0.80;
    /// <summary>Duración default para condiciones CC cuando la skill no la especifica.</summary>
    public const float DefaultConditionDurationCrowdControl = 3f;
    /// <summary>Duración default para condiciones State cuando la skill no la especifica.</summary>
    public const float DefaultConditionDurationState = 6f;

    // ── World tick ──
    /// <summary>Porcentaje de max mana que se regenera por tick (2%).</summary>
    public const float ManaRegenPercentPerTick = 0.02f;
    /// <summary>Daño por tick de DoT (Poison/Heat) como porcentaje de max HP (1.5%).</summary>
    public const float DotDamagePercentPerTick = 0.015f;
    /// <summary>Multiplicador de DoT cuando Poison y Heat coexisten.</summary>
    public const decimal StackedDotMultiplier = 1.5m;

    // ──────────────────────────────────────────────────────────────
    // SINERGIAS DE ESTADO (responsabilidad del sistema de combate)
    // ──────────────────────────────────────────────────────────────
    //
    // Las sinergias son un mecanismo PURO DEL SISTEMA DE COMBATE. Las
    // habilidades solo declaran qué efectos aplican — nunca conocen
    // estas reglas. Cuando un estado nuevo llega a un objetivo que ya
    // tiene un estado distinto, se dispara una sinergia:
    //
    //   1. Se aplica un multiplicador de daño (valor de la tabla).
    //   2. Se limpian TODOS los estados del objetivo (no los CC).
    //   3. El estado nuevo NO se aplica (fue "consumido" por la sinergia).
    //
    // Solo existen sinergias de 2. Nunca hay 3 estados al mismo tiempo
    // porque la sinergia colapsa los 2 a 0 inmediatamente.
    //
    // Los estados "State" (no CC) son: Heat, Cold, Electrified, Poison.
    // Combinaciones totales: C(4,2) = 6 pares únicos.

    /// <summary>Calor + Frío: choque térmico extremo.</summary>
    public const decimal HeatColdSynergyMultiplier = 1.30m;
    /// <summary>Calor + Electrificado: sobrecarga plásmica.</summary>
    public const decimal HeatElectrifiedSynergyMultiplier = 1.22m;
    /// <summary>Calor + Veneno: combustión tóxica.</summary>
    public const decimal HeatPoisonSynergyMultiplier = 1.20m;
    /// <summary>Frío + Electrificado: choque criogénico.</summary>
    public const decimal ColdElectrifiedSynergyMultiplier = 1.22m;
    /// <summary>Frío + Veneno: toxina hipotérmica.</summary>
    public const decimal ColdPoisonSynergyMultiplier = 1.20m;
    /// <summary>Electrificado + Veneno: corrosión eléctrica.</summary>
    public const decimal ElectrifiedPoisonSynergyMultiplier = 1.20m;

    /// <summary>
    /// Devuelve el multiplicador de sinergia para un par de estados.
    /// El orden de los argumentos es irrelevante (conmutativo).
    /// Si el par no es una sinergia válida, devuelve 1.0 (sin bonus).
    /// </summary>
    public static decimal GetStateSynergyMultiplier(string stateA, string stateB)
    {
        if (string.Equals(stateA, stateB, StringComparison.Ordinal))
            return 1m; // mismo estado no es sinergia

        // Normalizamos a orden alfabético para hacer la tabla conmutativa
        var (first, second) = string.CompareOrdinal(stateA, stateB) < 0
            ? (stateA, stateB)
            : (stateB, stateA);

        return (first, second) switch
        {
            ("Cold", "Heat") => HeatColdSynergyMultiplier,
            ("Cold", "Electrified") => ColdElectrifiedSynergyMultiplier,
            ("Cold", "Poison") => ColdPoisonSynergyMultiplier,
            ("Electrified", "Heat") => HeatElectrifiedSynergyMultiplier,
            ("Electrified", "Poison") => ElectrifiedPoisonSynergyMultiplier,
            ("Heat", "Poison") => HeatPoisonSynergyMultiplier,
            _ => 1m
        };
    }

    // ── Debug / observability ──
    /// <summary>Cuando true, el resultado de combate incluye un desglose fase-a-fase.</summary>
    public const bool IncludeDamageBreakdown = false;
}
