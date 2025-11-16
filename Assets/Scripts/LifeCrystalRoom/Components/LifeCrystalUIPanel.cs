using LGD.Core.Events;
using LGD.UIelements.Panels;
using System.Collections.Generic;
using UnityEngine;

public class LifeCrystalUIPanel : SlidePanel
{
    private List<LifeCrystalUpgradeDisplay> _upgradeDisplays = new List<LifeCrystalUpgradeDisplay>();
    private List<LifeCrystalPurchasableDisplay> _purchasableDisplays = new List<LifeCrystalPurchasableDisplay>();

    protected override void Start()
    {
        base.Start();
        FindAllDisplays();
    }

    private void FindAllDisplays()
    {
        // Find all upgrade displays in children
        _upgradeDisplays.Clear();
        _upgradeDisplays.AddRange(GetComponentsInChildren<LifeCrystalUpgradeDisplay>(true));

        // Find all purchasable displays in children
        _purchasableDisplays.Clear();
        _purchasableDisplays.AddRange(GetComponentsInChildren<LifeCrystalPurchasableDisplay>(true));

        Debug.Log($"[LifeCrystalUIPanel] Found {_upgradeDisplays.Count} upgrade displays and {_purchasableDisplays.Count} purchasable displays");
    }

    protected override void OnClose()
    {
        // Nothing specific needed on close
    }

    protected override void OnOpen()
    {
        RefreshAllDisplays();
    }

    private void RefreshAllDisplays()
    {
        foreach (var display in _upgradeDisplays)
        {
            if (display != null)
            {
                display.Refresh();
            }
        }

        foreach (var display in _purchasableDisplays)
        {
            if (display != null)
            {
                display.Refresh();
            }
        }
    }

    [Topic(LifeCrystalEventIds.ON_LIFE_CRYSTAL_PURCHASE_COMPLETED)]
    public void OnPurchaseCompleted(object sender)
    {
        RefreshAllDisplays();
    }
}