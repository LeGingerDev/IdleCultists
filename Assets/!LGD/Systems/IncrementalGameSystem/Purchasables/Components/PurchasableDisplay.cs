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

public class PurchasableDisplay : BasePurchasableDisplay
{
    [SerializeField, FoldoutGroup("Identity")]
    private BasePurchasable _purchasableBlueprint;

    private void Start()
    {
        if (_purchasableBlueprint != null)
        {
            Initialise();
        }
    }

    protected override void SetupStaticUI()
    {
        if (_showIcon && _iconImage != null)
            _iconImage.sprite = _purchasableBlueprint.icon;

        if (_showName && _displayNameText != null)
            _displayNameText.text = _purchasableBlueprint.displayName;

        if (_showDescription && _descriptionText != null)
            _descriptionText.text = _purchasableBlueprint.description;
    }

    protected override void RefreshDynamicUI()
    {
        int timesPurchased = GetTimesPurchased();

        if (_showTimesPurchased && _timesPurchasedText != null)
            _timesPurchasedText.text = GetTimesPurchasedDisplayText(timesPurchased);

        ResourceAmountPair cost = _purchasableBlueprint.GetCurrentCostSafe();
        if (_showCost && _costText != null)
            _costText.text = GetCostDisplayText(cost);

        CanPurchaseSet();
    }

    protected override void HookUpButton()
    {
        base.HookUpButton();
    }

    protected override void OnPurchaseClicked()
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

    protected override bool CanPurchase()
    {
        return _purchasableBlueprint != null && _purchasableBlueprint.CanAfford();
    }

    protected override string GetButtonText()
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
}