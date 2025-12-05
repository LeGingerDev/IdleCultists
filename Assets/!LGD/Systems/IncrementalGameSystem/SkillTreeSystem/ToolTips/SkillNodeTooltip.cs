using LGD.Extensions;
using Sirenix.OdinInspector;
using TMPro;
using ToolTipSystem.Components;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tooltip visualizer for skill nodes
/// Displays skill information including name, description, level, cost, and bonuses
/// </summary>
public class SkillNodeTooltip : ToolTip<SkillNodeData>
{
    [FoldoutGroup("UI References")]
    [FoldoutGroup("UI References/Header")]
    [SerializeField] private TextMeshProUGUI _skillNameText;

    [FoldoutGroup("UI References/Header")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [FoldoutGroup("UI References/Level")]
    [SerializeField] private TextMeshProUGUI _currentLevelText;

    [FoldoutGroup("UI References/Bonuses")]
    [SerializeField] private TextMeshProUGUI _currentBonusText;

    [FoldoutGroup("UI References/Bonuses")]
    [SerializeField] private TextMeshProUGUI _nextBonusText;

    [FoldoutGroup("UI References/Bonuses")]
    [SerializeField] private GameObject _nextBonusSection;

    [FoldoutGroup("UI References/Cost")]
    [SerializeField] private TextMeshProUGUI _costText;

    [FoldoutGroup("UI References/Status")]
    [SerializeField] private TextMeshProUGUI _statusText;

    [FoldoutGroup("UI References/Status")]
    [SerializeField] private TextMeshProUGUI _statusIcon;

    [FoldoutGroup("Visual Settings")]
    [SerializeField] private Color _lockedColor = Color.red;

    [FoldoutGroup("Visual Settings")]
    [SerializeField] private Color _unlockedColor = Color.yellow;

    [FoldoutGroup("Visual Settings")]
    [SerializeField] private Color _purchasedColor = Color.green;
    [FoldoutGroup("Visual Settings")]
    [SerializeField] private Color _maxxedColor = Color.green;


    public override void Show(SkillNodeData data)
    {
        if (data == null)
            return;

        UpdateHeader(data);
        UpdateLevel(data);
        UpdateBonuses(data);
        UpdateCost(data);
        UpdateStatus(data);
    }

    private void UpdateHeader(SkillNodeData data)
    {
        if (_skillNameText != null)
            _skillNameText.text = $"<wave>{data.skillName}";

        if (_descriptionText != null)
            _descriptionText.text = data.description;
    }

    private void UpdateLevel(SkillNodeData data)
    {
        if (_currentLevelText == null)
            return;

        if (data.maxLevel == -1)
        {
            // Infinite levels
            _currentLevelText.text = $"Level {data.currentLevel}";
        }
        else if (data.maxLevel == 1)
        {
            // One-time purchase
            _currentLevelText.text = data.isPurchased ? "Purchased" : "Not Purchased";
        }
        else
        {
            // Capped levels
            _currentLevelText.text = $"Level {data.currentLevel}/{data.maxLevel}";
        }
    }

    private void UpdateBonuses(SkillNodeData data)
    {
        // Current bonus
        if (_currentBonusText != null)
        {
            string header = data.isPurchased ? "Current:" : "Effect:";
            _currentBonusText.text = $"{header}\n{data.currentBonusText}";
        }

        // Next level bonus (only if not maxed)
        if (_nextBonusSection != null)
        {
            _nextBonusSection.SetActive(!data.isMaxedOut);
        }

        if (_nextBonusText != null && !data.isMaxedOut)
        {
            _nextBonusText.text = $"Next Level:\n{data.nextBonusText}";
        }
    }

    private void UpdateCost(SkillNodeData data)
    {
        if (_costText == null)
            return;

        if (data.isMaxedOut)
        {
            _costText.text = "Maxed Out";
            return;
        }

        if (data.cost == null || data.cost.amount.isZero)
        {
            _costText.text = "Cost: Free";
            return;
        }

        string resourceName = data.cost.resource != null ? data.cost.resource.displayName : "Unknown";
        string amountStr = data.cost.amount.FormatWithDecimals();
        _costText.text = $"Cost: {amountStr} {resourceName}";
    }

    private void UpdateStatus(SkillNodeData data)
    {
        if (_statusText == null)
            return;

        string statusMessage;
        Color statusColor;

        if (data.isMaxedOut)
        {
            statusMessage = "Maxxed";
            statusColor = _maxxedColor;
        }
        else if (data.prerequisitesMet && data.canAfford)
        {
            statusMessage = "Click to Purchase";
            statusColor = _unlockedColor;
        }
        else
        {
            // Either locked or can't afford - both mean "can't purchase right now"
            statusMessage = "Can't Afford";
            statusColor = _lockedColor;
        }

        _statusText.text = statusMessage;
        _statusText.color = statusColor;

        if (_statusIcon != null)
        {
            _statusIcon.color = statusColor;
        }
    }
}
