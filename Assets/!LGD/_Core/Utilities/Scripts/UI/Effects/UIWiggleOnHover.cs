using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Sirenix.OdinInspector;

public class UIWiggleOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [FoldoutGroup("Target Settings")]
    [SerializeField] private bool _useCustomTarget;

    [FoldoutGroup("Target Settings")]
    [SerializeField, ShowIf(nameof(_useCustomTarget))]
    private RectTransform _customTarget;

    [FoldoutGroup("Wiggle Settings")]
    [SerializeField] private float _wiggleDuration = 0.3f;

    [FoldoutGroup("Wiggle Settings")]
    [SerializeField] private float _wiggleStrength = 15f;

    [FoldoutGroup("Wiggle Settings")]
    [SerializeField] private int _wiggleVibrato = 10;

    [FoldoutGroup("Wiggle Settings")]
    [SerializeField] private float _wiggleRandomness = 90f;

    private RectTransform _targetTransform;
    private Tween _currentWiggleTween;

    private void Awake()
    {
        SetupTarget();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayWiggle();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopWiggle();
    }

    private void SetupTarget()
    {
        _targetTransform = _useCustomTarget ? _customTarget : GetComponent<RectTransform>();
    }

    private void PlayWiggle()
    {
        KillCurrentTween();

        _currentWiggleTween = _targetTransform
            .DOPunchRotation(
                new Vector3(0, 0, _wiggleStrength),
                _wiggleDuration,
                _wiggleVibrato,
                _wiggleRandomness
            )
            .SetUpdate(true);
    }

    private void StopWiggle()
    {
        KillCurrentTween();
        ResetRotation();
    }

    private void KillCurrentTween()
    {
        if (_currentWiggleTween != null && _currentWiggleTween.IsActive())
        {
            _currentWiggleTween.Kill();
            _currentWiggleTween = null;
        }
    }

    private void ResetRotation()
    {
        if (_targetTransform != null)
        {
            _targetTransform.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }

    // Public getter if you need to check wiggle state from outside
    public bool IsWiggling()
    {
        return _currentWiggleTween != null && _currentWiggleTween.IsActive();
    }
}