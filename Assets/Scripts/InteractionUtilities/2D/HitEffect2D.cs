using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class HitEffect2D : MonoBehaviour
{
    [FoldoutGroup("References")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [FoldoutGroup("Hit Effect Settings")]
    [SerializeField] private float _hitEffectTargetValue = 1f;

    [FoldoutGroup("Hit Effect Settings")]
    [SerializeField] private float _hitEffectDuration = 0.3f;

    [FoldoutGroup("Hit Effect Settings")]
    [SerializeField] private Ease _hitEffectEaseType = Ease.OutQuad;

    private const string HIT_EFFECT_KEYWORD = "HITEFFECT_ON";
    private const string HIT_EFFECT_BLEND_PROPERTY = "_HitEffectBlend";
    private Material _targetMaterial;
    private float _initialBlendValue;
    private Tween _currentTween;

    private void Awake()
    {
        CacheMaterialReference();
        CacheInitialBlendValue();
    }

    private void CacheMaterialReference()
    {
        if (_spriteRenderer != null)
        {
            _targetMaterial = _spriteRenderer.material;
        }
    }

    [FoldoutGroup("Controls")]
    [Button("Trigger Hit Effect", ButtonSizes.Large)]
    public void TriggerHitEffect()
    {
        PlayHitEffect();
    }

    private void CacheInitialBlendValue()
    {
        if (_targetMaterial != null)
        {
            _initialBlendValue = _targetMaterial.GetFloat(HIT_EFFECT_BLEND_PROPERTY);
        }
    }

    private void PlayHitEffect()
    {
        if (_targetMaterial == null)
        {
            Debug.LogWarning("Target Material is not assigned!");
            return;
        }

        KillCurrentTween();
        EnableHitEffectKeyword();

        _currentTween = CreateHitEffectSequence();
    }

    private void EnableHitEffectKeyword()
    {
        if (_targetMaterial != null)
        {
            _targetMaterial.EnableKeyword(HIT_EFFECT_KEYWORD);
        }
    }

    private void DisableHitEffectKeyword()
    {
        if (_targetMaterial != null)
        {
            _targetMaterial.DisableKeyword(HIT_EFFECT_KEYWORD);
        }
    }

    private Tween CreateHitEffectSequence()
    {
        Sequence hitEffectSequence = DOTween.Sequence();

        hitEffectSequence.Append(_targetMaterial.DOFloat(_hitEffectTargetValue, HIT_EFFECT_BLEND_PROPERTY, _hitEffectDuration)
            .SetEase(_hitEffectEaseType));

        hitEffectSequence.Append(_targetMaterial.DOFloat(_initialBlendValue, HIT_EFFECT_BLEND_PROPERTY, _hitEffectDuration)
            .SetEase(_hitEffectEaseType));

        hitEffectSequence.OnComplete(DisableHitEffectKeyword);

        return hitEffectSequence;
    }

    private void KillCurrentTween()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
            DisableHitEffectKeyword();
        }
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }

    // Getter functions
    public float GetCurrentBlendValue()
    {
        return _targetMaterial != null ? _targetMaterial.GetFloat(HIT_EFFECT_BLEND_PROPERTY) : 0f;
    }

    public bool IsHitEffectPlaying()
    {
        return _currentTween != null && _currentTween.IsActive();
    }
}