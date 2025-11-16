using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Extension methods for Purchasable-related classes.
/// Provides convenient, null-safe helpers that wrap common Purchase/Resource operations.
/// </summary>
public static class PurchasableExtensions
{
    /// <summary>
    /// Returns how many times this purchasable has been purchased (0 if unavailable).
    /// </summary>
    public static int GetTimesPurchased(this PurchasableBlueprint blueprint)
    {
        if (blueprint == null) return 0;
        return PurchaseManager.Instance != null ? PurchaseManager.Instance.GetTimesPurchased(blueprint.purchasableId) : 0;
    }

    /// <summary>
    /// Returns how many times the purchasable with the given id has been purchased (0 if unavailable).
    /// </summary>
    public static int GetTimesPurchased(this string purchasableId)
    {
        if (string.IsNullOrEmpty(purchasableId)) return 0;
        return PurchaseManager.Instance != null ? PurchaseManager.Instance.GetTimesPurchased(purchasableId) : 0;
    }

    /// <summary>
    /// Convenience wrapper for getting the current cost for this purchasable.
    /// Falls back to a zero cost pair if blueprint is null.
    /// </summary>
    public static ResourceAmountPair GetCurrentCostSafe(this PurchasableBlueprint blueprint)
    {
        if (blueprint == null) return new ResourceAmountPair(null, AlphabeticNotation.zero);
        return blueprint.GetCurrentCost();
    }

    /// <summary>
    /// Returns true when the player can currently afford this purchasable.
    /// Null-safe and safe across manager startup ordering.
    /// </summary>
    public static bool CanAfford(this PurchasableBlueprint blueprint)
    {
        if (blueprint == null) return false;
        ResourceAmountPair cost = blueprint.GetCurrentCost();
        ResourceManager rm = ResourceManager.Instance;
        return rm != null && rm.CanSpend(cost);
    }

    /// <summary>
    /// Executes a purchase for this blueprint.
    /// If <paramref name="removeCost"/> is true the cost will be removed via <see cref="ResourceManager"/>.
    /// Returns true on success.
    /// </summary>
    public static bool ExecutePurchase(this PurchasableBlueprint blueprint, bool removeCost = true)
    {
        if (blueprint == null) return false;

        PurchaseManager pm = PurchaseManager.Instance;
        if (pm == null) return false;

        if (removeCost)
        {
            ResourceManager rm = ResourceManager.Instance;
            if (rm == null) return false;

            ResourceAmountPair cost = blueprint.GetCurrentCost();
            if (!rm.CanSpend(cost)) return false;

            if (!rm.RemoveResource(cost))
                return false;
        }

        return pm.ExecutePurchase(blueprint.purchasableId);
    }

    /// <summary>
    /// Gets the blueprint associated with this runtime data using the registry.
    /// </summary>
    public static PurchasableBlueprint GetBlueprint(this PurchasableRuntimeData runtimeData)
    {
        if (runtimeData == null || string.IsNullOrEmpty(runtimeData.purchasableId))
            return null;

        PurchasableRegistry registry = RegistryManager.Instance.GetRegistry<PurchasableBlueprint>() as PurchasableRegistry;
        return registry?.GetItemById(runtimeData.purchasableId);
    }

    /// <summary>
    /// Maps a collection of runtime data objects to their blueprints (skips missing items).
    /// </summary>
    public static List<PurchasableBlueprint> ToBlueprints(this IEnumerable<PurchasableRuntimeData> runtimes)
    {
        List<PurchasableBlueprint> result = new List<PurchasableBlueprint>();
        if (runtimes == null) return result;

        PurchasableRegistry registry = RegistryManager.Instance.GetRegistry<PurchasableBlueprint>() as PurchasableRegistry;
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
    /// Creates runtime data objects for a collection of blueprints.
    /// This does not add them to any save provider - it just constructs objects.
    /// </summary>
    public static List<PurchasableRuntimeData> ToRuntimeData(this IEnumerable<PurchasableBlueprint> blueprints)
    {
        List<PurchasableRuntimeData> result = new List<PurchasableRuntimeData>();
        if (blueprints == null) return result;

        foreach (var bp in blueprints)
        {
            if (bp == null) continue;
            result.Add(new PurchasableRuntimeData(bp.purchasableId));
        }

        return result;
    }
}
