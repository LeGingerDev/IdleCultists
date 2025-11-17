using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.Extensions;
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

public class SummoningPurchasableDisplay : BasePurchasableDisplay
{
    [SerializeField, FoldoutGroup("Identity")]
    private TextMeshProUGUI _gainText;

    [SerializeField, FoldoutGroup("Actionable")]
    private ValueBar _timerBar;

    private SummonPurchasableBlueprint _blueprint;
    private Func<SummonPurchasableBlueprint, bool> _canPurchase;
    private Action<SummonPurchasableBlueprint> _onPurchase;
    private Action<SummonPurchasableBlueprint> _onComplete;

    private bool _isTimerRunning = false;
    private bool _isProcessingPurchase = false;

    public void Initialise(SummonPurchasableBlueprint blueprint, Func<SummonPurchasableBlueprint, bool> canPurchase, Action<SummonPurchasableBlueprint> onPurchase, Action<SummonPurchasableBlueprint> onComplete)
    {
        _blueprint = blueprint;

        _canPurchase = canPurchase;
        _onPurchase = onPurchase;
        _onComplete = onComplete;

        base.Initialise();
    }

    protected override void SetupStaticUI()
    {
        if (_showIcon && _iconImage != null)
            _iconImage.sprite = _blueprint.icon;

        if (_showName && _displayNameText != null)
            _displayNameText.text = _blueprint.displayName;

        if (_showDescription && _descriptionText != null)
            _descriptionText.text = _blueprint.description;

        if (_gainText != null)
            _gainText.text = _blueprint.gainsDescription;

        // Initialize timer bar with full value
        float summonTime = (float)(double)_blueprint.GetSummonTime();
        UpdateTimeLeft(new ValueChange(summonTime, summonTime));
    }

    protected override void RefreshDynamicUI()
    {
        UpdateCostDisplay();
        CanPurchaseSet();
    }

    protected override void HookUpButton()
    {
        if (_showButton && _purchaseButton != null)
        {
            _purchaseButton.onClick.RemoveAllListeners();
            _purchaseButton.onClick.AddListener(() =>
            {
                if (!CanPurchase()) return;

                _isProcessingPurchase = true;
                _onPurchase?.Invoke(_blueprint);
                UpdateCostDisplay();
            });
        }
    }

    protected override void OnPurchaseClicked()
    {
        // Not used because we override HookUpButton with custom logic, but implement for completeness
        if (!CanPurchase()) return;
        _isProcessingPurchase = true;
        _onPurchase?.Invoke(_blueprint);
        UpdateCostDisplay();
    }

    protected override bool CanPurchase()
    {
        if (_isTimerRunning || _isProcessingPurchase) return false;
        return _canPurchase != null && _canPurchase.Invoke(_blueprint);
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
        if (_blueprint == null) return;

        ResourceAmountPair resourcePair = _blueprint.GetCurrentCostSafe();

        if (_costText != null)
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
    public void OnSummoningTimerUpdated(object sender, System.Collections.Generic.List<(string contextId, ValueChange valueChange)> updates)
    {
        if (!updates.Exists(u => u.contextId == _blueprint.GetContextId()))
            return;

        var updateValues = updates.Find(u => u.contextId == _blueprint.GetContextId()).valueChange;

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