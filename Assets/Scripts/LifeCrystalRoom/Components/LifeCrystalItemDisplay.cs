using DG.Tweening;
using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unified display component for LifeCrystal purchasables (both StatPurchasables and EventPurchasables)
/// Replaces both LifeCrystalUpgradeDisplay and LifeCrystalPurchasableDisplay
/// </summary>
public class LifeCrystalItemDisplay : BaseBehaviour
{
    [SerializeField, FoldoutGroup("Blueprint")]
    private BasePurchasable _purchasableBlueprint;

    [SerializeField, FoldoutGroup("UI References")]
    private Image _displayIcon;

    [SerializeField, FoldoutGroup("UI References")]
    private Outline _outline;

    [SerializeField, FoldoutGroup("UI References")]
    private Button _button;

    [SerializeField, FoldoutGroup("UI References")]
    private CanvasGroup _canvasGroup;

    [SerializeField, FoldoutGroup("Settings")]
    private float _fadeDuration = 0.5f;

    private bool _isSelected = false;
    private bool _isVisible = false;
    private Tween _fadeTween;

    private void Start()
    {
        if (_purchasableBlueprint != null)
        {
            Initialise();
        }
    }

    [Button]
    private void Initialise()
    {
        SetupUI();
        HookUpButton();

        // Set initial visibility based on prerequisites
        bool shouldBeVisible = ArePrerequisitesMet();
        SetVisibilityImmediate(shouldBeVisible);
    }

    private void SetupUI()
    {
        if (_purchasableBlueprint.icon != null)
        {
            _displayIcon.sprite = _purchasableBlueprint.icon;
        }

        if (_outline != null)
        {
            _outline.enabled = false;
        }

        // Ensure CanvasGroup exists
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogWarning($"[{GetType().Name}] No CanvasGroup found on {gameObject.name}. Adding one.");
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void HookUpButton()
    {
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnDisplayClicked);
    }

    private void OnDisplayClicked()
    {
        // Only allow clicking if visible
        if (!_isVisible)
            return;

        // Publish unified event with the purchasable blueprint
        Publish(LifeCrystalEventIds.ON_PURCHASABLE_SELECTED, _purchasableBlueprint);
    }

    public void Refresh()
    {
        bool shouldBeVisible = ArePrerequisitesMet();

        if (shouldBeVisible && !_isVisible)
        {
            FadeIn();
        }
        else if (!shouldBeVisible && _isVisible)
        {
            FadeOut();
        }
    }

    private void SetVisibilityImmediate(bool visible)
    {
        _isVisible = visible;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }

    private void FadeIn()
    {
        _isVisible = true;

        // Kill existing tween
        _fadeTween?.Kill();

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration)
                .SetEase(Ease.OutQuad);
        }

        // Determine type for logging
        string itemType = _purchasableBlueprint is StatPurchasable ? "stat purchasable" : "event purchasable";
        Debug.Log($"<color=green>[LifeCrystal] Revealed {itemType}:</color> {_purchasableBlueprint.displayName}");
    }

    private void FadeOut()
    {
        _isVisible = false;

        // Kill existing tween
        _fadeTween?.Kill();

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (_outline != null)
        {
            _outline.enabled = selected;
        }
    }

    private bool ArePrerequisitesMet()
    {
        if (_purchasableBlueprint == null)
            return false;

        return _purchasableBlueprint.ArePrerequisitesMet();
    }

    public BasePurchasable GetBlueprint()
    {
        return _purchasableBlueprint;
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
    }

    // Deselect when another item is selected
    [Topic(LifeCrystalEventIds.ON_PURCHASABLE_SELECTED)]
    public void OnAnyPurchasableSelected(object sender, BasePurchasable blueprint)
    {
        if (blueprint != _purchasableBlueprint)
        {
            SetSelected(false);
        }
        else
        {
            SetSelected(true);
        }
    }

    // Listen for any purchase to check if we should reveal
    [Topic(LifeCrystalEventIds.ON_LIFE_CRYSTAL_PURCHASE_COMPLETED)]
    public void OnPurchaseCompleted(object sender)
    {
        Refresh();
    }
}
