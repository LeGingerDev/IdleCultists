using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightPulse2D : MonoBehaviour
{
    [FoldoutGroup("References")]
    [SerializeField] private Light2D _light2D;

    [FoldoutGroup("Pulse Settings")]
    [SerializeField] private float _targetIntensity = 2f;

    [FoldoutGroup("Pulse Settings")]
    [SerializeField] private float _pulseDuration = 0.5f;

    [FoldoutGroup("Pulse Settings")]
    [SerializeField] private Ease _pulseEaseType = Ease.InOutSine;

    private float _initialIntensity;
    private Tween _currentTween;

    private void Awake()
    {
        CacheLightReference();
        CacheInitialIntensity();
    }

    private void CacheLightReference()
    {
        if (_light2D == null)
        {
            _light2D = GetComponent<Light2D>();
        }
    }

    [FoldoutGroup("Controls")]
    [Button("Trigger Pulse Effect", ButtonSizes.Large)]
    public void TriggerPulseEffect()
    {
        PlayPulseEffect();
    }

    private void CacheInitialIntensity()
    {
        if (_light2D != null)
        {
            _initialIntensity = _light2D.intensity;
        }
    }

    private void PlayPulseEffect()
    {
        if (_light2D == null)
        {
            Debug.LogWarning("Light2D is not assigned!");
            return;
        }

        KillCurrentTween();
        _currentTween = CreatePulseSequence();
    }

    private Tween CreatePulseSequence()
    {
        Sequence pulseSequence = DOTween.Sequence();

        pulseSequence.Append(DOTween.To(() => _light2D.intensity, x => _light2D.intensity = x, _targetIntensity, _pulseDuration)
            .SetEase(_pulseEaseType)
            .SetTarget(_light2D));

        pulseSequence.Append(DOTween.To(() => _light2D.intensity, x => _light2D.intensity = x, _initialIntensity, _pulseDuration)
            .SetEase(_pulseEaseType)
            .SetTarget(_light2D));

        return pulseSequence;
    }

    private void KillCurrentTween()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
        }
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }

    // Getter functions
    public float GetCurrentIntensity()
    {
        return _light2D != null ? _light2D.intensity : 0f;
    }

    public bool IsPulseEffectPlaying()
    {
        return _currentTween != null && _currentTween.IsActive();
    }

    public float GetInitialIntensity()
    {
        return _initialIntensity;
    }
}