using LargeNumbers;
using LGD.Core.Events;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using LGD.UIelements.Panels;
using LGD.Utilities.Data;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class TomeOfSummoningPanel : SlidePanel
{
    [FoldoutGroup("Summoning")]
    [SerializeField, FoldoutGroup("Summoning/References")]
    private List<SummonPurchasableBlueprint> _purchasables = new List<SummonPurchasableBlueprint>();

    [SerializeField, FoldoutGroup("Summoning/References")]
    private Transform _summonDisplayParent;

    [SerializeField, FoldoutGroup("Summoning/References")]
    private SummoningPurchasableDisplay _displayPrefab;

    private List<SummoningPurchasableDisplay> _createdDisplays = new();
    private AlphabeticNotation _pendingCapacity = AlphabeticNotation.zero;

    private int _activeSummonCount = 0;

    protected override void Start()
    {
        base.Start();
        CreateDisplays();
        RestorePendingCapacity();
    }

    protected override void OnClose()
    {
    }

    protected override void OnOpen()
    {
    }

    public void CreateDisplays()
    {
        _purchasables.ForEach(d =>
        {
            SummoningPurchasableDisplay display = Instantiate(_displayPrefab, _summonDisplayParent);
            display.Initialise(d, CanPurchase, OnPurchase, OnComplete);
            _createdDisplays.Add(display);
        });
    }

    /// <summary>
    /// Restores pending capacity from any active timers that were loaded from save
    /// Call this after displays are created so we can look up blueprints
    /// </summary>
    private void RestorePendingCapacity()
    {
        List<GameTimer> activeTimers = TimerManager.Instance.GetAllActiveTimers();

        if (activeTimers.Count == 0)
        {
            Debug.Log("<color=cyan>No timers to restore for summoning</color>");
            return;
        }

        _pendingCapacity = AlphabeticNotation.zero;
        int restoredCount = 0;

        foreach (GameTimer timer in activeTimers)
        {
            // Find the blueprint that matches this timer's contextId
            SummonPurchasableBlueprint blueprint = _purchasables.Find(p => p.GetContextId() == timer.contextId);

            if (blueprint != null)
            {
                AlphabeticNotation entityCapacity = blueprint.GetEntityCapacityAmount();
                _pendingCapacity += entityCapacity;
                restoredCount++;
                Debug.Log($"<color=yellow>Restored pending capacity for {blueprint.displayName}:</color> +{entityCapacity}");
            }
        }

        if (_pendingCapacity > AlphabeticNotation.zero)
        {
            Debug.Log($"<color=green>Total pending capacity restored:</color> {_pendingCapacity}");

            // Set initial active count from restored timers
            _activeSummonCount = restoredCount;

            // If we restored any, publish the "any active" event
            if (_activeSummonCount > 0)
            {
                Publish(SummoningEventIds.ON_ANY_SUMMONING_ACTIVE);
            }
        }
    }

    private bool CanPurchase(SummonPurchasableBlueprint blueprint)
    {
        AlphabeticNotation currentCapacity = StatManager.Instance.QueryStat(StatType.CapacityCount);
        AlphabeticNotation maxCapacity = StatManager.Instance.QueryStat(StatType.MaxCapacity);

        if ((currentCapacity + _pendingCapacity) > maxCapacity)
            return false;

        if (!blueprint.CanAfford())
            return false;

        return true;
    }

    private void OnPurchase(SummonPurchasableBlueprint blueprint)
    {
        // Execute purchase via extension (will remove cost and call PurchasableManager)
        blueprint.ExecutePurchase();
    }

    private void OnComplete(SummonPurchasableBlueprint blueprint)
    {
        AlphabeticNotation entityCapacity = blueprint.GetEntityCapacityAmount();
        _pendingCapacity -= entityCapacity;
        Publish(SummoningEventIds.ON_SUMMONING_STARTED, blueprint.entityToSummon);
    }

    [Topic(PurchasableEventIds.ON_SUMMON_ENTITY_PURCHASED)]
    public void OnEntityPurchaseExecuted(object sender, EntityBlueprint blueprint, BasePurchasableRuntimeData runtimeData)
    {
        SummonPurchasableBlueprint summonBlueprint = (SummonPurchasableBlueprint)sender;

        // Convert AlphabeticNotation to float for timer (summon times are in seconds, won't overflow)
        float summonTime = (float)(double)summonBlueprint.GetSummonTime();
        TimerManager.Instance.StartTimer(summonBlueprint.GetContextId(), summonTime);
        AlphabeticNotation entityCapacity = summonBlueprint.GetEntityCapacityAmount();
        _pendingCapacity += entityCapacity;
    }

    //////////////////////////////
    //// Topic Listeners for Overall State Tracking
    //////////////////////////////

    [Topic(TimerEventIds.TIMER_STARTED)]
    public void OnTimerStarted(object sender, string contextId, ValueChange valueChange)
    {
        // Check if this timer belongs to one of our summons
        if (!IsSummoningContextId(contextId))
            return;

        _activeSummonCount++;

        // If we just went from 0 to 1, publish "any active"
        if (_activeSummonCount == 1)
        {
            Publish(SummoningEventIds.ON_ANY_SUMMONING_ACTIVE);
            Debug.Log("<color=green>Summoning circle ACTIVATED</color>");
        }
    }

    [Topic(TimerEventIds.TIMER_COMPLETED)]
    public void OnTimerCompleted(object sender, string contextId)
    {
        // Check if this timer belongs to one of our summons
        if (!IsSummoningContextId(contextId))
            return;

        _activeSummonCount--;

        // If we just went from 1 to 0, publish "none active"
        if (_activeSummonCount == 0)
        {
            Publish(SummoningEventIds.ON_NO_SUMMONING_ACTIVE);
            Debug.Log("<color=red>Summoning circle DEACTIVATED</color>");
        }
    }

    [Topic(TimerEventIds.TIMER_CANCELLED)]
    public void OnTimerCancelled(object sender, string contextId)
    {
        // Check if this timer belongs to one of our summons
        if (!IsSummoningContextId(contextId))
            return;

        _activeSummonCount--;

        // If we just went from 1 to 0, publish "none active"
        if (_activeSummonCount == 0)
        {
            Publish(SummoningEventIds.ON_NO_SUMMONING_ACTIVE);
            Debug.Log("<color=red>Summoning circle DEACTIVATED</color>");
        }
    }

    private bool IsSummoningContextId(string contextId)
    {
        return _purchasables.Exists(p => p.GetContextId() == contextId);
    }
}