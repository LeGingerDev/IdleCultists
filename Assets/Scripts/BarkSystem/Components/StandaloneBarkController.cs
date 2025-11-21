using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Standalone bark controller for one-off characters or objects.
/// Self-contained system that periodically displays random barks from a custom list.
/// Does NOT integrate with the Topic system - completely standalone.
/// Perfect for NPCs, ambient characters, or just adding personality to objects.
/// </summary>
public class StandaloneBarkController : SerializedMonoBehaviour
{
    #region UI References

    [SerializeField, FoldoutGroup("UI References")]
    [Required, Tooltip("The container GameObject that holds the bark bubble (will be shown/hidden)")]
    private GameObject _bubbleContainer;

    [SerializeField, FoldoutGroup("UI References")]
    [Required, Tooltip("The TextMeshPro component that displays the bark text")]
    private TextMeshProUGUI _barkText;

    [SerializeField, FoldoutGroup("UI References")]
    [Required, Tooltip("Canvas group for fade animations")]
    private CanvasGroup _canvasGroup;

    #endregion

    #region Bark Content

    [SerializeField, FoldoutGroup("Bark Content")]
    [InfoBox("List of barks this character can say. One will be randomly selected each time.")]
    [TextArea(2, 5)]
    private List<string> _barks = new List<string>()
    {
        "Hello there!",
        "What a lovely day.",
        "Hmm...",
        "I wonder what's for dinner?",
        "This is fine."
    };

    #endregion

    #region Timing Settings

    [SerializeField, FoldoutGroup("Timing Settings")]
    [Tooltip("Minimum time in seconds between barks")]
    [MinValue(1f)]
    private float _minBarkInterval = 10f;

    [SerializeField, FoldoutGroup("Timing Settings")]
    [Tooltip("Maximum time in seconds between barks")]
    [MinValue(1f)]
    private float _maxBarkInterval = 30f;

    [SerializeField, FoldoutGroup("Timing Settings")]
    [Tooltip("If true, starts barking automatically on enable")]
    private bool _autoStart = true;

    [SerializeField, FoldoutGroup("Timing Settings")]
    [Tooltip("If true, delays the first bark by a random interval. If false, first bark happens immediately.")]
    private bool _delayFirstBark = true;

    #endregion

    #region Display Settings

    [SerializeField, FoldoutGroup("Display Settings")]
    [Tooltip("How long the bark stays visible (in seconds)")]
    [MinValue(0.5f)]
    private float _displayDuration = 3f;

    [SerializeField, FoldoutGroup("Display Settings")]
    [Tooltip("Duration of fade in animation (in seconds)")]
    [MinValue(0.1f)]
    private float _fadeInDuration = 0.2f;

    [SerializeField, FoldoutGroup("Display Settings")]
    [Tooltip("Duration of fade out animation (in seconds)")]
    [MinValue(0.1f)]
    private float _fadeOutDuration = 0.3f;

    #endregion

    #region Runtime State

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private bool _isActive;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private float _nextBarkTime;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private string _lastBarkShown;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private int _totalBarksShown;

