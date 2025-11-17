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

    protected override BasePurchasable GetDisplayedBlueprint()
    {
        return _purchasableBlueprint;
    }

    private void Start()
    {
        if (_purchasableBlueprint != null)
        {
            Initialise();
        }
    }

    // Base now handles icon/name/description via SetupStaticUI and provides a
    // SetupAdditionalStaticUI hook for any extra static elements.



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

    

    public int GetTimesPurchased()
    {
        return _purchasableBlueprint != null ? _purchasableBlueprint.GetPurchaseCount() : 0;
    }
}