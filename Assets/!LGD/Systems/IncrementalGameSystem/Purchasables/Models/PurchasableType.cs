/// <summary>
/// Change this when making a new game. Yes I've baked it into the PurchasableBlueprint but only as a sort of marker. This will change game to game.
/// </summary>
public enum PurchasableType
{
    TomeOfSummoning,   // Spawns an entity after timer
    BuyUpgrade,     // Applies upgrade modifiers
    PerformRitual,  // Triggers one-time effect
    UnlockFeature   // Unlocks new game system
}