using DG.Tweening;
using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Beat-synced pulse effect for visual elements
/// Pulses transforms in sync with music beats from the Boombox
/// </summary>
public class BeatPulseEffect : BaseBehaviour
{
    [FoldoutGroup("Targets"), Required]
    [Tooltip("List of transforms to pulse on each beat")]
    [SerializeField] private List<Transform> targetTransforms = new List<Transform>();

    [FoldoutGroup("Pulse Settings")]
    [Tooltip("Scale multiplier for pulse (1.1 = 10% larger)")]
    [Range(1.0f, 2.0f)]
    [SerializeField] private float pulseScale = 1.1f;

    [FoldoutGroup("Pulse Settings")]
    [Tooltip("Duration of the pulse animation (in seconds)")]
    [Range(0.05f, 1.0f)]
    [SerializeField] private float pulseDuration = 0.1f;

    [FoldoutGroup("Pulse Settings")]
    [Tooltip("Easing curve for the pulse animation")]
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [FoldoutGroup("Settings")]
    [Tooltip("If true, effect is active. If false, no pulsing occurs")]
    [SerializeField] private bool isActive = true;

    // Store active tweens for each transform
    private Dictionary<Transform, Tween> _activeTweens = new Dictionary<Transform, Tween>();

    private void OnValidate()
    {
        // If targetTransforms list is empty, add this transform as default
        if (targetTransforms == null || targetTransforms.Count == 0)
        {
            targetTransforms = new List<Transform> { transform };
        }
    }

    #region Event Listeners

    [Topic(BoomboxEventIds.ON_BEAT)]
    public void OnBeat(object sender, float bpm)
    {
        if (!isActive)
            return;

        // Pulse all target transforms
        foreach (var target in targetTransforms)
        {
            if (target != null)
            {
                PulseTransform(target);
            }
        }
    }

    [Topic(BoomboxEventIds.ON_BEAT_RESYNC)]
    public void OnBeatResync(object sender)
    {
        if (!isActive)
            return;

        // Stop all active tweens and restart them
        // This helps prevent drift accumulation
        foreach (var target in targetTransforms)
        {
            if (target != null)
            {
                StopPulse(target);
            }
        }
    }

    [Topic(BoomboxEventIds.ON_TRACK_STOPPED)]
    public void OnTrackStopped(object sender)
    {
        // Clean up all tweens when music stops
        StopAllPulses();
    }

    #endregion

    #region Pulse Logic

    /// <summary>
    /// Pulse a single transform
    /// </summary>
    private void PulseTransform(Transform target)
    {
        if (target == null)
            return;

        // Kill any existing tween for this transform
        StopPulse(target);

        // Create punch scale effect (subtle bounce)
        Vector3 punchAmount = Vector3.one * (pulseScale - 1f);
        Tween tween = target.DOPunchScale(punchAmount, pulseDuration, 1, 0.5f)
            .SetEase(easeType);

        // Store the tween
        _activeTweens[target] = tween;
    }

    /// <summary>
    /// Stop pulse for a single transform
    /// </summary>
    private void StopPulse(Transform target)
    {
        if (target == null)
            return;

        if (_activeTweens.TryGetValue(target, out Tween tween))
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
            _activeTweens.Remove(target);
        }

        // Reset scale to original
        target.localScale = Vector3.one;
    }

    /// <summary>
    /// Stop all active pulses
    /// </summary>
    private void StopAllPulses()
    {
        foreach (var target in targetTransforms)
        {
            if (target != null)
            {
                StopPulse(target);
            }
        }
        _activeTweens.Clear();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Enable or disable the effect
    /// </summary>
    public void ToggleSetActive(bool active)
    {
        isActive = active;

        if (!active)
        {
            StopAllPulses();
        }
    }

    /// <summary>
    /// Add a transform to the pulse list
    /// </summary>
    public void AddTarget(Transform target)
    {
        if (target != null && !targetTransforms.Contains(target))
        {
            targetTransforms.Add(target);
        }
    }

    /// <summary>
    /// Remove a transform from the pulse list
    /// </summary>
    public void RemoveTarget(Transform target)
    {
        if (target != null)
        {
            StopPulse(target);
            targetTransforms.Remove(target);
        }
    }

    #endregion

    #region Cleanup

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllPulses();
    }

    private void OnDestroy()
    {
        StopAllPulses();
    }

    #endregion

    #region Editor

#if UNITY_EDITOR
    [Button("Test Pulse"), FoldoutGroup("Testing")]
    private void TestPulse()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter play mode to test");
            return;
        }

        foreach (var target in targetTransforms)
        {
            if (target != null)
            {
                PulseTransform(target);
            }
        }
    }

    [Button("Stop All Pulses"), FoldoutGroup("Testing")]
    private void TestStopAll()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter play mode to test");
            return;
        }

        StopAllPulses();
    }
#endif

    #endregion
}
