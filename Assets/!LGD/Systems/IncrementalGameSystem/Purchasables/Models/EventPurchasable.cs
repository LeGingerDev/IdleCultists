using UnityEngine;

/// <summary>
/// Abstract base class for purchasables that trigger custom behaviors/events
/// Replaces the old PurchasableBlueprint system
/// Subclasses must implement HandlePurchase to define their specific behavior
/// Examples: SummonPurchasable, RitualPurchasable, UnlockPurchasable, etc.
/// </summary>
public abstract class EventPurchasable : BasePurchasable
{
    // EventPurchasable is fully abstract - all behavior is defined in concrete implementations
    // Subclasses must implement:
    // - HandlePurchase(BasePurchasableRuntimeData runtimeData) - What happens when purchased
    // - GetContextId() - Context identifier for the event system

    // Examples of concrete implementations:
    // - SummonPurchasable: Spawns an entity
    // - RitualPurchasable: Triggers a ritual event
    // - UnlockPurchasable: Unlocks a new game feature
}
