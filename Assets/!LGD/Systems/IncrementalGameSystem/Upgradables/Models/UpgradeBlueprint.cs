using LargeNumbers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeBlueprint_[NAME]", menuName = "LGD/Incremental/Upgrade/Create Blueprint")]
public class UpgradeBlueprint : ScriptableObject
{
    [FoldoutGroup("Identity")]
    public string upgradeId;

    [FoldoutGroup("Identity")]
    public string displayName;

    [FoldoutGroup("Identity"), TextArea(2, 4)]
    public string description;

    [FoldoutGroup("Identity")]
    public Sprite icon;

    [FoldoutGroup("Upgrade Type")]
    public UpgradeType upgradeType;

    [FoldoutGroup("Upgrade Type"), ShowIf("@upgradeType != UpgradeType.Infinite")]
    public int maxTier = -1;

    [FoldoutGroup("Cost Scaling")]
    public CostScaling costScaling;

    [FoldoutGroup("Modifier Scaling")]
    public List<ModifierScaling> modifierScaling = new List<ModifierScaling>();

    [FoldoutGroup("Prerequisites")]
    public List<UpgradeBlueprint> prerequisiteUpgrades = new List<UpgradeBlueprint>();

    [FoldoutGroup("Prerequisites")]
    public List<PurchasableBlueprint> prerequisitePurchasables = new List<PurchasableBlueprint>();

    [FoldoutGroup("Full Preview"), ShowInInspector, HideLabel, TextArea(15, 30), ReadOnly]
    private string _fullPreview = "Click 'Preview Full Upgrade' to see complete breakdown";

    public AlphabeticNotation GetCostForTier(int tier)
    {
        return costScaling.CalculateCost(tier);
    }

    public ResourceAmountPair GetCostWithResourceForTier(int tier)
    {
        return costScaling.CalculateCostWithResource(tier);
    }

    public List<StatModifier> GetModifiersAtTier(int tier)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        foreach (var scaling in modifierScaling)
        {
            if (scaling.modifierType == ModifierType.Additive)
            {
                // Additive: use AlphabeticNotation
                AlphabeticNotation value = scaling.CalculateAdditiveValue(tier);
                modifiers.Add(new StatModifier(scaling.statType, value, upgradeId));
            }
            else // Multiplicative
            {
                // Multiplicative: use float for percentages
                float value = scaling.CalculateMultiplicativeValue(tier);
                modifiers.Add(new StatModifier(scaling.statType, value, upgradeId));
            }
        }

        return modifiers;
    }

    public bool ArePrerequisitesMet()
    {
        // Check upgrade prerequisites
        foreach (var upgrade in prerequisiteUpgrades)
        {
            if (upgrade != null && upgrade.GetCurrentTier() == 0)
                return false;
        }

        // Check purchasable prerequisites
        foreach (var purchasable in prerequisitePurchasables)
        {
            if (purchasable != null && purchasable.GetTimesPurchased() == 0)
                return false;
        }

        return true;
    }


#if UNITY_EDITOR
    [Button("Preview Full Upgrade (20 Tiers)"), FoldoutGroup("Full Preview"), PropertyOrder(100)]
    private void PreviewFullUpgrade()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {displayName.ToUpper()} - FULL PREVIEW ===\n");
        sb.AppendLine($"Type: {upgradeType}");
        sb.AppendLine($"Max Tier: {(maxTier == -1 ? "Infinite" : maxTier.ToString())}");
        sb.AppendLine();

        AlphabeticNotation totalCost = AlphabeticNotation.zero;
        int previewTiers = upgradeType == UpgradeType.OneTime ? 1 : 20;

        for (int tier = 1; tier <= previewTiers; tier++)
        {
            AlphabeticNotation cost = GetCostForTier(tier);
            totalCost += cost;

            sb.AppendLine($"--- TIER {tier} ---");
            sb.AppendLine($"Cost: {cost} (Total spent: {totalCost})");

            List<StatModifier> mods = GetModifiersAtTier(tier);
            foreach (var mod in mods)
            {
                string displayValue = mod.modifierType == ModifierType.Multiplicative
                    ? $"+{mod.multiplicativeValue * 100:F1}%"
                    : $"+{mod.additiveValue}";

                sb.AppendLine($"  � {mod.statType}: {displayValue} ({mod.modifierType})");
            }
            sb.AppendLine();
        }

        _fullPreview = sb.ToString();
        DebugManager.Log($"[IncrementalGame] {_fullPreview}");
    }

    [Button("Rename Asset to Match Upgrade Name"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            DebugManager.Warning("[IncrementalGame] Display name is empty. Cannot rename asset.");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string newName = $"UpgradeBlueprint_{displayName}";
        string result = UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);

        if (string.IsNullOrEmpty(result))
        {
            DebugManager.Log($"[IncrementalGame] Successfully renamed asset to: {newName}");
        }
        else
        {
            DebugManager.Error($"[IncrementalGame] Failed to rename asset: {result}");
        }
    }
#endif
}