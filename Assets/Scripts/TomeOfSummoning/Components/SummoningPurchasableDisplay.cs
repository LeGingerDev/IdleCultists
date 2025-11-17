using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.ResourceSystem.Models;
using LGD.Utilities.Data;
using LGD.Utilities.UI.UIComponents;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SummoningPurchasableDisplay : BaseBehaviour
{
    [SerializeField, FoldoutGroup("Identity")]
    private Image _iconImage;
    [SerializeField, FoldoutGroup("Identity")]
    private TextMeshProUGUI _displayNameText;
    [SerializeField, FoldoutGroup("Identity")]
    private TextMeshProUGUI _descriptionText;
    [SerializeField, FoldoutGroup("Identity")]
    private TextMeshProUGUI _gainText;

    [SerializeField, FoldoutGroup("Actionable")]
    private Button _summonButton;
    [SerializeField, FoldoutGroup("Actionable")]
    private TextMeshProUGUI _summonButtonText;
    [SerializeField, FoldoutGroup("Actionable")]
    private TextMeshProUGUI _costText;
    [SerializeField, FoldoutGroup("Actionable")]
    private ValueBar _timerBar;

    private SummonPurchasableBlueprint _blueprint;
    private Func<SummonPurchasableBlueprint, bool> _canPurchase;
    private Action<SummonPurchasableBlueprint> _onPurchase;
    private Action<SummonPurchasableBlueprint> _onComplete;

    private Coroutine _canPurchaseLoop;

    private bool _isTimerRunning = false;
    private bool _isProcessingPurchase = false;

    public void Initialise(SummonPurchasableBlueprint blueprint, Func<SummonPurchasableBlueprint, bool> canPurchase, Action<SummonPurchasableBlueprint> onPurchase, Action<SummonPurchasableBlueprint> onComplete)
    {
        _blueprint = blueprint;

        _canPurchase = canPurchase;
        _onPurchase = onPurchase;
        _onComplete = onComplete;

        _iconImage.sprite = blueprint.icon;
        _displayNameText.text = blueprint.displayName;
        _descriptionText.text = blueprint.description;
        _gainText.text = blueprint.gainsDescription;

        UpdateCostDisplay();

        // Convert AlphabeticNotation to float for timer (summon times are in seconds, won't overflow)
        float summonTime = (float)(double)blueprint.GetSummonTime();
        UpdateTimeLeft(new ValueChange(summonTime, summonTime));

        TriggerBuyLoop();
        HookUpButton();
    }

    public void HookUpButton()
    {
        _summonButton.onClick.RemoveAllListeners();
        _summonButton.onClick.AddListener(() =>
        {
            if (!CanPurchaseUpgrade()) return; // Safety check

            _isProcessingPurchase = true;
            _onPurchase?.Invoke(_blueprint);
            UpdateCostDisplay();
        });
    }

    public void TriggerBuyLoop()
    {
        StopBuyLoop();

        _canPurchaseLoop = StartCoroutine(CanPurchaseLoop());
    }

    public void StopBuyLoop()
    {
        if (_canPurchaseLoop != null)
        {
            StopCoroutine(_canPurchaseLoop);
            _canPurchaseLoop = null;
        }
    }

    public IEnumerator CanPurchaseLoop()
    {
        while (true)
        {
            bool canPurchase = CanPurchaseUpgrade();
            _summonButton.interactable = canPurchase;
            _summonButtonText.text = canPurchase ? "Summon" : "Can't Summon";
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void UpdateTimeLeft(ValueChange valueChange)
    {
        _timerBar.UpdateBar(valueChange);
    }

    public bool CanPurchaseUpgrade()
    {
        if (_isTimerRunning || _isProcessingPurchase) return false; // Check both
        return _canPurchase.Invoke(_blueprint);
    }

    public void UpdateCostDisplay()
    {
        ResourceAmountPair resourcePair = _blueprint.GetCurrentCostSafe();

        // Use automatic formatting: 2 decimals under 1K, 1 decimal at K+
        _costText.text = $"Cost\n{resourcePair.resource.displayName} {resourcePair.amount.FormatWithDecimals()}";
    }

    //////////////////////////////
    //// Topic Listeners
    //////////////////////////////

    [Topic(TimerEventIds.TIMER_STARTED)]
    public void OnSummonTimerStarted(object sender, string contextId, ValueChange valueChange)
    {
        if (contextId != _blueprint.GetContextId())
            return;

        UpdateTimeLeft(valueChange);
        _isTimerRunning = true;
        _isProcessingPurchase = false; // Clear the processing flag once timer starts
    }

    [Topic(TimerEventIds.TIMERS_UPDATED)]
    public void OnSummoningTimerUpdated(object sender, List<(string contextId, ValueChange valueChange)> updates)
    {
        if (!updates.Any(u => u.contextId == _blueprint.GetContextId()))
            return;

        ValueChange updateValues;
        updateValues = updates.First(u => u.contextId == _blueprint.GetContextId()).valueChange;

        UpdateTimeLeft(updateValues);
    }

    [Topic(TimerEventIds.TIMER_COMPLETED)]
    public void OnSummoningTimerCompleted(object sender, string contextId)
    {
        if (contextId != _blueprint.GetContextId())
            return;

        UpdateTimeLeft(new ValueChange(0, 1));

        _onComplete?.Invoke(_blueprint);
        _isTimerRunning = false;
    }

    [Topic(TimerEventIds.TIMER_CANCELLED)]
    public void OnSummoningTimerCancelled(object sender, string contextId)
    {
        if (contextId != _blueprint.GetContextId())
            return;

        _isTimerRunning = false;
        _isProcessingPurchase = false; // Also clear here in case of cancellation
        UpdateTimeLeft(new ValueChange(0, 0));
    }
}