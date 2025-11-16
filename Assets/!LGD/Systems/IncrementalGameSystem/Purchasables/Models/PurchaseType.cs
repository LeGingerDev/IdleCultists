/// <summary>
/// Defines how many times a purchasable can be bought
/// </summary>
public enum PurchaseType
{
    OneTime,    // Can only buy once
    Capped,     // Can buy up to maxPurchases
    Infinite    // Can buy forever
}
