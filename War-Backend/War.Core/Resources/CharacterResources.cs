namespace War.Core.Resources;

public class CharacterResources
{
    public decimal CurrentHp { get; private set; }
    public decimal CurrentMana { get; private set; }
    public decimal UltimateCharge { get; private set; }

    public CharacterResources(decimal hp, decimal mana, decimal ultimateCharge = 0m)
    {
        SetHp(hp);
        SetMana(mana);
        SetUltimateCharge(ultimateCharge);
    }

    public decimal this[CharacterResourceType resourceType] => Get(resourceType);

    public decimal Get(CharacterResourceType resourceType)
    {
        return resourceType switch
        {
            CharacterResourceType.Hp => CurrentHp,
            CharacterResourceType.Mana => CurrentMana,
            CharacterResourceType.UltimateCharge => UltimateCharge,
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, "Unknown resource type.")
        };
    }

    public bool HasAvailable(CharacterResourceType resourceType, decimal requiredAmount)
    {
        return Get(resourceType) >= Normalize(requiredAmount);
    }

    public void Set(CharacterResourceType resourceType, decimal value)
    {
        var normalizedValue = Normalize(value);

        switch (resourceType)
        {
            case CharacterResourceType.Hp:
                CurrentHp = normalizedValue;
                break;
            case CharacterResourceType.Mana:
                CurrentMana = normalizedValue;
                break;
            case CharacterResourceType.UltimateCharge:
                UltimateCharge = normalizedValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, "Unknown resource type.");
        }
    }

    public void SetHp(decimal hp)
    {
        Set(CharacterResourceType.Hp, hp);
    }

    public void SetMana(decimal mana)
    {
        Set(CharacterResourceType.Mana, mana);
    }

    public void SetUltimateCharge(decimal ultimateCharge)
    {
        Set(CharacterResourceType.UltimateCharge, ultimateCharge);
    }

    private static decimal Normalize(decimal value)
    {
        return value < 0 ? 0 : value;
    }
}