using System.Collections.Generic;
using LargeNumbers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Base class for all purchasable items in the incremental game system.
/// Consolidates the purchasing logic from both UpgradeBlueprint and PurchasableBlueprint.
/// Subclasses: StatPurchasable (for stat modifiers) and EventPurchasable (for custom behaviors)
/// </summary>
public abstract class BasePurchasable : ScriptableObject
{
    #region Identity
    [FoldoutGroup("Identity")]
    public string purchasableId;

    [FoldoutGroup("Identity")]
    public string displayName;

    [FoldoutGroup("Identity"), TextArea(2, 4)]
    public string description;

    [FoldoutGroup("Identity")]
    public Sprite icon;
    #endregion

    #region Purchase Configuration
    [FoldoutGroup("Purchase Configuration")]
    public PurchaseType purchaseType;

    [FoldoutGroup("Purchase Configuration"), ShowIf("@purchaseType == PurchaseType.Capped")]
    [Tooltip("Maximum number of times this can be purchased (only for Capped type)")]
    public int maxPurchases = -1;
    #endregion

    #region Cost
    [FoldoutGroup("Cost")]
    public CostScaling costScaling;
    #endregion

    #region Prerequisites
    [FoldoutGroup("Prerequisites")]
    [Tooltip("Other purchasables that must be bought at least once before this unlocks")]
    public List<BasePurchasable> prerequisitePurchasables = new List<BasePurchasable>();
    #endregion

    #region Cost Calculation
    /// <summary>
    /// Get the cost for a specific purchase number
    /// </summary>
    public AlphabeticNotation GetCostForPurchase(int purchaseNumber)
    {
        return costScaling.CalculateCost(purchaseNumber);
    }

    /// <summary>
    /// Get the cost with resource type for a specific purchase number
    /// </summary>
    public ResourceAmountPair GetCostWithResourceForPurchase(int purchaseNumber)
    {
        return costScaling.CalculateCostWithResource(purchaseNumber);
    }
    #endregion

    #region Prerequisites
    /// <summary>
    /// Check if all prerequisites are met
    /// </summary>
    public bool ArePrerequisitesMet()
    {
        foreach (var prerequisite in prerequisitePurchasables)
        {
            if (prerequisite != null && prerequisite.GetPurchaseCount() == 0)
                return false;
        }

        return true;
    }
    #endregion

    #region Abstract Methods
    /// <summary>
    /// Called when purchase is successfully completed
    /// Override in subclasses to define custom behavior (e.g., apply stat modifiers, trigger events)
    /// </summary>
    public abstract void HandlePurchase(BasePurchasableRuntimeData runtimeData);

    /// <summary>
    /// Get context identifier for this purchasable (used for event-based purchasables)
    /// For StatPurchasables, this typically returns the purchasableId
    /// For EventPurchasables, this might return a specific context (e.g., "SummoningSystem")
    /// </summary>
    public abstract string GetContextId();
    #endregion

    #region Editor Utilities
#if UNITY_EDITOR
    [Button("Rename Asset to Match Display Name"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            DebugManager.Warning("[IncrementalGame] Display name is empty. Cannot rename asset.");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string prefix = GetType().Name.Replace("Purchasable", ""); // e.g., "Stat" or "Event"
        string newName = $"{prefix}Purchasable_{displayName}";
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
    #endregion

    #region Runtime Data Access (Extension Point)
    /// <summary>
    /// Get the current purchase count for this purchasable
    /// This is overridden by extension methods to access the manager
    /// </summary>
    public virtual int GetPurchaseCount()
    {
        // This will be implemented via extension methods to avoid circular dependency
        return 0;
    }
    #endregion
}
