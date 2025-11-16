using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using ToolTipSystem.Components;
using UnityEngine;
/// <summary>
/// Triggers detailed resource tooltip with stats
/// </summary>
public class ResourceDetailedTooltipTrigger : ToolTipBase<ResourceDetailedData>
{
    [SerializeField, FoldoutGroup("Tooltip Settings/Resource")]
    private Resource _resource;

    [SerializeField, FoldoutGroup("Tooltip Settings/Active (Clicking)")]
    private List<StatType> _activeStats = new List<StatType>();

    [SerializeField, FoldoutGroup("Tooltip Settings/Active (Clicking)")]
    private StatType _activeTotalStat = StatType.DevotionPerClick;

    [SerializeField, FoldoutGroup("Tooltip Settings/Passive (Generation)")]
    private List<StatType> _passiveStats = new List<StatType>();

    [SerializeField, FoldoutGroup("Tooltip Settings/Passive (Generation)")]
    private StatType _passiveTotalStat = StatType.DevotionPerSecond;

    public override ResourceDetailedData Data => GetDetailedData();

    private ResourceDetailedData GetDetailedData()
    {
        if (_resource == null)
            return null;

        return new ResourceDetailedData(
            _resource,
            _activeStats,
            _activeTotalStat,
            _passiveStats,
            _passiveTotalStat
        );
    }
}
