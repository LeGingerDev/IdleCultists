using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using TMPro;
using ToolTipSystem.Components;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Basic resource tooltip - just shows icon and name
/// </summary>
public class ResourceSimpleTooltip : ToolTip<Resource>
{
    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _resourceNameText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _resourceDescriptionText;

    public override void Show(Resource data)
    {
        if (data == null)
            return;

        if (_resourceNameText != null)
            _resourceNameText.text = data.displayName;

        if (_resourceDescriptionText != null)
            _resourceDescriptionText.text = data.description;
    }
}
