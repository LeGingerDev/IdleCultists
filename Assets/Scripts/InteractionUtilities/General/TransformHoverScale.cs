using LGD.Core;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class TransformHoverScale : HoverBase
{
    [FoldoutGroup("Target Settings")]
    [SerializeField] private bool _useCustomTarget = false;

    [FoldoutGroup("Target Settings")]
    [SerializeField, ShowIf("_useCustomTarget")] private Transform _target;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private float _hoverScale = 1.03f;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private float _scaleDuration = 0.2f;

    [FoldoutGroup("Scale Settings")]
    [SerializeField] private Ease _scaleEase = Ease.OutBack;

    private Vector3 _originalScale;
    private Tween _scaleTween;
    private Transform _scaleTransform;

    private void Awake()
    {
        _scaleTransform = _useCustomTarget ? _target : transform;
        _originalScale = _scaleTransform.localScale;
    }

    public override void OnHoverStart()
    {
        _scaleTween?.Kill();
        _scaleTween = _scaleTransform.DOScale(_originalScale * _hoverScale, _scaleDuration)
            .SetEase(_scaleEase);
    }

    public override void OnHoverEnd()
    {
        _scaleTween?.Kill();
        _scaleTween = _scaleTransform.DOScale(_originalScale, _scaleDuration)
            .SetEase(_scaleEase);
    }

    private void OnDestroy()
    {
        _scaleTween?.Kill();
    }
}
