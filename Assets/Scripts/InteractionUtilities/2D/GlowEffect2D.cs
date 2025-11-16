using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class GlowEffect2D : MonoBehaviour
{
    [FoldoutGroup("References")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [FoldoutGroup("Glow Settings")]
    [SerializeField] private float _glowTargetValue = 1f;

    [FoldoutGroup("Glow Settings")]
    [SerializeField] private float _glowDuration = 0.5f;

    [FoldoutGroup("Glow Settings")]
    [SerializeField] private Ease _glowEaseType = Ease.InOutSine;

    private const string GLOW_PROPERTY = "_Glow";
    private Material _targetMaterial;
    private float _initialGlowValue;
    private Tween _currentTween;

    private void Awake()
    {
        CacheMaterialReference();
        CacheInitialGlowValue();
    }

    private void CacheMaterialReference()
    {
        if (_spriteRenderer != null)
        {
            _targetMaterial = _spriteRenderer.material;
        }
    }

    [FoldoutGroup("Controls")]
    [Button("Trigger Glow Effect", ButtonSizes.Large)]
    public void TriggerGlowEffect()
    {
        PlayGlowEffect();
    }

    private void CacheInitialGlowValue()
    {
        if (_targetMaterial != null)
        {
            _initialGlowValue = _targetMaterial.GetFloat(GLOW_PROPERTY);
        }
    }

    private void PlayGlowEffect()
    {
        if (_targetMaterial == null)
        {
            Debug.LogWarning("Target Material is not assigned!");
            return;
        }

        KillCurrentTween();

        _currentTween = CreateGlowSequence();
    }

    private Tween CreateGlowSequence()
    {
        Sequence glowSequence = DOTween.Sequence();

        glowSequence.Append(_targetMaterial.DOFloat(_glowTargetValue, GLOW_PROPERTY, _glowDuration)
            .SetEase(_glowEaseType));

        glowSequence.Append(_targetMaterial.DOFloat(_initialGlowValue, GLOW_PROPERTY, _glowDuration)
            .SetEase(_glowEaseType));

        return glowSequence;
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
    public float GetCurrentGlowValue()
    {
        return _targetMaterial != null ? _targetMaterial.GetFloat(GLOW_PROPERTY) : 0f;
    }

    public bool IsGlowEffectPlaying()
    {
        return _currentTween != null && _currentTween.IsActive();
    }
}
