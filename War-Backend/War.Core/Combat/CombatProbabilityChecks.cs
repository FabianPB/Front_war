namespace War.Core.Combat;

public interface ICombatProbabilityService
{
    CombatProbabilityCheckResult Evaluate(decimal successChance, string? note = null);
}

public sealed class RandomCombatProbabilityService : ICombatProbabilityService
{
    private readonly Random _random;

    public RandomCombatProbabilityService(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public CombatProbabilityCheckResult Evaluate(decimal successChance, string? note = null)
    {
        var effectiveChance = Math.Clamp(successChance, 0m, 1m);
        var roll = (decimal)_random.NextDouble();

        return new CombatProbabilityCheckResult(
            successChance,
            effectiveChance,
            roll,
            Succeeded: roll < effectiveChance,
            WasChanceClamped: effectiveChance != successChance,
            note);
    }
}
