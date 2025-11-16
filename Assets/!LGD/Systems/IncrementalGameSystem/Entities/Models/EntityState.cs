/// <summary>
/// This can be changed per game past the assigned section. 
/// The pickup system doesn't NEED to be used. it can though should you want.
/// </summary>
public enum EntityState
{
    Idle,        // Spawned but not assigned to any zone
    InTransit,   // Being dragged by player
    Assigned,    // Working in a zone, contributing stats
    Sacrificing, // On altar, being consumed
    Sleeping     // In rest area (future use)
}
