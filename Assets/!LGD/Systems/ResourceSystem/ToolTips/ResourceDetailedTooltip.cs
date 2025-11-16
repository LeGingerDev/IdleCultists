using LargeNumbers;
using LGD.Extensions;
using Sirenix.OdinInspector;
using TMPro;
using ToolTipSystem.Components;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detailed resource tooltip - shows icon, name, current amount, and stats
/// </summary>
public class ResourceDetailedTooltip : ToolTip<ResourceDetailedData>
{
    [FoldoutGroup("UI References")]
    [SerializeField, FoldoutGroup("UI References/Header")]
    private Image _resourceIcon;

    [SerializeField, FoldoutGroup("UI References/Header")]
    private TextMeshProUGUI _resourceNameText;

    [SerializeField, FoldoutGroup("UI References/Header")]
    private TextMeshProUGUI _descriptionText;

    [SerializeField, FoldoutGroup("UI References/Active Section")]
    private TextMeshProUGUI _activeHeaderText;

    [SerializeField, FoldoutGroup("UI References/Active Section")]
    private TextMeshProUGUI _activeStatsText;

    [SerializeField, FoldoutGroup("UI References/Active Section")]
    private TextMeshProUGUI _activeTotalText;

    [SerializeField, FoldoutGroup("UI References/Passive Section")]
    private TextMeshProUGUI _passiveHeaderText;

    [SerializeField, FoldoutGroup("UI References/Passive Section")]
    private TextMeshProUGUI _passiveStatsText;

    [SerializeField, FoldoutGroup("UI References/Passive Section")]
    private TextMeshProUGUI _passiveTotalText;

    public override void Show(ResourceDetailedData data)
    {
        if (data == null || data.resource == null)
            return;

        UpdateHeader(data);
        UpdateActiveSection(data);
        UpdatePassiveSection(data);
    }

    private void UpdateHeader(ResourceDetailedData data)
    {
        if (_resourceIcon != null)
            _resourceIcon.sprite = data.resource.icon;

        if (_resourceNameText != null)
            _resourceNameText.text = data.resource.displayName;

        if (_descriptionText != null)
            _descriptionText.text = data.resource.description;
    }

    private void UpdateActiveSection(ResourceDetailedData data)
    {
        if (_activeHeaderText != null)
            _activeHeaderText.text = "Active";

        if (_activeStatsText != null)
            _activeStatsText.text = BuildStatsListWithBreakdown(data.activeStats);

        if (_activeTotalText != null)
        {
            StatBreakdown breakdown = GetStatBreakdown(data.activeTotalStat);
            _activeTotalText.text = $"Total: {breakdown.finalValue.FormatWithDecimals(2)} per click";
        }
    }

    private void UpdatePassiveSection(ResourceDetailedData data)
    {
        if (_passiveHeaderText != null)
            _passiveHeaderText.text = "Passive";

        if (_passiveStatsText != null)
            _passiveStatsText.text = BuildStatsListWithBreakdown(data.passiveStats);

        if (_passiveTotalText != null)
        {
            StatBreakdown breakdown = GetStatBreakdown(data.passiveTotalStat);
            _passiveTotalText.text = $"Total: {breakdown.finalValue.FormatWithDecimals(2)} per second";
        }
    }

    private string BuildStatsListWithBreakdown(System.Collections.Generic.List<StatType> stats)
    {
        if (stats == null || stats.Count == 0)
            return string.Empty;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (StatType statType in stats)
        {
            StatBreakdown breakdown = GetStatBreakdown(statType);

            // Skip stats that are currently zero
            if (breakdown.finalValue.isZero)
                continue;

            string formattedName = FormatStatName(statType);

            // Show base + additive
            AlphabeticNotation additiveSum = breakdown.baseValue + breakdown.additiveTotal;
            sb.AppendLine($"{formattedName}: {additiveSum.FormatWithDecimals(2)}");

            // Show multiplicative if it exists
            if (breakdown.HasMultiplicativeModifiers)
            {
                float percentageBonus = breakdown.multiplicativeTotal * 100f;
                sb.AppendLine($"{formattedName}: +{percentageBonus:F0}%");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private StatBreakdown GetStatBreakdown(StatType statType)
    {
        return StatManager.Instance.GetStatBreakdown(statType);
    }

    private string FormatStatName(StatType statType)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            statType.ToString(),
            "(\\B[A-Z])",
            " $1"
        );
    }
}