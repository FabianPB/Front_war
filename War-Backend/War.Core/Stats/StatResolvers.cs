using War.Core.Combat;

namespace War.Core.Stats;

public interface IMitigationResolver
{
    MitigationResolution Resolve(StatType statType, decimal rawValue);
}

public sealed class MitigationResolver : IMitigationResolver
{
    private readonly MitigationCurveOptions _options;

    public MitigationResolver(MitigationCurveOptions? options = null)
    {
        _options = Validate(options ?? new MitigationCurveOptions());
    }

    public MitigationResolution Resolve(StatType statType, decimal rawValue)
    {
        if (statType is not (StatType.Defense or StatType.MagicResistance))
        {
            throw new ArgumentException($"Stat '{statType}' is not supported by the mitigation resolver.", nameof(statType));
        }

        var sanitizedValue = rawValue < 0m ? 0m : rawValue;
        var mitigationRatio = sanitizedValue == 0m
            ? 0m
            : _options.AsymptoticCapRatio * (sanitizedValue / (sanitizedValue + _options.Softness));

        mitigationRatio = Math.Clamp(mitigationRatio, 0m, _options.AsymptoticCapRatio);

        return new MitigationResolution(
            statType,
            sanitizedValue,
            mitigationRatio,
            _options.AsymptoticCapRatio,
            UsesProvisionalCurve: true);
    }

    private static MitigationCurveOptions Validate(MitigationCurveOptions options)
    {
        if (options.AsymptoticCapRatio <= 0m || options.AsymptoticCapRatio >= 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.AsymptoticCapRatio, "Mitigation cap ratio must be between 0 and 1 (exclusive).");
        }

        if (options.Softness <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Softness, "Mitigation softness must be greater than zero.");
        }

        return options;
    }
}

public interface IHitChanceResolver
{
    HitChanceResolution Resolve(decimal accuracy, decimal evasion);
}

public sealed class HitChanceResolver : IHitChanceResolver
{
    private readonly HitChanceCurveOptions _options;

    public HitChanceResolver(HitChanceCurveOptions? options = null)
    {
        _options = Validate(options ?? new HitChanceCurveOptions());
    }

    public HitChanceResolution Resolve(decimal accuracy, decimal evasion)
    {
        var sanitizedAccuracy = accuracy < 0m ? 0m : accuracy;
        var sanitizedEvasion = evasion < 0m ? 0m : evasion;
        var difference = sanitizedAccuracy - sanitizedEvasion;
        var normalizedDifference = difference == 0m
            ? 0m
            : difference / (Math.Abs(difference) + _options.SmoothingValue);

        var chanceToHit = normalizedDifference >= 0m
            ? _options.BaseChanceWhenEqual + ((_options.MaximumChance - _options.BaseChanceWhenEqual) * normalizedDifference)
            : _options.BaseChanceWhenEqual + ((_options.BaseChanceWhenEqual - _options.MinimumChance) * normalizedDifference);

        chanceToHit = Math.Clamp(chanceToHit, _options.MinimumChance, _options.MaximumChance);

        return new HitChanceResolution(
            sanitizedAccuracy,
            sanitizedEvasion,
            chanceToHit,
            _options.BaseChanceWhenEqual,
            _options.MinimumChance,
            _options.MaximumChance,
            UsesProvisionalCurve: true);
    }

    private static HitChanceCurveOptions Validate(HitChanceCurveOptions options)
    {
        if (options.MinimumChance < 0m || options.MinimumChance > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MinimumChance, "Minimum hit chance must be between 0 and 1.");
        }

        if (options.MaximumChance < 0m || options.MaximumChance > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaximumChance, "Maximum hit chance must be between 0 and 1.");
        }

        if (options.BaseChanceWhenEqual < options.MinimumChance || options.BaseChanceWhenEqual > options.MaximumChance)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.BaseChanceWhenEqual, "Base hit chance must be within the configured minimum and maximum range.");
        }

        if (options.SmoothingValue <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.SmoothingValue, "Hit-chance smoothing must be greater than zero.");
        }

        return options;
    }
}

public interface ICriticalChanceResolver
{
    CriticalChanceResolution Resolve(decimal critChance, decimal criticalEvasion);
}

public sealed class CriticalChanceResolver : ICriticalChanceResolver
{
    public CriticalChanceResolution Resolve(decimal critChance, decimal criticalEvasion)
    {
        var sanitizedCritChance = critChance < 0m ? 0m : critChance;
        var sanitizedCriticalEvasion = criticalEvasion < 0m ? 0m : criticalEvasion;
        var unboundedEffectiveCritChance = sanitizedCritChance - sanitizedCriticalEvasion;
        var effectiveCritChance = Math.Clamp(unboundedEffectiveCritChance, 0m, 1m);

        return new CriticalChanceResolution(
            sanitizedCritChance,
            sanitizedCriticalEvasion,
            unboundedEffectiveCritChance,
            effectiveCritChance,
            WasClampedToProbabilityBounds: effectiveCritChance != unboundedEffectiveCritChance);
    }
}

public interface ICrowdControlDurationResolver
{
    CrowdControlDurationResolution Resolve(decimal tenacity);
}

public sealed class CrowdControlDurationResolver : ICrowdControlDurationResolver
{
    private readonly CrowdControlDurationOptions _options;

