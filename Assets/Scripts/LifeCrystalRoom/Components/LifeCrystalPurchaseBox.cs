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

public class LifeCrystalPurchaseBox : BaseBehaviour
{
    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _titleText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _descriptionText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _levelText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _bonusesText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _costText;

    [SerializeField, FoldoutGroup("UI References")]
    private Button _purchaseButton;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _purchaseButtonText;

    [SerializeField, FoldoutGroup("UI References")]
    private GameObject _contentContainer;

    private UpgradeBlueprint _currentUpgrade;
    private PurchasableBlueprint _currentPurchasable;
    private bool _isUpgradeSelected = false;

    private Coroutine _canPurchaseLoopCoroutine;

    protected override void OnEnable()
    {
        base.OnEnable();
        HideContent();
    }

    private void HideContent()
    {
        if (_contentContainer != null)
        {
            _contentContainer.SetActive(false);
        }
    }

    private void ShowContent()
    {
        if (_contentContainer != null)
        {
            _contentContainer.SetActive(true);
        }
    }

    [Topic(LifeCrystalEventIds.ON_UPGRADE_SELECTED)]
    public void OnUpgradeSelected(object sender, UpgradeBlueprint blueprint)
    {
        _currentUpgrade = blueprint;
        _currentPurchasable = null;
        _isUpgradeSelected = true;

        ShowContent();
        DisplayUpgradeInfo(blueprint);
        HookUpPurchaseButton();
        StartPurchaseLoop();
    }

    [Topic(LifeCrystalEventIds.ON_PURCHASABLE_SELECTED)]
    public void OnPurchasableSelected(object sender, PurchasableBlueprint blueprint)
    {
        _currentUpgrade = null;
        _currentPurchasable = blueprint;
        _isUpgradeSelected = false;

        ShowContent();
        DisplayPurchasableInfo(blueprint);
        HookUpPurchaseButton();
        StartPurchaseLoop();
    }

    private void DisplayUpgradeInfo(UpgradeBlueprint blueprint)
    {
        int currentTier = UpgradeManager.Instance.GetUpgradeTier(blueprint.upgradeId);
        int nextTier = currentTier + 1;

        _titleText.text = blueprint.displayName;
        _descriptionText.text = blueprint.description;
        _levelText.text = GetUpgradeLevelText(blueprint, currentTier);
        _bonusesText.text = GetUpgradeBonusText(blueprint, currentTier, nextTier);

        // Cost display
        ResourceAmountPair cost = blueprint.GetCostWithResourceForTier(nextTier);
        _costText.text = GetCostDisplayText(cost);
    }

    private void DisplayPurchasableInfo(PurchasableBlueprint blueprint)
    {
        int timesPurchased = PurchaseManager.Instance.GetTimesPurchased(blueprint.purchasableId);

        _titleText.text = blueprint.displayName;
        _descriptionText.text = blueprint.description;
        _levelText.text = GetPurchasableLevelText(timesPurchased);
        _bonusesText.text = GetPurchasableBonusText(blueprint);

        // Cost display
        ResourceAmountPair cost = blueprint.GetCurrentCost();
        _costText.text = GetCostDisplayText(cost);
    }

    private void HookUpPurchaseButton()
    {
        _purchaseButton.onClick.RemoveAllListeners();
        _purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }

    private void OnPurchaseButtonClicked()
    {
        if (_isUpgradeSelected && _currentUpgrade != null)
        {
            PurchaseUpgrade();
        }
        else if (!_isUpgradeSelected && _currentPurchasable != null)
        {
            PurchasePurchasable();
        }
    }

    private void PurchaseUpgrade()
    {
        int nextTier = UpgradeManager.Instance.GetUpgradeTier(_currentUpgrade.upgradeId) + 1;
        ResourceAmountPair cost = _currentUpgrade.GetCostWithResourceForTier(nextTier);

        if (!ResourceManager.Instance.CanSpend(cost))
        {
            Debug.LogWarning($"Cannot afford {_currentUpgrade.displayName}");
            return;
        }

        ResourceManager.Instance.RemoveResource(cost);
        bool success = UpgradeManager.Instance.PurchaseUpgradeTier(_currentUpgrade.upgradeId);

        if (success)
        {
            Publish(LifeCrystalEventIds.ON_LIFE_CRYSTAL_PURCHASE_COMPLETED);
            DisplayUpgradeInfo(_currentUpgrade); // Refresh display
        }
    }

    private void PurchasePurchasable()
    {
        ResourceAmountPair cost = _currentPurchasable.GetCurrentCost();

        if (!ResourceManager.Instance.CanSpend(cost))
        {
            Debug.LogWarning($"Cannot afford {_currentPurchasable.displayName}");
            return;
        }

        ResourceManager.Instance.RemoveResource(cost);
        bool success = PurchaseManager.Instance.ExecutePurchase(_currentPurchasable.purchasableId);

        if (success)
        {
            Publish(LifeCrystalEventIds.ON_LIFE_CRYSTAL_PURCHASE_COMPLETED);
            DisplayPurchasableInfo(_currentPurchasable); // Refresh display
        }
    }

