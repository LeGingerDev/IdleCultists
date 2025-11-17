using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.Extensions;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchasableDisplay : BaseBehaviour
{
    [SerializeField, FoldoutGroup("Identity")]
    private BasePurchasable _purchasableBlueprint;

    [SerializeField, FoldoutGroup("UI References")]
    private Image _iconImage;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _displayNameText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _descriptionText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _timesPurchasedText;

    [SerializeField, FoldoutGroup("UI References")]
    private Button _purchaseButton;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _buttonPurchaseText;

    [SerializeField, FoldoutGroup("UI References")]
    private TextMeshProUGUI _costText;

    private Coroutine _canPurchaseLoopCoroutine;

    private void Start()
    {
        if (_purchasableBlueprint != null)
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
        _iconImage.sprite = _purchasableBlueprint.icon;
        _displayNameText.text = _purchasableBlueprint.displayName;
        _descriptionText.text = _purchasableBlueprint.description;
    }

    private void RefreshDynamicUI()
    {
        int timesPurchased = GetTimesPurchased();

        // Times purchased display
        _timesPurchasedText.text = GetTimesPurchasedDisplayText(timesPurchased);

        // Cost display
        ResourceAmountPair cost = _purchasableBlueprint.GetCurrentCostSafe();
        _costText.text = GetCostDisplayText(cost);

        CanPurchaseSet();
    }

    private void HookUpButton()
    {
        _purchaseButton.onClick.RemoveAllListeners();
        _purchaseButton.onClick.AddListener(OnPurchaseClicked);
    }

    private void OnPurchaseClicked()
    {
        if (_purchasableBlueprint == null)
            return;

        if (!_purchasableBlueprint.CanAfford())
        {
            DebugManager.Warning($"[IncrementalGame] Cannot afford {_purchasableBlueprint.displayName}");
            return;
        }

        bool success = _purchasableBlueprint.ExecutePurchase();

        if (success)
        {
            RefreshDynamicUI();
        }
    }

    public bool CanPurchase()
    {
        return _purchasableBlueprint != null && _purchasableBlueprint.CanAfford();
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
        _purchaseButton.interactable = CanPurchase();
        _buttonPurchaseText.text = GetButtonText();
    }

    // TODO: Replace with Topic System
    [Topic(StatEventIds.ON_STATS_RECALCULATED)]
    public void OnStatsRecalculated(object sender)
    {
        RefreshDynamicUI();
        
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

    private void OnDestroy()
    {
        StopPurchaseLoop();
    }

    #region Display Helpers

    private string GetTimesPurchasedDisplayText(int timesPurchased)
    {
        if (timesPurchased == 0)
            return "Not Yet Purchased";

        return $"Purchased {timesPurchased} time{(timesPurchased == 1 ? "" : "s")}";
    }

    private string GetButtonText()
    {
        if (!CanPurchase())
            return "Can't Afford";

        int timesPurchased = GetTimesPurchased();
        if (timesPurchased == 0)
            return "Purchase";

        return "Purchase Again";
    }

    private string GetCostDisplayText(ResourceAmountPair cost)
    {
        if (cost.amount.isZero)
            return "Free";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Cost\n");

        // Use automatic formatting: 2 decimals under 1K, 1 decimal at K+
        sb.Append($"{cost.amount.FormatWithDecimals()} {cost.resource.displayName}");

        return sb.ToString();
    }

    public int GetTimesPurchased()
    {
        return _purchasableBlueprint != null ? _purchasableBlueprint.GetPurchaseCount() : 0;
    }

    #endregion
}