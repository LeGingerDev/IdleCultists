using LargeNumbers;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonPurchasable_[NAME]", menuName = "LGD/Idle Cultist/Purchasable/Summon Entity")]
public class SummonPurchasableBlueprint : PurchasableBlueprint
{
    [FoldoutGroup("Summon Data")]
    public EntityBlueprint entityToSummon;

    [FoldoutGroup("Summon Data")]
    public string gainsDescription;

    [FoldoutGroup("Full Preview"), ShowInInspector, HideLabel, TextArea(15, 30), ReadOnly]
    private string _fullPreview = "Click 'Preview Purchasable' to see cost breakdown";

    public override void HandlePurchase(PurchasableRuntimeData runtimeData)
    {
        // TODO: Replace with Topic System
        ServiceBus.Publish(PurchasableEventIds.ON_SUMMON_ENTITY_PURCHASED, this, entityToSummon, runtimeData);
    }

    public override string GetContextId()
    {
        return entityToSummon.id;
    }

    public AlphabeticNotation GetEntityCapacityAmount() => entityToSummon.GetBaseStat(StatType.CapacityCount);

    public AlphabeticNotation GetSummonTime()
    {
        return StatManager.Instance.QueryStatWithBase(StatType.SummoningTime, entityToSummon.GetBaseStat(StatType.SummoningTime));
    }

#if UNITY_EDITOR
    [Button("Preview Purchasable (20 Purchases)"), FoldoutGroup("Full Preview"), PropertyOrder(100)]
    private void PreviewPurchasable()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {displayName.ToUpper()} - SUMMON PURCHASABLE PREVIEW ===\n");
        sb.AppendLine($"Description: {description}");
        sb.AppendLine();

        if (entityToSummon != null)
        {
            sb.AppendLine($"Summons: {entityToSummon.displayName}");
            sb.AppendLine($"Base Summoning Time: {entityToSummon.GetBaseStat(StatType.SummoningTime)}s");
            sb.AppendLine();
        }

        AlphabeticNotation totalCost = AlphabeticNotation.zero;

        for (int purchase = 1; purchase <= 20; purchase++)
        {
            AlphabeticNotation cost = costScaling.CalculateCost(purchase);
            totalCost += cost;

            sb.AppendLine($"Purchase {purchase,2}: {cost.ToString(),15}  (Total spent: {totalCost.ToString()})");
        }

        _fullPreview = sb.ToString();
        Debug.Log(_fullPreview);
    }

    [Button("Rename Asset to Match Purchasable Name"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("Display name is empty. Cannot rename asset.");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string newName = $"SummonPurchasable_{displayName}";
        string result = UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);

        if (string.IsNullOrEmpty(result))
        {
            Debug.Log($"Successfully renamed asset to: {newName}");
        }
        else
        {
            Debug.LogError($"Failed to rename asset: {result}");
        }
    }
#endif
}