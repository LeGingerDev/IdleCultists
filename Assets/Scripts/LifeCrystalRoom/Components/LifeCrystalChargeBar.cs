using LGD.Core;
using LGD.Core.Events;
using LGD.Utilities.Data;
using LGD.Utilities.UI.UIComponents;
using UnityEngine;

/// <summary>
/// UI component that listens to LifeCrystal charge events and updates a ValueBar
/// Attach this to a GameObject with a ValueBar component
/// </summary>
[RequireComponent(typeof(ValueBar))]
public class LifeCrystalChargeBar : BaseBehaviour
{
    [SerializeField, Tooltip("The zone ID to listen to. Leave empty to listen to all zones.")]
    private string _zoneIdToListenTo;

    private ValueBar _valueBar;

    private void Awake()
    {
        _valueBar = GetComponent<ValueBar>();
        if (_valueBar == null)
        {
            DebugManager.Error("[LifeCrystalChargeBar] No ValueBar component found!");
        }
    }

    [Topic(LifeCrystalEventIds.ON_CHARGE_STARTED)]
    public void OnChargeStarted(object sender, string zoneId, ValueChange chargeProgress)
    {
        // Filter by zone ID if specified
        if (!string.IsNullOrEmpty(_zoneIdToListenTo) && _zoneIdToListenTo != zoneId)
            return;

        if (_valueBar != null)
        {
            _valueBar.UpdateBar(chargeProgress);
        }
    }

    [Topic(LifeCrystalEventIds.ON_CHARGE_UPDATED)]
    public void OnChargeUpdated(object sender, string zoneId, ValueChange chargeProgress)
    {
        // Filter by zone ID if specified
        if (!string.IsNullOrEmpty(_zoneIdToListenTo) && _zoneIdToListenTo != zoneId)
            return;

        if (_valueBar != null)
        {
            _valueBar.UpdateBar(chargeProgress);
        }
    }

    [Topic(LifeCrystalEventIds.ON_CHARGE_COMPLETED)]
    public void OnChargeCompleted(object sender, string zoneId)
    {
        // Filter by zone ID if specified
        if (!string.IsNullOrEmpty(_zoneIdToListenTo) && _zoneIdToListenTo != zoneId)
            return;

        // Reset the bar to 0
        if (_valueBar != null)
        {
            _valueBar.UpdateBar(new ValueChange(0, 1));
        }
    }
}
