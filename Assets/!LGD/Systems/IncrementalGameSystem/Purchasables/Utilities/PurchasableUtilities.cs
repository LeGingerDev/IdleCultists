using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility wrappers for quick Purchasable/Registry/Manager access.
/// Keeps common one-liners in a single place to reduce repetition.
/// </summary>
public static class PurchasableUtilities
{
    /// <summary>
    /// Gets a blueprint by id from the PurchasableRegistry (null-safe).
    /// </summary>
    public static PurchasableBlueprint GetBlueprintById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        PurchasableRegistry registry = RegistryManager.Instance.GetRegistry<PurchasableBlueprint>() as PurchasableRegistry;
        return registry?.GetItemById(id);
    }

    /// <summary>
    /// Gets the runtime data for a purchasable id (null-safe).
    /// </summary>
    public static PurchasableRuntimeData GetRuntimeData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return PurchaseManager.Instance != null ? PurchaseManager.Instance.GetPurchasableRuntimeData(id) : null;
    }

    /// <summary>
    /// Gets all registered purchasable blueprints (returns empty list if registry missing).
    /// </summary>
    public static List<PurchasableBlueprint> GetAllBlueprints()
    {
        PurchasableRegistry registry = RegistryManager.Instance.GetRegistry<PurchasableBlueprint>() as PurchasableRegistry;
        return registry != null ? registry.GetAllItems() : new List<PurchasableBlueprint>();
    }

    /// <summary>
    /// Executes a purchase by id, optionally removing cost. Null-safe wrapper.
    /// </summary>
    public static bool ExecutePurchaseById(string purchasableId, bool removeCost = true)
    {
        if (string.IsNullOrEmpty(purchasableId)) return false;

        PurchasableBlueprint bp = GetBlueprintById(purchasableId);
        if (bp == null) return false;

        return bp.ExecutePurchase(removeCost);
    }
}
