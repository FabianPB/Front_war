using War.Core.Resources;
using War.Core.Stats;

namespace War.Core.Combat;

public enum CombatActionScalingType
{
    FixedOnly,
    PhysicalAttack,
    MagicAttack,
    TargetMissingHp
}

public sealed record CombatActionMagnitudeProfile(
    decimal FixedBaseMagnitude,
    CombatActionScalingType ScalingType = CombatActionScalingType.FixedOnly,
    decimal ScalingCoefficient = 0m,
    string? ConfigurationName = null);

public sealed record CombatActionMagnitudeResolution(
    decimal FixedBaseMagnitude,
    CombatActionScalingType ScalingType,
    StatType? ScalingStatType,
    decimal ScalingStatValue,
    decimal ScalingCoefficient,
    decimal ScaledContribution,
    decimal FinalBaseMagnitude,
    bool UsedLegacyBaseMagnitudeFallback,
    string? ScalingInputLabel = null,
    string? ConfigurationName = null,
    string? Note = null)
{
    public bool UsesDynamicScalingInput =>
        ScalingStatType is null && !string.IsNullOrWhiteSpace(ScalingInputLabel);

    public bool HasScaling =>
        ScalingType != CombatActionScalingType.FixedOnly &&
        ScalingCoefficient > 0m &&
        (ScalingStatType.HasValue || UsesDynamicScalingInput);
}

public interface ICombatActionMagnitudeService
{
    CombatActionMagnitudeResolution Resolve(CombatEventContext context);
}

public sealed class CombatActionMagnitudeService : ICombatActionMagnitudeService
{
    public CombatActionMagnitudeResolution Resolve(CombatEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var profile = context.MagnitudeProfile;
        var usedLegacyBaseMagnitudeFallback = profile is null;
        var fixedBaseMagnitude = profile?.FixedBaseMagnitude ?? context.BaseMagnitude;
        var scalingType = profile?.ScalingType ?? CombatActionScalingType.FixedOnly;
        var scalingCoefficient = profile?.ScalingCoefficient ?? 0m;
        var configurationName = profile?.ConfigurationName;

        if (fixedBaseMagnitude < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(context), fixedBaseMagnitude, "Fixed base magnitude cannot be negative.");
        }

        if (scalingCoefficient < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(context), scalingCoefficient, "Scaling coefficient cannot be negative.");
        }

        if (scalingType == CombatActionScalingType.FixedOnly && scalingCoefficient > 0m)
        {
            throw new ArgumentException("A fixed-only action magnitude profile cannot declare a positive scaling coefficient.", nameof(context));
        }

        var (scalingStatType, scalingInputLabel, scalingInputValue) = ResolveScalingInput(context, scalingType);
        var scaledContribution = scalingInputValue * scalingCoefficient;
        var finalBaseMagnitude = fixedBaseMagnitude + scaledContribution;

        return new CombatActionMagnitudeResolution(
            fixedBaseMagnitude,
            scalingType,
            scalingStatType,
            scalingInputValue,
            scalingCoefficient,
            scaledContribution,
            finalBaseMagnitude,
            usedLegacyBaseMagnitudeFallback,
            scalingInputLabel,
            configurationName,
            usedLegacyBaseMagnitudeFallback
                ? "Legacy BaseMagnitude fallback was used because no explicit action magnitude profile was supplied."
                : null);
    }

    private static (StatType? ScalingStatType, string? ScalingInputLabel, decimal ScalingInputValue) ResolveScalingInput(
        CombatEventContext context,
        CombatActionScalingType scalingType)
    {
        return scalingType switch
        {
            CombatActionScalingType.FixedOnly => (null, null, 0m),
            CombatActionScalingType.PhysicalAttack => (StatType.PhysicalAttack, null, context.Actor.Stats.Get(StatType.PhysicalAttack)),
            CombatActionScalingType.MagicAttack => (StatType.MagicAttack, null, context.Actor.Stats.Get(StatType.MagicAttack)),
            CombatActionScalingType.TargetMissingHp => (null, "TargetMissingHp", GetTargetMissingHp(context)),
            _ => throw new ArgumentOutOfRangeException(nameof(scalingType), scalingType, "Unsupported action scaling type.")
        };
    }

    private static decimal GetTargetMissingHp(CombatEventContext context)
    {
        var maximumHp = context.Target.GetResourceMaximum(CharacterResourceType.Hp);
        var currentHp = context.Target.GetCurrentResource(CharacterResourceType.Hp);
        return Math.Max(0m, maximumHp - currentHp);
    }
}
