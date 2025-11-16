using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PurchasableRegistry : RegistryProviderBase<PurchasableBlueprint>
{
    public override PurchasableBlueprint GetItemById(string id)
    {
        return _items.Find(item => item.purchasableId == id);
    }

    public List<PurchasableBlueprint> GetAllPurchasablesByType(PurchasableType type) => _items.Where(i => i.purchaseType == type).ToList(); 
}
