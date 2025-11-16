using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Unified registry for all purchasables (both StatPurchasables and EventPurchasables)
/// Replaces both UpgradeRegistry and PurchasableRegistry
/// </summary>
public class UnifiedPurchasableRegistry : RegistryProviderBase<BasePurchasable>
{
    public override BasePurchasable GetItemById(string id)
    {
        return _items.Find(item => item.purchasableId == id);
    }

    /// <summary>
    /// Get all purchasables of a specific purchase type (OneTime, Capped, Infinite)
    /// </summary>
    public List<BasePurchasable> GetAllPurchasablesByPurchaseType(PurchaseType type)
    {
        return _items.Where(i => i.purchaseType == type).ToList();
    }

    /// <summary>
    /// Get all StatPurchasables (provides stat modifiers)
    /// </summary>
    public List<StatPurchasable> GetAllStatPurchasables()
    {
        return _items.OfType<StatPurchasable>().ToList();
    }

    /// <summary>
    /// Get all EventPurchasables (triggers custom behaviors)
    /// </summary>
    public List<EventPurchasable> GetAllEventPurchasables()
    {
        return _items.OfType<EventPurchasable>().ToList();
    }
}
