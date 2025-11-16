using LGD.Core.Events;
using LGD.Core.Singleton;
using LGD.Utilities.Data;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerManager : MonoSingleton<TimerManager>
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private List<GameTimer> _activeTimers = new List<GameTimer>();

    [SerializeField, FoldoutGroup("Settings")]
    private float _updateInterval = 0.2f;

    [SerializeField, FoldoutGroup("Settings")]
    private bool _autoSave = true;

    private float _timeSinceLastUpdate = 0f;
    private SaveLoadProviderBase<GameTimer> _saveProvider;
    private bool _isInitialized = false;

    #region Initialization

    private void Start()
    {
        StartCoroutine(InitializeTimers());
    }

    /// <summary>
    /// Phase 1: Silent load - loads timers from save file but DOES NOT publish events
    /// Call this automatically during bootstrap scene loading
    /// </summary>
    private IEnumerator InitializeTimers()
    {
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<GameTimer>();

        if (_saveProvider != null)
        {
            yield return _saveProvider.Load();

            _activeTimers = _saveProvider.GetData();
            Debug.Log($"<color=cyan>Timer Manager initialized:</color> {_activeTimers.Count} active timers loaded (silent mode)");
        }
        else
        {
            Debug.LogWarning("Timer save provider not found! Timers will not be saved. Make sure TimerSaveProvider is in the scene.");
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Phase 2: Manual reconnection - publishes TIMER_STARTED events for all loaded timers
    /// Call this AFTER game scene UI is ready and listeners are registered
    /// </summary>
    public void InitializeLoadedTimers()
    {
        if (_activeTimers.Count == 0)
        {
            Debug.Log("<color=cyan>No timers to restore</color>");
            return;
        }

        Debug.Log($"<color=yellow>Restoring {_activeTimers.Count} timer(s) - publishing reconnection events</color>");

        foreach (var timer in _activeTimers)
        {
            // Publish TIMER_STARTED so UI can reconnect to this timer
            ServiceBus.Publish(TimerEventIds.TIMER_STARTED, this, timer.contextId, timer.GetValueChange());
        }

        Debug.Log($"<color=green>Timer restoration complete:</color> {_activeTimers.Count} timer(s) reconnected");
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        TickAllTimers(Time.deltaTime);

        _timeSinceLastUpdate += Time.deltaTime;

        if (_timeSinceLastUpdate >= _updateInterval)
        {
            _timeSinceLastUpdate = 0f;
            PublishBatchUpdates();
            ProcessCompletedTimers();
        }
    }

    private void TickAllTimers(float deltaTime)
    {
        foreach (var timer in _activeTimers)
        {
            timer.Tick(deltaTime);
        }
    }

    private void PublishBatchUpdates()
    {
        List<(string contextId, ValueChange valueChange)> updates = new List<(string, ValueChange)>();

        foreach (var timer in _activeTimers)
        {
            if (!timer.IsComplete)
            {
                updates.Add((timer.contextId, timer.GetValueChange()));
            }
        }

        if (updates.Count > 0)
        {
            ServiceBus.Publish(TimerEventIds.TIMERS_UPDATED, this, updates);
        }
    }

    private void ProcessCompletedTimers()
    {
        // Publish completions first
        foreach (var timer in _activeTimers)
        {
            if (timer.IsComplete)
            {
                ServiceBus.Publish(TimerEventIds.TIMER_COMPLETED, this, timer.contextId);
            }
        }

        // Remove all completed timers and mark dirty
        int removedCount = _activeTimers.RemoveAll(t => t.IsComplete);
        if (removedCount > 0)
        {
            MarkDirty();
        }
    }

    #endregion

    #region Public API

    public GameTimer StartTimer(string contextId, float duration)
    {
        GameTimer timer = new GameTimer(contextId, duration);
        _activeTimers.Add(timer);

        MarkDirty();

        ServiceBus.Publish(TimerEventIds.TIMER_STARTED, this, timer.contextId, timer.GetValueChange());

        Debug.Log($"Started timer for {contextId} - {duration}s");
        return timer;
    }

    public GameTimer GetTimer(string contextId)
    {
        return _activeTimers.Find(t => t.contextId == contextId);
    }

    public void CancelTimer(string contextId)
    {
        GameTimer timer = _activeTimers.Find(t => t.contextId == contextId);
        if (timer != null)
        {
            _activeTimers.Remove(timer);

            MarkDirty();

            ServiceBus.Publish(TimerEventIds.TIMER_CANCELLED, this, contextId);
            Debug.Log($"Cancelled timer for {contextId}");
        }
    }

    public List<GameTimer> GetAllActiveTimers()
    {
        return _activeTimers;
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    #endregion

    #region Save Methods

    private void MarkDirty()
    {
        if (_saveProvider != null)
        {
            _saveProvider.MarkDirty();
        }
    }

    public IEnumerator ManualSave()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SetData(_activeTimers);
            yield return _saveProvider.Save();
        }
    }

    public IEnumerator SaveIfDirty()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SaveIfDirty();
        }
    }

    #endregion

    #region Debug Methods

    [Button("Manual Save"), FoldoutGroup("Debug")]
    private void DebugManualSave()
    {
        StartCoroutine(ManualSave());
    }

    [Button("Cancel All Timers"), FoldoutGroup("Debug")]
    private void DebugCancelAllTimers()
    {
        // Make a copy to avoid modification during iteration
        List<string> contextIds = new List<string>();
        foreach (var timer in _activeTimers)
        {
            contextIds.Add(timer.contextId);
        }

        foreach (var contextId in contextIds)
        {
            CancelTimer(contextId);
        }

        Debug.Log("Cancelled all timers");
    }

    [Button("Test: Start 10s Timer"), FoldoutGroup("Debug")]
    private void DebugStartTestTimer()
    {
        StartTimer("test-timer", 10f);
    }

    #endregion
}