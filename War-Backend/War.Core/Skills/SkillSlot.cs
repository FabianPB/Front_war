namespace War.Core.Skills;

public enum SkillSlot
{
    Slot01 = 1,
    Slot02 = 2,
    Slot03 = 3,
    Slot04 = 4,
    Slot05 = 5,
    Slot06 = 6,
    Slot07 = 7,
    Slot08 = 8,
    Slot09 = 9,
    Slot10 = 10,
    Slot11 = 11,
    Slot12 = 12,
    Slot13 = 13
}

public static class SkillSlotExtensions
{
    public static int GetOrder(this SkillSlot slot)
    {
        return (int)slot;
    }
}