    public CrowdControlDurationResolver(CrowdControlDurationOptions? options = null)
    {
        _options = Validate(options ?? new CrowdControlDurationOptions());
    }

    public CrowdControlDurationResolution Resolve(decimal tenacity)
    {
        var sanitizedTenacity = tenacity < 0m ? 0m : tenacity;
        var unboundedDurationMultiplier = _options.BaseDurationMultiplier - (sanitizedTenacity * _options.DurationReductionPerTenacity);
        var durationMultiplier = Math.Clamp(
            unboundedDurationMultiplier,
            _options.MinimumDurationMultiplier,
            _options.MaximumDurationMultiplier);

        return new CrowdControlDurationResolution(
            sanitizedTenacity,
            unboundedDurationMultiplier,
            durationMultiplier,
            _options.MinimumDurationMultiplier,
            _options.MaximumDurationMultiplier,
            UsesProvisionalCurve: true,
            WasClampedToConfiguredBounds: durationMultiplier != unboundedDurationMultiplier);
    }

    private static CrowdControlDurationOptions Validate(CrowdControlDurationOptions options)
    {
        if (options.MinimumDurationMultiplier < 0m || options.MinimumDurationMultiplier > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MinimumDurationMultiplier, "Minimum CC duration multiplier must be between 0 and 1.");
        }

        if (options.MaximumDurationMultiplier < 0m || options.MaximumDurationMultiplier > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaximumDurationMultiplier, "Maximum CC duration multiplier must be between 0 and 1.");
        }

        if (options.MinimumDurationMultiplier > options.MaximumDurationMultiplier)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MinimumDurationMultiplier, "Minimum CC duration multiplier cannot be greater than the maximum multiplier.");
        }

        if (options.BaseDurationMultiplier < options.MinimumDurationMultiplier || options.BaseDurationMultiplier > options.MaximumDurationMultiplier)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.BaseDurationMultiplier, "Base CC duration multiplier must be within the configured minimum and maximum range.");
        }

        if (options.DurationReductionPerTenacity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.DurationReductionPerTenacity, "Tenacity duration reduction factor cannot be negative.");
        }

        return options;
    }
}

public interface IDamageModifierResolver
{
    DamageModifierProfile ResolveForContext(FinalStats finalStats, DamageModifierContext context);

    DamageModifierProfile? TryResolveForCondition(FinalStats finalStats, CombatConditionType condition);
}

public sealed class DamageModifierResolver : IDamageModifierResolver
{
    public DamageModifierProfile ResolveForContext(FinalStats finalStats, DamageModifierContext context)
    {
        ArgumentNullException.ThrowIfNull(finalStats);

        var (increaseStat, reductionStat) = context switch
        {
            DamageModifierContext.BasicAttack => (StatType.BasicAttackDamageIncrease, StatType.BasicAttackDamageReduction),
            DamageModifierContext.Skill => (StatType.SkillDamageIncrease, StatType.SkillDamageReduction),
            DamageModifierContext.Critical => (StatType.CritDamageIncrease, StatType.CritDamageTakenReduction),
            DamageModifierContext.Monster => (StatType.MonsterDamageIncrease, StatType.MonsterDamageReduction),
            DamageModifierContext.Boss => (StatType.BossDamageIncrease, StatType.BossDamageReduction),
            DamageModifierContext.PvP => (StatType.PvPDamageIncrease, StatType.PvPDamageReduction),
            DamageModifierContext.Heat => (StatType.HeatDamageIncrease, StatType.HeatDamageReduction),
            DamageModifierContext.Cold => (StatType.ColdDamageIncrease, StatType.ColdDamageReduction),
            DamageModifierContext.Electrified => (StatType.ElectrifiedDamageIncrease, StatType.ElectrifiedDamageReduction),
            DamageModifierContext.Poison => (StatType.PoisonDamageIncrease, StatType.PoisonDamageReduction),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Unknown damage modifier context.")
        };

        return new DamageModifierProfile(
            context,
            finalStats.Get(increaseStat),
            finalStats.Get(reductionStat));
    }

    public DamageModifierProfile? TryResolveForCondition(FinalStats finalStats, CombatConditionType condition)
    {
        return condition switch
        {
            CombatConditionType.Heat => ResolveForContext(finalStats, DamageModifierContext.Heat),
            CombatConditionType.Cold => ResolveForContext(finalStats, DamageModifierContext.Cold),
            CombatConditionType.Electrified => ResolveForContext(finalStats, DamageModifierContext.Electrified),
            CombatConditionType.Poison => ResolveForContext(finalStats, DamageModifierContext.Poison),
            _ => null
        };
    }
}

public interface IStatusChanceResolver
{
    StatusChanceProfile Resolve(FinalStats finalStats, CombatConditionType condition);
}

public sealed class StatusChanceResolver : IStatusChanceResolver
{
    public StatusChanceProfile Resolve(FinalStats finalStats, CombatConditionType condition)
    {
        ArgumentNullException.ThrowIfNull(finalStats);

        var definition = CombatConditionCatalog.Get(condition);

        return new StatusChanceProfile(
            condition,
            finalStats.Get(definition.ApplyChanceStat),
            finalStats.Get(definition.EvadeChanceStat),
            definition.RequiresHitBeforeApplication,
            definition.ChecksEvadeAfterApplyRoll,
            definition.DurationAffectedByTenacity);
    }
}
