using LGD.Core;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using LGD.InteractionSystem;

public class TransformClickScale : ClickBase
{
    [FoldoutGroup("Target Settings")]
    [SerializeField] private bool _useCustomTarget = false;

    [FoldoutGroup("Target Settings")]
    [SerializeField, ShowIf("_useCustomTarget")] private Transform _target;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private float _pressScale = 0.97f;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private float _pressDuration = 0.1f;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private Ease _pressEase = Ease.OutQuad;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private float _releaseDuration = 0.15f;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private Ease _releaseEase = Ease.OutBack;

    private Vector3 _originalScale;
    private Tween _scaleTween;
    private Transform _scaleTransform;

    private void Awake()
    {
        _scaleTransform = _useCustomTarget ? _target : transform;
        _originalScale = _scaleTransform.localScale;
    }

    public override void OnMouseDownEvent(InteractionData clickData)
    {
        _scaleTween?.Kill();
        _scaleTween = _scaleTransform.DOScale(_originalScale * _pressScale, _pressDuration)
            .SetEase(_pressEase);
    }

    public override void OnMouseUpEvent(InteractionData clickData)
    {
        _scaleTween?.Kill();
        _scaleTween = _scaleTransform.DOScale(_originalScale, _releaseDuration)
            .SetEase(_releaseEase);
    }

    private void OnDestroy()
    {
        _scaleTween?.Kill();
    }
}