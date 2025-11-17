using System.Collections.Generic;
using LargeNumbers;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Purchasable that provides stat modifiers when purchased
/// Replaces the old UpgradeBlueprint system
/// Each purchase increases the tier and applies stronger modifiers
/// </summary>
[CreateAssetMenu(fileName = "StatPurchasable_[NAME]", menuName = "LGD/Incremental/Purchasables/Stat Purchasable")]
public class StatPurchasable : BasePurchasable
{
    #region Stat Modifiers
    [FoldoutGroup("Stat Modifiers")]
    [Tooltip("Define how stat modifiers scale with each purchase/tier")]
    public List<ModifierScaling> modifierScaling = new List<ModifierScaling>();
    #endregion

    #region Preview
    [FoldoutGroup("Full Preview"), ShowInInspector, HideLabel, TextArea(15, 30), ReadOnly]
    private string _fullPreview = "Click 'Preview Full Progression' to see complete breakdown";
    #endregion

    #region Stat Modifier Calculation
    /// <summary>
    /// Get all stat modifiers at a specific purchase tier
    /// </summary>
    public List<StatModifier> GetModifiersAtTier(int tier)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        foreach (var scaling in modifierScaling)
        {
            if (scaling.modifierType == ModifierType.Additive)
            {
                // Additive: use AlphabeticNotation
                AlphabeticNotation value = scaling.CalculateAdditiveValue(tier);
                modifiers.Add(new StatModifier(scaling.statType, value, purchasableId));
            }
            else // Multiplicative
            {
                // Multiplicative: use float for percentages
                float value = scaling.CalculateMultiplicativeValue(tier);
                modifiers.Add(new StatModifier(scaling.statType, value, purchasableId));
            }
        }

        return modifiers;
    }
    #endregion

    #region Abstract Implementation
    public override void HandlePurchase(BasePurchasableRuntimeData runtimeData)
    {
        // StatPurchasables automatically apply their modifiers through IStatProvider
        // No additional handling needed here
        // The stat system will query GetModifiersAtTier when recalculating stats
    }

    public override string GetContextId()
    {
        // For stat purchasables, the context is the purchasable ID itself
        return purchasableId;
    }
    #endregion

    #region Editor Utilities
#if UNITY_EDITOR
    [Button("Preview Full Progression (20 Tiers)"), FoldoutGroup("Full Preview"), PropertyOrder(100)]
    private void PreviewFullProgression()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {displayName.ToUpper()} - FULL PREVIEW ===\n");
        sb.AppendLine($"Type: {purchaseType}");
        sb.AppendLine($"Max Purchases: {(maxPurchases == -1 ? "Infinite" : maxPurchases.ToString())}");
        sb.AppendLine();

        AlphabeticNotation totalCost = AlphabeticNotation.zero;
        int previewTiers = purchaseType == PurchaseType.OneTime ? 1 : 20;

        for (int tier = 1; tier <= previewTiers; tier++)
        {
            AlphabeticNotation cost = GetCostForPurchase(tier);
            totalCost += cost;

            sb.AppendLine($"--- PURCHASE {tier} ---");
            sb.AppendLine($"Cost: {cost} (Total spent: {totalCost})");

            List<StatModifier> mods = GetModifiersAtTier(tier);
            foreach (var mod in mods)
            {
                string displayValue = mod.modifierType == ModifierType.Multiplicative
                    ? $"+{mod.multiplicativeValue * 100:F1}%"
                    : $"+{mod.additiveValue}";

                sb.AppendLine($"  • {mod.statType}: {displayValue} ({mod.modifierType})");
            }
            sb.AppendLine();
        }

        _fullPreview = sb.ToString();
        DebugManager.Log($"[IncrementalGame] {_fullPreview}");
    }
#endif
    #endregion
}
