using LGD.Core.Events;
using LGD.UIelements.Panels;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Updated to use unified LifeCrystalItemDisplay for all purchasables
/// </summary>
public class LifeCrystalUIPanel : SlidePanel
{
    private List<LifeCrystalItemDisplay> _itemDisplays = new List<LifeCrystalItemDisplay>();

    protected override void Start()
    {
        base.Start();
        FindAllDisplays();
    }

    private void FindAllDisplays()
    {
        // Find all item displays in children (works for both StatPurchasables and EventPurchasables)
        _itemDisplays.Clear();
        _itemDisplays.AddRange(GetComponentsInChildren<LifeCrystalItemDisplay>(true));

        Debug.Log($"[LifeCrystalUIPanel] Found {_itemDisplays.Count} purchasable item displays");
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
        foreach (var display in _itemDisplays)
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