    public IEnumerator CanPurchaseLoop()
    {
        while (true)
        {
            UpdatePurchaseButton();
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void UpdatePurchaseButton()
    {
        bool canPurchase = false;
        string buttonText = "Purchase";

        if (_isUpgradeSelected && _currentUpgrade != null)
        {
            int currentTier = UpgradeManager.Instance.GetUpgradeTier(_currentUpgrade.upgradeId);
            canPurchase = CanPurchaseUpgrade(_currentUpgrade, currentTier);
            buttonText = GetUpgradeButtonText(_currentUpgrade, currentTier, canPurchase);
        }
        else if (!_isUpgradeSelected && _currentPurchasable != null)
        {
            canPurchase = CanPurchasePurchasable(_currentPurchasable);
            buttonText = GetPurchasableButtonText(_currentPurchasable, canPurchase);
        }

        _purchaseButton.interactable = canPurchase;
        _purchaseButtonText.text = buttonText;
    }

    private bool CanPurchaseUpgrade(UpgradeBlueprint blueprint, int currentTier)
    {
        // Check if maxed out
        if (blueprint.upgradeType != UpgradeType.Infinite &&
            blueprint.maxTier != -1 &&
            currentTier >= blueprint.maxTier)
        {
            return false;
        }

        // Check if can afford
        int nextTier = currentTier + 1;
        ResourceAmountPair cost = blueprint.GetCostWithResourceForTier(nextTier);
        return ResourceManager.Instance.CanSpend(cost);
    }

    private bool CanPurchasePurchasable(PurchasableBlueprint blueprint)
    {
        ResourceAmountPair cost = blueprint.GetCurrentCost();
        return ResourceManager.Instance.CanSpend(cost);
    }

    public void StartPurchaseLoop()
    {
        StopPurchaseLoop();
        _canPurchaseLoopCoroutine = StartCoroutine(CanPurchaseLoop());
    }

    public void StopPurchaseLoop()
    {
        if (_canPurchaseLoopCoroutine != null)
        {
            StopCoroutine(_canPurchaseLoopCoroutine);
            _canPurchaseLoopCoroutine = null;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopPurchaseLoop();
    }

    #region Display Helpers

    private string GetUpgradeLevelText(UpgradeBlueprint blueprint, int currentTier)
    {
        if (currentTier == 0)
            return "Not Purchased";

        bool isMaxed = blueprint.upgradeType != UpgradeType.Infinite &&
                       blueprint.maxTier != -1 &&
                       currentTier >= blueprint.maxTier;

        if (isMaxed)
            return "Max Level";

        if (blueprint.upgradeType == UpgradeType.Infinite)
            return $"Lv. {currentTier}";

        return $"Lv. {currentTier}/{blueprint.maxTier}";
    }

    private string GetPurchasableLevelText(int timesPurchased)
    {
        if (timesPurchased == 0)
            return "Not Purchased";

        return $"Purchased {timesPurchased} time{(timesPurchased == 1 ? "" : "s")}";
    }

    private string GetUpgradeBonusText(UpgradeBlueprint blueprint, int currentTier, int nextTier)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool isMaxed = blueprint.upgradeType != UpgradeType.Infinite &&
                       blueprint.maxTier != -1 &&
                       currentTier >= blueprint.maxTier;

        foreach (var modifierScaling in blueprint.modifierScaling)
        {
            if (isMaxed)
            {
                // Show current values
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
            }
            else
            {
                // Show progression
                if (currentTier == 0)
                {
                    // First purchase
                    if (modifierScaling.modifierType == ModifierType.Multiplicative)
                    {
                        float nextValue = modifierScaling.CalculateMultiplicativeValue(nextTier);
                        sb.AppendLine($"{modifierScaling.statType}: +{nextValue * 100:F1}%");
                    }
                    else
                    {
                        AlphabeticNotation nextValue = modifierScaling.CalculateAdditiveValue(nextTier);
                        sb.AppendLine($"{modifierScaling.statType}: +{nextValue}");
                    }
                }
                else
                {
                    // Show progression from current to next
                    if (modifierScaling.modifierType == ModifierType.Multiplicative)
                    {
                        float currentValue = modifierScaling.CalculateMultiplicativeValue(currentTier);
                        float nextValue = modifierScaling.CalculateMultiplicativeValue(nextTier);
                        sb.AppendLine($"{modifierScaling.statType}: +{currentValue * 100:F1}% → +{nextValue * 100:F1}%");
                    }
                    else
                    {
                        AlphabeticNotation currentValue = modifierScaling.CalculateAdditiveValue(currentTier);
                        AlphabeticNotation nextValue = modifierScaling.CalculateAdditiveValue(nextTier);
                        sb.AppendLine($"{modifierScaling.statType}: +{currentValue} → +{nextValue}");
                    }
                }
            }
        }

        return sb.ToString();
    }

    private string GetPurchasableBonusText(PurchasableBlueprint blueprint)
    {
        // Purchasables don't have modifiers like upgrades
        // This would be custom per purchasable type
        // For now, return description or empty
        return ""; // Or could return additional flavor text
    }

    private string GetCostDisplayText(ResourceAmountPair cost)
    {
        if (cost.amount.isZero)
            return "Free";

        AlphabeticNotation currentAmount = ResourceManager.Instance.GetResourceAmount(cost.resource);
        
        return $"{currentAmount}/{cost.amount} - {cost.resource.displayName}";
    }

    private string GetUpgradeButtonText(UpgradeBlueprint blueprint, int currentTier, bool canAfford)
    {
        bool isMaxed = blueprint.upgradeType != UpgradeType.Infinite &&
                       blueprint.maxTier != -1 &&
                       currentTier >= blueprint.maxTier;

        if (isMaxed)
            return "Maxed";

        if (!canAfford)
            return "Can't Afford";

        if (currentTier == 0)
            return "Purchase";

        return "Upgrade";
    }

    private string GetPurchasableButtonText(PurchasableBlueprint blueprint, bool canAfford)
    {
        if (!canAfford)
            return "Can't Afford";

        int timesPurchased = PurchaseManager.Instance.GetTimesPurchased(blueprint.purchasableId);
        
        if (timesPurchased == 0)
            return "Purchase";

        return "Purchase Again";
    }

    #endregion
}