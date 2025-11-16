using LGD.ResourceSystem.Models;
using System.Collections.Generic;

/// <summary>
/// Use this data type when you want a detailed resource tooltip with stats
/// </summary>
public class ResourceDetailedData
{
    public Resource resource;

    // Active (Clicking) Stats
    public List<StatType> activeStats;
    public StatType activeTotalStat; // The main "per click" stat

    // Passive Stats
    public List<StatType> passiveStats;
    public StatType passiveTotalStat; // The main "per second" stat

    public ResourceDetailedData(
        Resource resource,
        List<StatType> activeStats,
        StatType activeTotalStat,
        List<StatType> passiveStats,
        StatType passiveTotalStat)
    {
        this.resource = resource;
        this.activeStats = activeStats ?? new List<StatType>();
        this.activeTotalStat = activeTotalStat;
        this.passiveStats = passiveStats ?? new List<StatType>();
        this.passiveTotalStat = passiveTotalStat;
    }
}