    private Coroutine _periodicBarkCoroutine;
    private Coroutine _displayCoroutine;
    private Tween _fadeTween;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Ensure bubble is hidden at start
        if (_bubbleContainer != null)
        {
            HideBubbleImmediate();
        }
    }

    private void OnEnable()
    {
        if (_autoStart)
        {
            StartBarking();
        }
    }

    private void OnDisable()
    {
        StopBarking();
    }

    private void OnDestroy()
    {
        KillFadeTween();
    }

    private void OnValidate()
    {
        // Ensure max is always >= min
        if (_maxBarkInterval < _minBarkInterval)
        {
            _maxBarkInterval = _minBarkInterval;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Start the periodic bark system.
    /// </summary>
    [Button("Start Barking"), FoldoutGroup("Debug")]
    public void StartBarking()
    {
        if (_isActive)
        {
            Debug.LogWarning($"[StandaloneBarkController] Already active on {gameObject.name}");
            return;
        }

        if (_barks == null || _barks.Count == 0)
        {
            Debug.LogError($"[StandaloneBarkController] No barks configured on {gameObject.name}");
            return;
        }

        if (_periodicBarkCoroutine != null)
            StopCoroutine(_periodicBarkCoroutine);

        _periodicBarkCoroutine = StartCoroutine(PeriodicBarkCoroutine());
        _isActive = true;

        Debug.Log($"[StandaloneBarkController] Started barking on {gameObject.name}");
    }

    /// <summary>
    /// Stop the periodic bark system.
    /// </summary>
    [Button("Stop Barking"), FoldoutGroup("Debug")]
    public void StopBarking()
    {
        if (_periodicBarkCoroutine != null)
        {
            StopCoroutine(_periodicBarkCoroutine);
            _periodicBarkCoroutine = null;
        }

        _isActive = false;
        HideBubble();

        Debug.Log($"[StandaloneBarkController] Stopped barking on {gameObject.name}");
    }

    /// <summary>
    /// Immediately show a random bark from the list.
    /// </summary>
    [Button("Force Random Bark"), FoldoutGroup("Debug")]
    public void ShowRandomBark()
    {
        if (_barks == null || _barks.Count == 0)
        {
            Debug.LogError($"[StandaloneBarkController] No barks configured on {gameObject.name}");
            return;
        }

        string bark = GetRandomBark();
        ShowBark(bark);
    }

    /// <summary>
    /// Show a specific bark text (not from the list).
    /// </summary>
    /// <param name="text">The text to display</param>
    public void ShowCustomBark(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[StandaloneBarkController] Attempted to show empty bark on {gameObject.name}");
            return;
        }

        ShowBark(text);
    }

    /// <summary>
    /// Add a new bark to the list at runtime.
    /// </summary>
    /// <param name="bark">The bark text to add</param>
    public void AddBark(string bark)
    {
        if (string.IsNullOrEmpty(bark))
            return;

        if (_barks == null)
            _barks = new List<string>();

        _barks.Add(bark);
        Debug.Log($"[StandaloneBarkController] Added new bark to {gameObject.name}: \"{bark}\"");
    }

    /// <summary>
    /// Clear all barks from the list.
    /// </summary>
    public void ClearBarks()
    {
        if (_barks == null)
            _barks = new List<string>();
        else
            _barks.Clear();

        Debug.Log($"[StandaloneBarkController] Cleared all barks on {gameObject.name}");
    }

    /// <summary>
    /// Replace the entire bark list.
    /// </summary>
    /// <param name="newBarks">The new list of barks</param>
    public void SetBarks(List<string> newBarks)
    {
        _barks = newBarks ?? new List<string>();
        Debug.Log($"[StandaloneBarkController] Set new bark list on {gameObject.name} ({_barks.Count} barks)");
    }

    #endregion

    #region Coroutines

    private IEnumerator PeriodicBarkCoroutine()
    {
        // Optional delay before first bark
        if (_delayFirstBark)
        {
            float initialDelay = Random.Range(_minBarkInterval * 0.5f, _minBarkInterval);
            _nextBarkTime = Time.time + initialDelay;
            yield return new WaitForSeconds(initialDelay);
        }

        // Main loop
        while (true)
        {
            // Show random bark
            ShowRandomBark();

            // Wait random interval before next bark
            float interval = Random.Range(_minBarkInterval, _maxBarkInterval);
            _nextBarkTime = Time.time + interval;
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(_displayDuration);
        HideBubble();
    }

    #endregion

    #region Display Logic

    private string GetRandomBark()
    {
        if (_barks == null || _barks.Count == 0)
            return "";

        return _barks[Random.Range(0, _barks.Count)];
    }

    private void ShowBark(string text)
    {
        // Validate references
        if (_bubbleContainer == null || _barkText == null || _canvasGroup == null)
        {
            Debug.LogError($"[StandaloneBarkController] Missing UI references on {gameObject.name}");
            return;
        }

        // Stop any existing display
        if (_displayCoroutine != null)
            StopCoroutine(_displayCoroutine);

        KillFadeTween();

        // Update bark text
        _barkText.text = text;
        _bubbleContainer.SetActive(true);

        // Track for debug
        _lastBarkShown = text;
        _totalBarksShown++;

        // Fade in animation
        _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration).From(0f);

        // Start hide timer
        _displayCoroutine = StartCoroutine(HideAfterDelay());

        Debug.Log($"[StandaloneBarkController] Showing bark on {gameObject.name}: \"{text}\"");
    }

    private void HideBubble()
    {
        if (_canvasGroup == null || _bubbleContainer == null)
            return;

        KillFadeTween();

        // Fade out animation
        _fadeTween = _canvasGroup.DOFade(0f, _fadeOutDuration).OnComplete(() =>
        {
            if (_bubbleContainer != null)
                _bubbleContainer.SetActive(false);
        });
    }

    private void HideBubbleImmediate()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (_bubbleContainer != null)
            _bubbleContainer.SetActive(false);
    }

    private void KillFadeTween()
    {
        if (_fadeTween != null && _fadeTween.IsActive())
        {
            _fadeTween.Kill();
        }
    }

    #endregion

    #region Debug Helpers

    [Button("Test Custom Bark"), FoldoutGroup("Debug")]
    private void DebugTestCustomBark(string testBark = "This is a test bark!")
    {
        ShowCustomBark(testBark);
    }

    [Button("Show All Barks (Sequential)"), FoldoutGroup("Debug")]
    private void DebugShowAllBarks()
    {
        if (_barks == null || _barks.Count == 0)
        {
            Debug.LogWarning($"[StandaloneBarkController] No barks to show on {gameObject.name}");
            return;
        }

        StartCoroutine(DebugShowAllBarksCoroutine());
    }

    private IEnumerator DebugShowAllBarksCoroutine()
    {
        bool wasActive = _isActive;
        if (wasActive)
            StopBarking();

        Debug.Log($"[StandaloneBarkController] Showing all {_barks.Count} barks sequentially...");

        foreach (string bark in _barks)
        {
            ShowBark(bark);
            yield return new WaitForSeconds(_displayDuration + _fadeInDuration + _fadeOutDuration + 0.5f);
        }

        Debug.Log($"[StandaloneBarkController] Finished showing all barks");

        if (wasActive)
            StartBarking();
    }

    [Button("Log Stats"), FoldoutGroup("Debug")]
    private void DebugLogStats()
    {
        Debug.Log($"=== STANDALONE BARK CONTROLLER STATS ({gameObject.name}) ===");
        Debug.Log($"Active: {_isActive}");
        Debug.Log($"Total Barks in List: {(_barks != null ? _barks.Count : 0)}");
        Debug.Log($"Total Barks Shown: {_totalBarksShown}");
        Debug.Log($"Last Bark: \"{_lastBarkShown}\"");
        Debug.Log($"Next Bark Time: {(_isActive ? _nextBarkTime.ToString("F2") : "N/A")}");
        Debug.Log($"Bark Interval: {_minBarkInterval}s - {_maxBarkInterval}s");
    }

    #endregion
}
