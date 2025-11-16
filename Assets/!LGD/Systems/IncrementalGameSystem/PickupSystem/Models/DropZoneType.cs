using System;
/// <summary>
/// These can be changed per game past the None section
/// </summary>
[Flags]
public enum DropZoneType
{
    None = 0,
    Devotion = 1 << 0,    // Prayer areas
    LifeCrystal = 1 << 1,   // Altars for rituals
    Work = 1 << 2,        // Crafting stations
    Sleep = 1 << 3,       // Rest areas
    Ritual = 1 << 4,      // Special ceremony zones
    Idle = 1 << 5,        // Storage/waiting areas (no special behavior)
    Any = ~0              // Accepts any entity type
}