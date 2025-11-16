using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(EntityController))]
public class EntityNameController : HoverBase
{
    [SerializeField, FoldoutGroup("Identity")] private TextMeshProUGUI _nameText;
    [SerializeField, FoldoutGroup("Fade")] private CanvasGroup _nameFadeGroup;
    [SerializeField, FoldoutGroup("Fade")] private float _fadeDuration = 0.3f;
    [SerializeField, FoldoutGroup("Fade")] private float _fadeInAlpha = 1f;
    [SerializeField, FoldoutGroup("Fade")] private float _fadeOutAlpha = 0f;

    private EntityController _parentEntity;
    private Tween _currentFadeTween;

    private void Awake()
    {
        _parentEntity = GetComponent<EntityController>();
    }

    private void Start()
    {
        _nameFadeGroup.alpha = _fadeOutAlpha;
    }

    public override void OnHoverStart()
    {
        base.OnHoverStart();
        _nameText.text = _parentEntity.RuntimeData.entityName;
        FadeIn();
    }

    public override void OnHoverEnd()
    {
        base.OnHoverEnd();
        FadeOut();
    }

    private void FadeIn()
    {
        KillCurrentTween();
        _currentFadeTween = _nameFadeGroup.DOFade(_fadeInAlpha, _fadeDuration);
    }

    private void FadeOut()
    {
        KillCurrentTween();
        _currentFadeTween = _nameFadeGroup.DOFade(_fadeOutAlpha, _fadeDuration);
    }

    private void KillCurrentTween()
    {
        if (_currentFadeTween != null && _currentFadeTween.IsActive())
        {
            _currentFadeTween.Kill();
        }
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }
}