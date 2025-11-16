using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unified extension methods for all purchasables (both StatPurchasables and EventPurchasables)
/// Replaces both UpgradeExtensions and PurchasableExtensions
/// Provides convenient, null-safe helpers that wrap common purchase/resource operations
/// </summary>
public static class UnifiedPurchasableExtensions
{
    #region Purchase Count
    /// <summary>
    /// Returns how many times this purchasable has been purchased (0 if unavailable)
    /// </summary>
    public static int GetPurchaseCount(this BasePurchasable blueprint)
    {
        if (blueprint == null) return 0;
        return UnifiedPurchasableManager.Instance != null
            ? UnifiedPurchasableManager.Instance.GetPurchaseCount(blueprint.purchasableId)
            : 0;
    }

    /// <summary>
    /// Returns how many times the purchasable with the given id has been purchased (0 if unavailable)
    /// </summary>
    public static int GetPurchaseCount(this string purchasableId)
    {
        if (string.IsNullOrEmpty(purchasableId)) return 0;
        return UnifiedPurchasableManager.Instance != null
            ? UnifiedPurchasableManager.Instance.GetPurchaseCount(purchasableId)
            : 0;
    }

    /// <summary>
    /// Get the next purchase index (current count + 1)
    /// </summary>
    public static int GetNextPurchaseIndex(this BasePurchasable blueprint)
    {
        if (blueprint == null) return 1;
        return blueprint.GetPurchaseCount() + 1;
    }
    #endregion

    #region Cost Calculation
    /// <summary>
    /// Get the cost for a specific purchase number (safe, returns zero if null)
    /// </summary>
    public static ResourceAmountPair GetCostForPurchaseSafe(this BasePurchasable blueprint, int purchaseNumber)
    {
        if (blueprint == null) return new ResourceAmountPair(null, AlphabeticNotation.zero);
        return blueprint.GetCostWithResourceForPurchase(purchaseNumber);
    }

    /// <summary>
    /// Get the cost for the next purchase
    /// </summary>
    public static ResourceAmountPair GetCostForNextPurchase(this BasePurchasable blueprint)
    {
        if (blueprint == null) return new ResourceAmountPair(null, AlphabeticNotation.zero);
        int nextPurchase = blueprint.GetNextPurchaseIndex();
        return blueprint.GetCostWithResourceForPurchase(nextPurchase);
    }

    /// <summary>
    /// Convenience wrapper for getting current cost (safe, returns zero if null)
    /// </summary>
    public static ResourceAmountPair GetCurrentCostSafe(this BasePurchasable blueprint)
    {
        return blueprint.GetCostForNextPurchase();
    }
    #endregion

    #region Affordability
    /// <summary>
    /// Returns true when the player can currently afford the next purchase
    /// Null-safe and safe across manager startup ordering
    /// </summary>
    public static bool CanAfford(this BasePurchasable blueprint)
    {
        if (blueprint == null) return false;
        ResourceAmountPair cost = blueprint.GetCostForNextPurchase();
        ResourceManager rm = ResourceManager.Instance;
        return rm != null && rm.CanSpend(cost);
    }
    #endregion

    #region Purchase Limits
    /// <summary>
    /// Check if this purchasable is maxed out (can't purchase more)
    /// </summary>
    public static bool IsMaxedOut(this BasePurchasable blueprint)
    {
        if (blueprint == null) return true;

        if (blueprint.purchaseType == PurchaseType.Infinite)
            return false;

        if (blueprint.purchaseType == PurchaseType.OneTime)
            return blueprint.GetPurchaseCount() >= 1;

        // Capped
        int current = blueprint.GetPurchaseCount();
        return blueprint.maxPurchases != -1 && current >= blueprint.maxPurchases;
    }

    /// <summary>
    /// Check if we can purchase more of this purchasable
    /// </summary>
    public static bool CanPurchaseMore(this BasePurchasable blueprint)
    {
        return blueprint != null && !blueprint.IsMaxedOut();
    }
    #endregion

