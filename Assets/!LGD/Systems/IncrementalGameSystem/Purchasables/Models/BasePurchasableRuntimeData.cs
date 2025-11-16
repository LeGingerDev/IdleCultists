using System;

/// <summary>
/// Unified runtime data for all purchasable types
/// Replaces both UpgradeRuntimeData and PurchasableRuntimeData
/// </summary>
[Serializable]
public class BasePurchasableRuntimeData
{
    public string purchasableId;
    public int purchaseCount; // Unified: replaces both currentTier and timesPurchased
    public bool isActive;

    // Parameterless constructor for JSON deserialization
    public BasePurchasableRuntimeData() { }

    public BasePurchasableRuntimeData(string id)
    {
        purchasableId = id;
        purchaseCount = 0;
        isActive = false;
    }

    public void IncrementPurchase()
    {
        purchaseCount++;
        isActive = true;
    }
}
