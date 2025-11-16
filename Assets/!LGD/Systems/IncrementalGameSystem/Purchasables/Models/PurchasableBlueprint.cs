using System.Collections.Generic;
using LargeNumbers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class PurchasableBlueprint : ScriptableObject
{
    [FoldoutGroup("Identity")]
    public string purchasableId;

    [FoldoutGroup("Identity")]
    public string displayName;

    [FoldoutGroup("Identity"), TextArea(2, 4)]
    public string description;

    [FoldoutGroup("Identity")]
    public Sprite icon;

    [FoldoutGroup("Identity")]
    public PurchasableType purchaseType;

    [FoldoutGroup("Prerequisites")]
    public List<UpgradeBlueprint> prerequisiteUpgrades = new List<UpgradeBlueprint>();

    [FoldoutGroup("Prerequisites")]
    public List<PurchasableBlueprint> prerequisitePurchasables = new List<PurchasableBlueprint>();

    [FoldoutGroup("Cost")]
    public CostScaling costScaling;

    // Abstract method - each concrete type implements its own logic
    public abstract void HandlePurchase(PurchasableRuntimeData runtimeData);

    /// <summary>
    /// Get the cost for the next purchase based on current purchase count from PurchaseManager
    /// </summary>
    public ResourceAmountPair GetCurrentCost()
    {
        int timesPurchased = PurchaseManager.Instance.GetTimesPurchased(purchasableId);
        return GetCurrentCost(timesPurchased);
    }

    /// <summary>
    /// Get the cost for a specific purchase number (manual override)
    /// </summary>
    public ResourceAmountPair GetCurrentCost(int timesPurchased)
    {
        AlphabeticNotation cost = costScaling.CalculateCost(timesPurchased + 1);
        return new ResourceAmountPair(costScaling.costType, cost);
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

    public abstract string GetContextId();
}