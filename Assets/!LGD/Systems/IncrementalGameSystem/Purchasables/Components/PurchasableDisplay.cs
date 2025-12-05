using Sirenix.OdinInspector;
using UnityEngine;

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
        DebugManager.Log($"[IncrementalGame] OnPurchaseClicked called on {gameObject.name}");

        if (_purchasableBlueprint == null)
        {
            DebugManager.Warning($"[IncrementalGame] OnPurchaseClicked: _purchasableBlueprint is NULL on {gameObject.name}");
            return;
        }

        if (!_purchasableBlueprint.CanAfford())
        {
            DebugManager.Warning($"[IncrementalGame] Cannot afford {_purchasableBlueprint.displayName}");
            return;
        }

        DebugManager.Log($"[IncrementalGame] Executing purchase for {_purchasableBlueprint.displayName} ({_purchasableBlueprint.purchasableId})");
        bool success = _purchasableBlueprint.ExecutePurchase();
        DebugManager.Log($"[IncrementalGame] Purchase execution returned: {success}");

        if (success)
        {
            DebugManager.Log($"[IncrementalGame] Purchase successful! Calling RefreshDynamicUI()...");
            RefreshDynamicUI();
        }
        else
        {
            DebugManager.Warning($"[IncrementalGame] Purchase FAILED for {_purchasableBlueprint.displayName}");
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