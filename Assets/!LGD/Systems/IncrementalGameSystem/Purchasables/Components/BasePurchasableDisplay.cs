using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LGD.Core;

/// <summary>
/// Shared base for purchasable UI displays. Contains common serialized UI references,
/// Odin toggles (ShowIf) and runtime helpers (purchase loop, toggle application).
/// Concrete displays should inherit and implement the abstract UI/data hooks.
/// </summary>
public abstract class BasePurchasableDisplay : BaseBehaviour
{
    [SerializeField, FoldoutGroup("UI Options")]
    protected bool _showIcon = true;

    [SerializeField, FoldoutGroup("UI Options")]
    protected bool _showName = true;

    [SerializeField, FoldoutGroup("UI Options")]
    protected bool _showDescription = true;

    [SerializeField, FoldoutGroup("UI Options")]
    protected bool _showTimesPurchased = true;

    [SerializeField, FoldoutGroup("UI Options")]
    protected bool _showButton = true;

    [SerializeField, FoldoutGroup("UI Options")]
    protected bool _showCost = true;

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showIcon))]
    protected Image _iconImage;

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showName))]
    protected TextMeshProUGUI _displayNameText;

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showDescription))]
    protected TextMeshProUGUI _descriptionText;

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showTimesPurchased))]
    protected TextMeshProUGUI _timesPurchasedText;

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showButton))]
    protected Button _purchaseButton;

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showButton))]
    protected TextMeshProUGUI _buttonPurchaseText;

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showCost))]
    protected TextMeshProUGUI _costText;

    private Coroutine _canPurchaseLoopCoroutine;

    public virtual void Initialise()
    {
        ApplyUiToggles();
        SetupStaticUI();
        RefreshDynamicUI();
        HookUpButton();
        StartPurchaseLoop();
    }

    protected void ApplyUiToggles()
    {
        if (_iconImage != null)
            _iconImage.gameObject.SetActive(_showIcon);

        if (_displayNameText != null)
            _displayNameText.gameObject.SetActive(_showName);

        if (_descriptionText != null)
            _descriptionText.gameObject.SetActive(_showDescription);

        if (_timesPurchasedText != null)
            _timesPurchasedText.gameObject.SetActive(_showTimesPurchased);

        if (_purchaseButton != null)
            _purchaseButton.gameObject.SetActive(_showButton);

        if (_buttonPurchaseText != null)
            _buttonPurchaseText.gameObject.SetActive(_showButton);

        if (_costText != null)
            _costText.gameObject.SetActive(_showCost);
    }

    protected abstract void SetupStaticUI();

    protected abstract void RefreshDynamicUI();

    protected virtual void HookUpButton()
    {
        if (_showButton && _purchaseButton != null)
        {
            _purchaseButton.onClick.RemoveAllListeners();
            _purchaseButton.onClick.AddListener(() => OnPurchaseClicked());
        }
    }

    protected abstract void OnPurchaseClicked();

    protected abstract bool CanPurchase();

    protected abstract string GetButtonText();

    public IEnumerator CanPurchaseLoop()
    {
        while (true)
        {
            CanPurchaseSet();
            yield return new WaitForSeconds(0.2f);
        }
    }

    public virtual void CanPurchaseSet()
    {
        if (_showButton && _purchaseButton != null)
            _purchaseButton.interactable = CanPurchase();

        if (_showButton && _buttonPurchaseText != null)
            _buttonPurchaseText.text = GetButtonText();
    }

    public void StartPurchaseLoop()
    {
        if (_canPurchaseLoopCoroutine == null)
        {
            _canPurchaseLoopCoroutine = StartCoroutine(CanPurchaseLoop());
        }
    }

    public void StopPurchaseLoop()
    {
        if (_canPurchaseLoopCoroutine != null)
        {
            StopCoroutine(_canPurchaseLoopCoroutine);
            _canPurchaseLoopCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        StopPurchaseLoop();
    }

    protected string GetTimesPurchasedDisplayText(int timesPurchased)
    {
        if (timesPurchased == 0)
            return "Not Yet Purchased";

        return $"Purchased {timesPurchased} time{(timesPurchased == 1 ? "" : "s")}";
    }
}
