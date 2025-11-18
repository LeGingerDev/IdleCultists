using System.Collections.Generic;
using UnityEngine;

namespace LGD.UIelements.Panels
{
    /// <summary>
    /// Intermediate panel class that manages the lifecycle of BasePurchasableDisplay children.
    /// Sits between SlidePanel and concrete panel implementations.
    /// Handles starting/stopping periodic refresh and purchase loops when panel opens/closes.
    /// </summary>
    public abstract class PurchasablePanel : SlidePanel
    {
        private List<BasePurchasableDisplay> _cachedDisplays;

        protected override void OnOpen()
        {
            // Cache displays on first open
            if (_cachedDisplays == null)
            {
                _cachedDisplays = new List<BasePurchasableDisplay>();
                GetComponentsInChildren(true, _cachedDisplays);
            }

            // Start all child display loops when panel opens
            foreach (var display in _cachedDisplays)
            {
                if (display != null)
                {
                    display.StartPeriodicRefresh();
                    display.StartPurchaseLoop();
                }
            }
        }

        protected override void OnClose()
        {
            // Stop all child display loops when panel closes
            if (_cachedDisplays != null)
            {
                foreach (var display in _cachedDisplays)
                {
                    if (display != null)
                    {
                        display.StopPeriodicRefresh();
                        display.StopPurchaseLoop();
                    }
                }
            }
        }
    }
}
