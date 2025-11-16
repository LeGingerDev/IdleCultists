/// <summary>
/// All possible bark contexts.
/// Reactive = triggered once by event
/// Ambient = triggered periodically while in state
/// </summary>
public enum BarkContext
{
    // ============ REACTIVE BARKS ============
    // Triggered once when action happens

    // Global Actions
    PickedUp,           // When cultist is picked up
    DroppedInvalid,     // When dropped on invalid location
    Spawned,            // When cultist spawns

    // Zone Assignment (happens once on drop)
    AssignedToDevotion,
    AssignedToLifeCrystal,
    AssignedToWork,
    AssignedToSleep,

    // Special Events
    Falling,            // When falling into pit
    Sacrificed,         // When sacrifice completes

    // ============ AMBIENT BARKS ============
    // Triggered periodically (every 20-40 seconds)

    DevotingAmbient,    // Random barks while praying at devotion zone
    LifeCrystalAmbient,   // Nervous barks while waiting at altar
    WorkAmbient,        // Work-related mutterings
    IdleAmbient,        // Generic idle/bored barks

    // Reactions
    DoubleDevotionProc,
    TripleDevotionProc,
}
