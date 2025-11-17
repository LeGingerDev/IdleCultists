using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.Extensions;
using LGD.ResourceSystem.Models;
using LGD.Utilities.Data;
using LGD.Utilities.UI.UIComponents;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SummoningPurchasableDisplay : BasePurchasableDisplay
{
    [SerializeField, FoldoutGroup("Identity")]
    private TextMeshProUGUI _gainText;

    [SerializeField, FoldoutGroup("Actionable")]
    private ValueBar _timerBar;

    [SerializeField, FoldoutGroup("Identity")]
    private SummonPurchasableBlueprint _blueprint;

    private bool _isTimerRunning = false;
    private bool _isProcessingPurchase = false;

    // Static registry for pending capacity and active summon contexts (counts per context)
    private static readonly Dictionary<string, int> s_contextCounts = new Dictionary<string, int>();
    private static AlphabeticNotation s_pendingCapacity = AlphabeticNotation.zero;
    private static bool s_restoredFromTimers = false;
    // Ensure completion events are only published once per timer context
    private static readonly System.Collections.Generic.HashSet<string> s_completionPublished = new System.Collections.Generic.HashSet<string>();

    public static AlphabeticNotation PendingCapacity => s_pendingCapacity;

    private void Start()
    {
        if (_blueprint == null)
        {
            DebugManager.Warning("[SummoningDisplay] No blueprint assigned to summoning display");
            return;
        }

        // Self-initialize UI and hooks
        base.Initialise();

        // One-time restoration: collect active timers and mark pending capacity counts
        if (!s_restoredFromTimers)
        {
            s_restoredFromTimers = true;

            var activeTimers = TimerManager.Instance?.GetAllActiveTimers();
            if (activeTimers != null && activeTimers.Count > 0)
            {
                // Find all Summoning displays in the scene to match context ids
                var displays = Object.FindObjectsByType<SummoningPurchasableDisplay>(FindObjectsInactive.Include, sortMode: FindObjectsSortMode.None);

                foreach (var timer in activeTimers)
                {
                    foreach (var d in displays)
                    {
                        if (d._blueprint != null && d._blueprint.GetContextId() == timer.contextId)
                        {
                            // For each occurrence of this context id, increment counts
                            AddPendingForContextStatic(timer.contextId, d._blueprint.GetEntityCapacityAmount(), 1);
                            break;
                        }
                    }
                }
            }
        }

        // If a timer currently exists for this context, reconnect this display
        var existing = TimerManager.Instance?.GetTimer(_blueprint.GetContextId());
        if (existing != null)
        {
            _isTimerRunning = true;
        }
    }

    protected override BasePurchasable GetDisplayedBlueprint()
    {
        return _blueprint;
    }

    protected override void SetupAdditionalStaticUI()
    {
        if (_gainText != null && _blueprint != null)
            _gainText.text = _blueprint.gainsDescription;

        if (_blueprint != null)
        {
            float summonTime = (float)(double)_blueprint.GetSummonTime();
            UpdateTimeLeft(new ValueChange(summonTime, summonTime));
        }
    }

    protected override void HookUpButton()
    {
        if (_showButton && _purchaseButton != null)
        {
            _purchaseButton.onClick.RemoveAllListeners();
                _purchaseButton.onClick.AddListener(() =>
            {
                if (!CanPurchase()) return;

                // Capacity check: don't allow purchase if it would exceed max capacity
                AlphabeticNotation currentCapacity = StatManager.Instance.QueryStat(StatType.CapacityCount);
                AlphabeticNotation maxCapacity = StatManager.Instance.QueryStat(StatType.MaxCapacity);
                AlphabeticNotation entityCapacity = _blueprint.GetEntityCapacityAmount();

                if ((currentCapacity + s_pendingCapacity + entityCapacity) > maxCapacity)
                {
                    DebugManager.Warning($"[Summoning] Cannot summon {_blueprint.displayName} - capacity would be exceeded");
                    return;
                }

                _isProcessingPurchase = true;

                DebugManager.Log($"[Summoning] Purchase button clicked for {_blueprint.displayName} on {gameObject.name}");

                // Execute purchase (removes cost & runs blueprint handlers)
                bool success = _blueprint.ExecutePurchase();
                if (!success)
                {
                    _isProcessingPurchase = false;
                    return;
                }

                // On success, start the summon timer and update UI/capacity
                DebugManager.Log($"[Summoning] Purchase succeeded for {_blueprint.displayName}, starting timer for context {_blueprint.GetContextId()}");
                float summonTime = (float)(double)_blueprint.GetSummonTime();
                TimerManager.Instance?.StartTimer(_blueprint.GetContextId(), summonTime);
                // (no publish here) - spawners should listen for completion, not start
                UpdateCostDisplay();
            });
        }
    }

    protected override void OnPurchaseClicked()
    {
        // Not used; HookUpButton handles purchase flow for summoning displays
    }

    protected override bool CanPurchase()
    {
        if (_isTimerRunning || _isProcessingPurchase) return false;
        return _blueprint != null && _blueprint.CanAfford() && _blueprint.CanPurchaseMore();
    }

    protected override string GetButtonText()
    {
        return CanPurchase() ? "Summon" : "Can't Summon";
    }

    public void UpdateTimeLeft(ValueChange valueChange)
    {
        if (_timerBar != null)
            _timerBar.UpdateBar(valueChange);
    }

    public void UpdateCostDisplay()
    {
        // Defer to base dynamic UI which handles cost/times purchased formatting
        RefreshDynamicUI();
    }

    private static void AddPendingForContextStatic(string contextId, AlphabeticNotation entityCapacity, int count = 1)
    {
        if (string.IsNullOrEmpty(contextId) || count <= 0) return;

        if (!s_contextCounts.ContainsKey(contextId))
            s_contextCounts[contextId] = 0;

        s_contextCounts[contextId] += count;
        s_pendingCapacity += entityCapacity * count;

        if (TotalActiveContextsCount() - count == 0 && TotalActiveContextsCount() > 0)
            ServiceBus.Publish(SummoningEventIds.ON_ANY_SUMMONING_ACTIVE, null);
    }

    private void AddPendingForContext(string contextId)
    {
        AddPendingForContextStatic(contextId, _blueprint.GetEntityCapacityAmount(), 1);
    }

    private static void RemovePendingForContextStatic(string contextId, AlphabeticNotation entityCapacity, int count = 1)
    {
        if (string.IsNullOrEmpty(contextId) || count <= 0) return;

        if (!s_contextCounts.ContainsKey(contextId)) return;

        s_contextCounts[contextId] -= count;
        if (s_contextCounts[contextId] <= 0)
            s_contextCounts.Remove(contextId);

        s_pendingCapacity -= entityCapacity * count;

        if (TotalActiveContextsCount() == 0)
            ServiceBus.Publish(SummoningEventIds.ON_NO_SUMMONING_ACTIVE, null);
    }

    private void RemovePendingForContext(string contextId)
    {
        RemovePendingForContextStatic(contextId, _blueprint.GetEntityCapacityAmount(), 1);
    }

    private static int TotalActiveContextsCount()
    {
        int total = 0;
        foreach (var kv in s_contextCounts)
            total += kv.Value;
        return total;
    }

    //////////////////////////////
    //// Topic Listeners
    //////////////////////////////

    [Topic(PurchasableEventIds.ON_SUMMON_ENTITY_PURCHASED)]
    public void OnSummonEntityPurchased(object sender, EntityBlueprint entityToSummon, BasePurchasableRuntimeData runtimeData)
    {
        if (_blueprint == null || entityToSummon == null) return;

        // Only respond if this display represents the same entity context
        if (_blueprint.GetContextId() != entityToSummon.id) return;

        DebugManager.Log($"[Summoning] Detected summon purchased for context {entityToSummon.id} on {gameObject.name} - starting timer via event listener");

        // Start timer and update UI/capacity similar to HookUpButton post-purchase flow
        float summonTime = (float)(double)_blueprint.GetSummonTime();
        TimerManager.Instance?.StartTimer(_blueprint.GetContextId(), summonTime);
        RefreshDynamicUI();
    }

    [Topic(TimerEventIds.TIMER_STARTED)]
    public void OnSummonTimerStarted(object sender, string contextId, ValueChange valueChange)
    {
        if (contextId != _blueprint.GetContextId())
            return;

        UpdateTimeLeft(valueChange);
        _isTimerRunning = true;
        _isProcessingPurchase = false; // Clear the processing flag once timer starts

        AddPendingForContext(contextId);
        // Notify listeners that a summoning for this blueprint has started
        ServiceBus.Publish(SummoningEventIds.ON_SUMMONING_STARTED, this, _blueprint, valueChange);
        // Clear any previous completion publish marker for this context so future completions will publish
        if (s_completionPublished.Contains(contextId))
            s_completionPublished.Remove(contextId);
    }

    [Topic(TimerEventIds.TIMERS_UPDATED)]
    public void OnSummoningTimerUpdated(object sender, System.Collections.Generic.List<(string contextId, ValueChange valueChange)> updates)
    {
        if (!updates.Exists(u => u.contextId == _blueprint.GetContextId()))
            return;

        var updateValues = updates.Find(u => u.contextId == _blueprint.GetContextId()).valueChange;

        UpdateTimeLeft(updateValues);
        // Forward timer updates as summoning-updated events for consumers who care about this blueprint
        ServiceBus.Publish(SummoningEventIds.ON_SUMMONING_UPDATED, this, _blueprint, updateValues);
    }

    [Topic(TimerEventIds.TIMER_COMPLETED)]
    public void OnSummoningTimerCompleted(object sender, string contextId)
    {
        if (contextId != _blueprint.GetContextId())
            return;

        UpdateTimeLeft(new ValueChange(0, 1));

        RemovePendingForContext(contextId);
        _isTimerRunning = false;
        _isProcessingPurchase = false;

        // Notify that the summoning completed so spawners/listeners can act
        // Ensure we only publish once per context (multiple displays may be listening)
        if (!s_completionPublished.Contains(contextId))
        {
            s_completionPublished.Add(contextId);
            ServiceBus.Publish(SummoningEventIds.ON_SUMMONING_COMPLETED, this, _blueprint);
        }
    }

    [Topic(TimerEventIds.TIMER_CANCELLED)]
    public void OnSummoningTimerCancelled(object sender, string contextId)
    {
        if (contextId != _blueprint.GetContextId())
            return;

        RemovePendingForContext(contextId);
        _isTimerRunning = false;
        _isProcessingPurchase = false; // Also clear here in case of cancellation
        UpdateTimeLeft(new ValueChange(0, 0));
        // If a timer is cancelled, clear any completion marker so future timers can publish again
        if (s_completionPublished.Contains(contextId))
            s_completionPublished.Remove(contextId);
    }
}