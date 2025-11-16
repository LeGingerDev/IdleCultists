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

/// <summary>
/// Updated to work with unified purchasable system (BasePurchasable)
/// Handles both StatPurchasables and EventPurchasables
/// </summary>
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

    private BasePurchasable _currentPurchasable;
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

    [Topic(LifeCrystalEventIds.ON_PURCHASABLE_SELECTED)]
    public void OnPurchasableSelected(object sender, BasePurchasable blueprint)
    {
        _currentPurchasable = blueprint;

        ShowContent();
        DisplayPurchasableInfo(blueprint);
        HookUpPurchaseButton();
        StartPurchaseLoop();
    }

    private void DisplayPurchasableInfo(BasePurchasable blueprint)
    {
        int currentPurchaseCount = blueprint.GetPurchaseCount(); // Use extension method
        int nextPurchase = currentPurchaseCount + 1;

        _titleText.text = blueprint.displayName;
        _descriptionText.text = blueprint.description;
        _levelText.text = GetPurchaseLevelText(blueprint, currentPurchaseCount);
        _bonusesText.text = GetPurchaseBonusText(blueprint, currentPurchaseCount, nextPurchase);

        // Cost display using extension method
        ResourceAmountPair cost = blueprint.GetCostForNextPurchase();
        _costText.text = GetCostDisplayText(cost);
    }

    private void HookUpPurchaseButton()
    {
        _purchaseButton.onClick.RemoveAllListeners();
        _purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }

    private void OnPurchaseButtonClicked()
    {
        if (_currentPurchasable != null)
        {
            PurchasePurchasable();
        }
    }

    private void PurchasePurchasable()
    {
        // Use extension method which handles cost check, removal, and execution
        bool success = _currentPurchasable.ExecutePurchase();

        if (success)
        {
            Publish(LifeCrystalEventIds.ON_LIFE_CRYSTAL_PURCHASE_COMPLETED);
            DisplayPurchasableInfo(_currentPurchasable); // Refresh display
        }
        else
        {
            Debug.LogWarning($"Cannot afford or execute purchase for {_currentPurchasable.displayName}");
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

        if (_currentPurchasable != null)
        {
            int currentPurchaseCount = _currentPurchasable.GetPurchaseCount(); // Use extension method
            canPurchase = CanPurchasePurchasable(_currentPurchasable, currentPurchaseCount);
            buttonText = GetPurchasableButtonText(_currentPurchasable, currentPurchaseCount, canPurchase);
        }

        _purchaseButton.interactable = canPurchase;
        _purchaseButtonText.text = buttonText;
    }

    private bool CanPurchasePurchasable(BasePurchasable blueprint, int currentPurchaseCount)
    {
        // Use extension method to check if maxed out
        if (blueprint.IsMaxedOut())
            return false;

        // Use extension method to check if can afford
        return blueprint.CanAfford();
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

    private string GetPurchaseLevelText(BasePurchasable blueprint, int currentPurchaseCount)
    {
        if (currentPurchaseCount == 0)
            return "Not Purchased";

        bool isMaxed = (blueprint.purchaseType == PurchaseType.OneTime && currentPurchaseCount >= 1) ||
                       (blueprint.purchaseType == PurchaseType.Capped &&
                        blueprint.maxPurchases != -1 &&
                        currentPurchaseCount >= blueprint.maxPurchases);

        if (isMaxed)
            return "Max Level";

        if (blueprint.purchaseType == PurchaseType.Infinite)
            return $"Lv. {currentPurchaseCount}";

        if (blueprint.purchaseType == PurchaseType.Capped)
            return $"Lv. {currentPurchaseCount}/{blueprint.maxPurchases}";

        return $"Purchased {currentPurchaseCount} time{(currentPurchaseCount == 1 ? "" : "s")}";
    }

    private string GetPurchaseBonusText(BasePurchasable blueprint, int currentPurchaseCount, int nextPurchase)
    {
        // Check if this is a StatPurchasable (provides modifiers)
        if (blueprint is StatPurchasable statPurchasable)
        {
            return GetStatPurchasableBonusText(statPurchasable, currentPurchaseCount, nextPurchase);
        }

        // EventPurchasables don't have bonus text (they trigger custom behaviors)
        return "";
    }

    private string GetStatPurchasableBonusText(StatPurchasable blueprint, int currentPurchaseCount, int nextPurchase)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool isMaxed = (blueprint.purchaseType == PurchaseType.OneTime && currentPurchaseCount >= 1) ||
                       (blueprint.purchaseType == PurchaseType.Capped &&
                        blueprint.maxPurchases != -1 &&
                        currentPurchaseCount >= blueprint.maxPurchases);

        foreach (var modifierScaling in blueprint.modifierScaling)
        {
            if (isMaxed)
            {
                // Show current values
                if (modifierScaling.modifierType == ModifierType.Multiplicative)
                {
                    float currentValue = modifierScaling.CalculateMultiplicativeValue(currentPurchaseCount);
                    sb.AppendLine($"{modifierScaling.statType}: +{currentValue * 100:F1}%");
                }
                else
                {
                    AlphabeticNotation currentValue = modifierScaling.CalculateAdditiveValue(currentPurchaseCount);
                    sb.AppendLine($"{modifierScaling.statType}: +{currentValue.FormatWithDecimals()}");
                }
            }
            else
            {
                // Show progression
                if (currentPurchaseCount == 0)
                {
                    // First purchase
                    if (modifierScaling.modifierType == ModifierType.Multiplicative)
                    {
                        float nextValue = modifierScaling.CalculateMultiplicativeValue(nextPurchase);
                        sb.AppendLine($"{modifierScaling.statType}: +{nextValue * 100:F1}%");
                    }
                    else
                    {
                        AlphabeticNotation nextValue = modifierScaling.CalculateAdditiveValue(nextPurchase);
                        sb.AppendLine($"{modifierScaling.statType}: +{nextValue.FormatWithDecimals()}");
                    }
                }
                else
                {
                    // Show progression from current to next
                    if (modifierScaling.modifierType == ModifierType.Multiplicative)
                    {
                        float currentValue = modifierScaling.CalculateMultiplicativeValue(currentPurchaseCount);
                        float nextValue = modifierScaling.CalculateMultiplicativeValue(nextPurchase);
                        sb.AppendLine($"{modifierScaling.statType}: +{currentValue * 100:F1}% → +{nextValue * 100:F1}%");
                    }
                    else
                    {
                        AlphabeticNotation currentValue = modifierScaling.CalculateAdditiveValue(currentPurchaseCount);
                        AlphabeticNotation nextValue = modifierScaling.CalculateAdditiveValue(nextPurchase);
                        sb.AppendLine($"{modifierScaling.statType}: +{currentValue.FormatWithDecimals()} → +{nextValue.FormatWithDecimals()}");
                    }
                }
            }
        }

        return sb.ToString();
    }

    private string GetCostDisplayText(ResourceAmountPair cost)
    {
        if (cost.amount.isZero)
            return "Free";

        AlphabeticNotation currentAmount = ResourceManager.Instance.GetResourceAmount(cost.resource);

        return $"{currentAmount.FormatWithDecimals()}/{cost.amount.FormatWithDecimals()} - {cost.resource.displayName}";
    }

    private string GetPurchasableButtonText(BasePurchasable blueprint, int currentPurchaseCount, bool canAfford)
    {
        bool isMaxed = (blueprint.purchaseType == PurchaseType.OneTime && currentPurchaseCount >= 1) ||
                       (blueprint.purchaseType == PurchaseType.Capped &&
                        blueprint.maxPurchases != -1 &&
                        currentPurchaseCount >= blueprint.maxPurchases);

        if (isMaxed)
            return "Maxed";

        if (!canAfford)
            return "Can't Afford";

        if (currentPurchaseCount == 0)
            return "Purchase";

        // StatPurchasables use "Upgrade", EventPurchasables use "Purchase Again"
        if (blueprint is StatPurchasable)
            return "Upgrade";

        return "Purchase Again";
    }

    #endregion
}
