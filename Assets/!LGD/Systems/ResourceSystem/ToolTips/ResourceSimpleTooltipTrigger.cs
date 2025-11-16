using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using ToolTipSystem.Components;
using UnityEngine;
/// <summary>
/// Triggers simple resource tooltip
/// </summary>
public class ResourceSimpleTooltipTrigger : ToolTipBase<Resource>
{
    [SerializeField, FoldoutGroup("Tooltip Settings")]
    private Resource _resource;

    public override Resource Data => _resource;
}