    #region Purchase Execution
    /// <summary>
    /// Executes a purchase for this blueprint
    /// If removeCost is true, the cost will be removed via ResourceManager
    /// Returns true on success
    /// </summary>
    public static bool ExecutePurchase(this BasePurchasable blueprint, bool removeCost = true)
    {
        if (blueprint == null) return false;

        UnifiedPurchasableManager pm = UnifiedPurchasableManager.Instance;
        if (pm == null) return false;

        // Check if can purchase more
        if (blueprint.IsMaxedOut())
        {
            DebugManager.Warning($"[IncrementalGame] Cannot purchase {blueprint.displayName} - already maxed out");
            return false;
        }

        if (removeCost)
        {
            ResourceManager rm = ResourceManager.Instance;
            if (rm == null) return false;

            ResourceAmountPair cost = blueprint.GetCostForNextPurchase();
            if (!rm.CanSpend(cost)) return false;

            if (!rm.RemoveResource(cost))
                return false;
        }

        return pm.ExecutePurchase(blueprint.purchasableId);
    }
    #endregion

    #region Blueprint/Runtime Data Conversion
    /// <summary>
    /// Gets the blueprint associated with this runtime data using the registry
    /// </summary>
    public static BasePurchasable GetBlueprint(this BasePurchasableRuntimeData runtimeData)
    {
        if (runtimeData == null || string.IsNullOrEmpty(runtimeData.purchasableId))
            return null;

        UnifiedPurchasableRegistry registry = RegistryManager.Instance.GetRegistry<BasePurchasable>() as UnifiedPurchasableRegistry;
        return registry?.GetItemById(runtimeData.purchasableId);
    }

    /// <summary>
    /// Maps a collection of runtime data objects to their blueprints (skips missing items)
    /// </summary>
    public static List<BasePurchasable> ToBlueprints(this IEnumerable<BasePurchasableRuntimeData> runtimes)
    {
        List<BasePurchasable> result = new List<BasePurchasable>();
        if (runtimes == null) return result;

        UnifiedPurchasableRegistry registry = RegistryManager.Instance.GetRegistry<BasePurchasable>() as UnifiedPurchasableRegistry;
        if (registry == null) return result;

        foreach (var runtime in runtimes)
        {
            if (runtime == null) continue;
            var bp = registry.GetItemById(runtime.purchasableId);
            if (bp != null) result.Add(bp);
        }

        return result;
    }

    /// <summary>
    /// Creates runtime data objects for a collection of blueprints
    /// This does not add them to any save provider - it just constructs objects
    /// </summary>
    public static List<BasePurchasableRuntimeData> ToRuntimeData(this IEnumerable<BasePurchasable> blueprints)
    {
        List<BasePurchasableRuntimeData> result = new List<BasePurchasableRuntimeData>();
        if (blueprints == null) return result;

        foreach (var bp in blueprints)
        {
            if (bp == null) continue;
            result.Add(new BasePurchasableRuntimeData(bp.purchasableId));
        }

        return result;
    }
    #endregion

    #region StatPurchasable-Specific Extensions
    /// <summary>
    /// Get stat modifiers at current purchase count (only for StatPurchasables)
    /// </summary>
    public static List<StatModifier> GetCurrentModifiers(this StatPurchasable statPurchasable)
    {
        if (statPurchasable == null) return new List<StatModifier>();
        int currentCount = statPurchasable.GetPurchaseCount();
        return statPurchasable.GetModifiersAtTier(currentCount);
    }

    /// <summary>
    /// Get stat modifiers for the next purchase tier (only for StatPurchasables)
    /// </summary>
    public static List<StatModifier> GetNextModifiers(this StatPurchasable statPurchasable)
    {
        if (statPurchasable == null) return new List<StatModifier>();
        int nextCount = statPurchasable.GetNextPurchaseIndex();
        return statPurchasable.GetModifiersAtTier(nextCount);
    }
    #endregion
}
