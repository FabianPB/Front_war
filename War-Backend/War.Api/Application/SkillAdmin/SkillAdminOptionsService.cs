using War.Core.Combat;
using War.Core.PowerScore;
using War.Core.Resources;
using War.Core.Skills;

namespace War.Api.Application.SkillAdmin;

public interface ISkillAdminOptionsService
{
    SkillAdminOptionsDto GetOptions();
}

public sealed class SkillAdminOptionsService : ISkillAdminOptionsService
{
    public SkillAdminOptionsDto GetOptions()
    {
        return new SkillAdminOptionsDto(
            Classes: Array.AsReadOnly(SkillCatalogRules.InitialClasses.Select(classType => ToOption(classType.ToString(), classType.ToString())).ToArray()),
            Slots: Array.AsReadOnly(Enum.GetValues<SkillSlot>().Select(slot => ToOption(slot.ToString(), $"Slot {(int)slot:00}")).ToArray()),
            ActionTypes: Array.AsReadOnly(Enum.GetValues<SkillActionType>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            DamageTypes: Array.AsReadOnly(Enum.GetValues<SkillDamageType>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            ScalingTypes: Array.AsReadOnly(Enum.GetValues<SkillScalingType>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            TargetPatterns: Array.AsReadOnly(Enum.GetValues<SkillTargetingPattern>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            TargetAffinities: Array.AsReadOnly(Enum.GetValues<SkillTargetAffinity>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            ConditionTypes: Array.AsReadOnly(CombatConditionCatalog.GetAll().OrderBy(definition => definition.Type).Select(definition => ToOption(definition.Type.ToString(), definition.Type.ToString(), definition.Description)).ToArray()),
            ResourceTypes: Array.AsReadOnly(Enum.GetValues<CharacterResourceType>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            ProtectionTypes: Array.AsReadOnly(Enum.GetValues<CombatProtectionType>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            ProtectionBlockTypes: Array.AsReadOnly(Enum.GetValues<CombatProtectionBlockType>().Where(value => value != CombatProtectionBlockType.None).Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            ProtectionRefreshPolicies: Array.AsReadOnly(Enum.GetValues<CombatProtectionRefreshPolicy>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            TriggerPhases: Array.AsReadOnly(Enum.GetValues<SkillExecutionTriggerPhase>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            TriggerTargets: Array.AsReadOnly(Enum.GetValues<SkillTriggeredActionTargetSelector>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            HitDistributionModes: Array.AsReadOnly(Enum.GetValues<SkillHitDistributionMode>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            Elements: Array.AsReadOnly(Enum.GetValues<SkillElementType>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            CombatRoles: Array.AsReadOnly(Enum.GetValues<SkillCombatRole>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            AscensionMaterialTypes: Array.AsReadOnly(Enum.GetValues<SkillAscensionMaterialType>().Select(value => ToOption(value.ToString(), value.ToString())).ToArray()),
            ElementalMatrixRules: Array.AsReadOnly(CombatConditionInteractionCatalog.GetAllDocumentedRules()
                .Select(rule => new SkillAdminConditionInteractionRuleDto(
                    rule.Key,
                    Array.AsReadOnly(rule.RequiredConditions.Select(condition => condition.ToString()).ToArray()),
                    rule.Status.ToString(),
                    rule.FinalDamageIncreasePercentage,
                    rule.AdditionalConditionToApply?.ToString(),
                    rule.Description,
                    rule.FutureRuleNote))
                .ToArray()));
    }

    private static SkillAdminOptionItemDto ToOption(string key, string label, string? description = null)
    {
        return new SkillAdminOptionItemDto(key, label, description);
    }
}

