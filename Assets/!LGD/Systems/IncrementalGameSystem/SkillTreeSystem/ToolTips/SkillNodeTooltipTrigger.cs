using LGD.Extensions;
using Sirenix.OdinInspector;
using ToolTipSystem.Components;
using UnityEngine;

/// <summary>
/// Tooltip trigger for skill nodes
/// Gets reference to SkillNodeDisplay and converts blueprint data to SkillNodeData
/// </summary>
[RequireComponent(typeof(SkillNodeDisplay))]
public class SkillNodeTooltipTrigger : ToolTipBase<SkillNodeData>
{
    [SerializeField, FoldoutGroup("References")]
    private SkillNodeDisplay _skillNodeDisplay;

    public override SkillNodeData Data => GetSkillNodeData();

    private void Reset()
    {
        // Auto-assign in editor
        _skillNodeDisplay = GetComponent<SkillNodeDisplay>();
    }

    private SkillNodeData GetSkillNodeData()
    {
        if (_skillNodeDisplay == null)
        {
            DebugManager.Warning("[SkillNodeTooltipTrigger] No SkillNodeDisplay reference!");
            return null;
        }

        BasePurchasable blueprint = _skillNodeDisplay.GetBlueprint();
        if (blueprint == null)
        {
            DebugManager.Warning("[SkillNodeTooltipTrigger] No blueprint assigned to SkillNodeDisplay!");
            return null;
        }

        // Get current state
        int currentLevel = blueprint.GetPurchaseCount();
        bool isPurchased = currentLevel > 0;
        bool isMaxedOut = blueprint.IsMaxedOut();
        bool prerequisitesMet = blueprint.ArePrerequisitesMet();
        bool canAfford = blueprint.CanAfford();

        // Determine max level
        int maxLevel = blueprint.purchaseType == PurchaseType.Infinite ? -1 :
                       blueprint.purchaseType == PurchaseType.Capped ? blueprint.maxPurchases : 1;

        // Get cost for next purchase
        var cost = blueprint.GetCurrentCostSafe();

        // Get bonus text
        string currentBonusText = GetCurrentBonusText(blueprint, currentLevel);
        string nextBonusText = GetNextBonusText(blueprint, currentLevel, isMaxedOut);

        return new SkillNodeData(
            blueprint.displayName,
            blueprint.description,
            currentLevel,
            maxLevel,
            cost,
            currentBonusText,
            nextBonusText,
            isPurchased,
            isMaxedOut,
            canAfford,
            prerequisitesMet
        );
    }

    private string GetCurrentBonusText(BasePurchasable blueprint, int currentLevel)
    {
        if (currentLevel == 0)
            return "Not purchased";

        // For StatPurchasables, show current modifiers
        if (blueprint is StatPurchasable statPurchasable)
        {
            var modifiers = statPurchasable.GetModifiersAtTier(currentLevel);
            return FormatModifiers(modifiers);
        }

        // For EventPurchasables, just show it's active
        return "Active";
    }

    private string GetNextBonusText(BasePurchasable blueprint, int currentLevel, bool isMaxedOut)
    {
        if (isMaxedOut)
            return "Maxed out";

        int nextLevel = currentLevel + 1;

        // For StatPurchasables, show next level modifiers
        if (blueprint is StatPurchasable statPurchasable)
        {
            var modifiers = statPurchasable.GetModifiersAtTier(nextLevel);
            return FormatModifiers(modifiers);
        }

        // For EventPurchasables
        return "Unlock";
    }

    private string FormatModifiers(System.Collections.Generic.List<StatModifier> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
            return "No bonuses";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (var modifier in modifiers)
        {
            string statName = FormatStatName(modifier.statType);
<<<<<<< HEAD
            string valueStr = modifier.additiveValue.FormatWithDecimals(2);
=======
>>>>>>> 374afab388731e19a5fbcba03fdc17462187ae7a

            if (modifier.modifierType == ModifierType.Additive)
            {
                string valueStr = modifier.additiveValue.FormatWithDecimals(2);
                sb.AppendLine($"+{valueStr} {statName}");
            }
            else // Multiplicative
            {
<<<<<<< HEAD
                float percentage = (float)(double)modifier.multiplicativeValue * 100f;
=======
                float percentage = modifier.multiplicativeValue * 100f;
>>>>>>> 374afab388731e19a5fbcba03fdc17462187ae7a
                sb.AppendLine($"+{percentage:F0}% {statName}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string FormatStatName(StatType statType)
    {
        // Add spaces before capital letters
        return System.Text.RegularExpressions.Regex.Replace(
            statType.ToString(),
            "(\\B[A-Z])",
            " $1"
        );
    }
}
