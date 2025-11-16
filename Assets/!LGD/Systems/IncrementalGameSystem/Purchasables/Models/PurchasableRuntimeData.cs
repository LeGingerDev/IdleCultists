using System;
[Serializable]
public class PurchasableRuntimeData
{
    public string purchasableId;
    public int timesPurchased;

    // Parameterless constructor for JSON deserialization
    public PurchasableRuntimeData() { }

    public PurchasableRuntimeData(string id)
    {
        purchasableId = id;
        timesPurchased = 0;
    }

    public void IncrementPurchase()
    {
        timesPurchased++;
    }
}