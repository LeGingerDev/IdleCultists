using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeDisplay : BaseBehaviour
{
    [SerializeField, FoldoutGroup("Identity")]
    private UpgradeBlueprint _upgradeBlueprint;

    [SerializeField, FoldoutGroup("UI References")]
    private Image _iconImage;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _displayNameText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _descriptionText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _currentTierText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _gainText;

    [SerializeField, FoldoutGroup("UI References")]
    private GameObject _endContainer;

    [SerializeField, FoldoutGroup("UI References")]
    private Button _purchaseButton;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _buttonPurchaseText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _costText;

    private Coroutine _canPurchaseLoopCoroutine;

    private void Start()
    {
        if (_upgradeBlueprint != null)
        {
            Initialise();
        }
    }

    [Button]
    private void Initialise()
    {
        SetupStaticUI();
        RefreshDynamicUI();
        HookUpButton();

        StartPurchaseLoop();
    }

    private void SetupStaticUI()
    {
        _iconImage.sprite = _upgradeBlueprint.icon;
        _displayNameText.text = _upgradeBlueprint.displayName;
        _descriptionText.text = _upgradeBlueprint.description;
    }

    private void RefreshDynamicUI()
    {
        int currentTier = _upgradeBlueprint.GetCurrentTier();
        int nextTier = currentTier + 1;

        // Tier display
        _currentTierText.text = GetTierDisplayText(currentTier);

        // Cost display
        ResourceAmountPair cost = _upgradeBlueprint.GetCostForNextTier();
        _costText.text = GetCostDisplayText(cost);
        _costText.gameObject.SetActive(!IsMaxedOut(currentTier));

        // Next tier bonus
        _gainText.text = GetNextTierBonusText(currentTier, nextTier);
        CanPurchaseSet();
    }

    private void HookUpButton()
    {
        _purchaseButton.onClick.RemoveAllListeners();
        _purchaseButton.onClick.AddListener(OnPurchaseClicked);
    }

    private void OnPurchaseClicked()
    {
        bool success = _upgradeBlueprint.ExecutePurchaseTier();

        if (success)
        {
            RefreshDynamicUI();
        }
    }

    public bool CanPurchase()
    {
        int currentTier = _upgradeBlueprint.GetCurrentTier();
        int nextTier = currentTier + 1;

        if (_upgradeBlueprint.IsMaxedOut())
            return false;

        if (!_upgradeBlueprint.CanAffordNextTier())
            return false;

        return true;
    }

    public IEnumerator CanPurchaseLoop()
    {
        while (true)
        {
            CanPurchaseSet();

            yield return new WaitForSeconds(0.2f);
        }
    }

    public void CanPurchaseSet()
    {
        int currentIndex = GetCurrentTierIndex();
        _purchaseButton.interactable = CanPurchase();
        _buttonPurchaseText.text = GetButtonText(currentIndex, _upgradeBlueprint.IsMaxedOut());
    }

    // TODO: Replace with Topic System
    [Topic(StatEventIds.ON_STATS_RECALCULATED)]
    public void OnStatsRecalculated(object sender)
    {
        RefreshDynamicUI();

        if (IsMaxedOut(GetCurrentTierIndex()))
        {
            _endContainer.gameObject.SetActive(false);
            StopPurchaseLoop();
        }
    }

    public void StartPurchaseLoop()
    {
        if (_canPurchaseLoopCoroutine == null)
        {
            _canPurchaseLoopCoroutine = StartCoroutine(CanPurchaseLoop());
        }
    }

    public void StopPurchaseLoop()
    {
        if (_canPurchaseLoopCoroutine != null)
        {
            StopCoroutine(_canPurchaseLoopCoroutine);
            _canPurchaseLoopCoroutine = null;
        }
    }

    #region Display Helpers

    private string GetTierDisplayText(int currentTier)
    {
        // Not purchased yet
        if (currentTier == 0)
            return "Not Purchased";

        // Check if maxed out
        bool isMaxed = IsMaxedOut(currentTier);
        if (isMaxed)
            return "Max Level";

        // Display based on upgrade type
        switch (_upgradeBlueprint.upgradeType)
        {
            case UpgradeType.Infinite:
                return $"Lvl {currentTier}";

            case UpgradeType.Capped:
            case UpgradeType.OneTime:
                return $"Lvl {currentTier}/{_upgradeBlueprint.maxTier}";

            default:
                return $"Lvl {currentTier}";
        }
    }

    private string GetNextTierBonusText(int currentTier, int nextTier)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool isMaxed = IsMaxedOut(currentTier);

        foreach (var modifierScaling in _upgradeBlueprint.modifierScaling)
        {
            // If maxed out, only show current values
            if (isMaxed)
            {
                if (modifierScaling.modifierType == ModifierType.Multiplicative)
                {
                    float currentValue = modifierScaling.CalculateMultiplicativeValue(currentTier);
                    sb.AppendLine($"{modifierScaling.statType}: +{currentValue * 100:F1}%");
                }
                else
                {
                    AlphabeticNotation currentValue = modifierScaling.CalculateAdditiveValue(currentTier);
                    sb.AppendLine($"{modifierScaling.statType}: +{currentValue}");
                }
                continue;
            }

            // Normal progression display
            string displayValue;

            if (modifierScaling.modifierType == ModifierType.Multiplicative)
            {
                float nextValue = modifierScaling.CalculateMultiplicativeValue(nextTier);
                displayValue = $"+{nextValue * 100:F1}%";
            }
            else
            {
                AlphabeticNotation nextValue = modifierScaling.CalculateAdditiveValue(nextTier);
                displayValue = $"+{nextValue}";
            }

            // Different format based on upgrade type
            if (_upgradeBlueprint.upgradeType == UpgradeType.OneTime || currentTier == 0)
            {
                // First purchase or one-time: "Add Max Capacity: 1"
                sb.AppendLine($"{modifierScaling.statType}: {displayValue}");
            }
            else
            {
                // Multi-tier: "Increase Max Capacity: 1 → 2"
                string currentDisplay;

                if (modifierScaling.modifierType == ModifierType.Multiplicative)
                {
                    float currentValue = modifierScaling.CalculateMultiplicativeValue(currentTier);
                    currentDisplay = $"+{currentValue * 100:F1}%";
                }
                else
                {
                    AlphabeticNotation currentValue = modifierScaling.CalculateAdditiveValue(currentTier);
                    currentDisplay = $"+{currentValue}";
                }

                sb.AppendLine($"{modifierScaling.statType}: {currentDisplay} → {displayValue}");
            }
        }

        return sb.ToString();
    }

    private string GetButtonText(int currentTier, bool isMaxed)
    {
        if (isMaxed)
            return "Maxed";

        if (!CanAffordNextTier(GetNextTierIndex()))
            return "Can't Afford";

        if (currentTier == 0)
            return "Purchase";

        return "Upgrade";
    }

    public int GetCurrentTierIndex()
    {
        return _upgradeBlueprint.GetCurrentTier();
    }

    public int GetNextTierIndex()
    {
        return _upgradeBlueprint.GetNextTierIndex();
    }

    private string GetCostDisplayText(ResourceAmountPair cost)
    {
        if (cost.amount.isZero)
            return "Free";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Cost\n");

        // Use AlphabeticNotation's ToString() - displays like "1.5K", "23.4M", etc.
        sb.Append($"{cost.amount} {cost.resource.displayName}");

        return sb.ToString();
    }

    private bool CanAffordNextTier(int nextTier)
    {
        ResourceAmountPair cost = _upgradeBlueprint.GetCostForTierSafe(nextTier);
        return ResourceManager.Instance != null && ResourceManager.Instance.CanSpend(cost);
    }

    private bool IsMaxedOut(int currentTier)
    {
        if (_upgradeBlueprint.upgradeType == UpgradeType.Infinite)
            return false;

        return _upgradeBlueprint.maxTier != -1 && currentTier >= _upgradeBlueprint.maxTier;
    }

    #endregion
}