using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LargeNumbers;
using LGD.Core;
using LGD.ResourceSystem.Models;
using LGD.Extensions;
using LGD.Core.Events;

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

    [SerializeField, FoldoutGroup("UI Options")]
    protected bool _showModifierGain = true;

    [SerializeField, FoldoutGroup("Refresh Settings")]
    [Tooltip("How often to refresh UI state (in seconds). Set to 0 to disable periodic refresh.")]
    [Range(0f, 1f)]
    protected float _refreshInterval = 0.2f;

    [SerializeField, FoldoutGroup("Fade Controls")]
    [Tooltip("Should this display fade out when prerequisites aren't met?")]
    protected bool _fadeWhenLocked = true;

    [SerializeField, FoldoutGroup("Fade Controls")]
    [Tooltip("Should this display ignore layout when prerequisites aren't met? (Makes it take no space)")]
    protected bool _ignoreLayoutWhenLocked = true;

    [SerializeField, FoldoutGroup("Fade Controls")]
    [Tooltip("Alpha value when faded (0 = invisible, 1 = fully visible)")]
    [Range(0f, 1f)]
    protected float _fadedAlpha = 0f;

    [SerializeField, FoldoutGroup("Fade Controls")]
    [Tooltip("Alpha value when visible")]
    [Range(0f, 1f)]
    protected float _visibleAlpha = 1f;

    [SerializeField, FoldoutGroup("Fade Controls")]
    [Tooltip("Canvas Group component for fading (will be automatically added if missing)")]
    protected CanvasGroup _canvasGroup;

    [SerializeField, FoldoutGroup("Fade Controls")]
    [Tooltip("Layout Element component for ignoring layout (will be automatically added if missing)")]
    protected LayoutElement _layoutElement;

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

    [SerializeField, FoldoutGroup("UI References"), ShowIf(nameof(_showModifierGain))]
    protected TextMeshProUGUI _modifierGainText;

    [SerializeField, FoldoutGroup("UI References")]
    [Tooltip("Optional: Container GameObject for the modifier text (will be toggled based on _showModifierGain)")]
    protected GameObject _modifierGainContainer;

    private Coroutine _canPurchaseLoopCoroutine;
    private Coroutine _periodicRefreshCoroutine;

    // Allow derived classes to expose the blueprint this display represents.
    // Concrete displays should override this to return their blueprint field.
    protected virtual BasePurchasable GetDisplayedBlueprint() { return null; }

    protected virtual void OnEnable()
    {
        base.OnEnable(); // This registers event handlers with ServiceBus
        DebugManager.Log($"[IncrementalGame] OnEnable called for {this.GetType().Name} on {gameObject.name} - Event handlers should now be registered");
    }

    protected virtual void OnDisable()
    {
        base.OnDisable(); // This unregisters event handlers
        DebugManager.Log($"[IncrementalGame] OnDisable called for {this.GetType().Name} on {gameObject.name} - Event handlers unregistered");
    }

    public virtual void Initialise()
    {
        DebugManager.Log($"[IncrementalGame] Initialising BasePurchasableDisplay on {gameObject.name}");

        SetupFadeComponents();
        ApplyUiToggles();
        SetupStaticUI();

        // If PurchasableManager is already initialized, refresh immediately
        // Otherwise, wait for ON_PURCHASABLES_INITIALIZED event
        if (PurchasableManager.Instance != null && PurchasableManager.Instance.IsInitialized())
        {
            DebugManager.Log($"[IncrementalGame] PurchasableManager already initialized, refreshing UI immediately");
            RefreshDynamicUI();
        }
        else
        {
            DebugManager.Log($"[IncrementalGame] Waiting for PurchasableManager to initialize...");
            // Apply initial visibility state even before data is loaded
            ApplyPrerequisiteVisibility();
        }

        HookUpButton();
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

        if (_modifierGainText != null)
            _modifierGainText.gameObject.SetActive(_showModifierGain);

        if (_modifierGainContainer != null)
            _modifierGainContainer.SetActive(_showModifierGain);
    }

    /// <summary>
    /// Ensures CanvasGroup and LayoutElement components exist if fade controls are enabled
    /// </summary>
    protected void SetupFadeComponents()
    {
        if (_fadeWhenLocked && _canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_ignoreLayoutWhenLocked && _layoutElement == null)
        {
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
            }
        }
    }

    /// <summary>
    /// Applies fade and layout visibility based on whether prerequisites are met
    /// </summary>
    protected void ApplyPrerequisiteVisibility()
    {
        var blueprint = GetDisplayedBlueprint();

        if (blueprint == null)
        {
            return;
        }

        bool prerequisitesMet = blueprint.ArePrerequisitesMet();

        // Apply fade if enabled
        if (_fadeWhenLocked && _canvasGroup != null)
        {
            _canvasGroup.alpha = prerequisitesMet ? _visibleAlpha : _fadedAlpha;
        }

        // Apply layout ignore if enabled
        if (_ignoreLayoutWhenLocked && _layoutElement != null)
        {
            _layoutElement.ignoreLayout = !prerequisitesMet;
        }
    }

    protected virtual void SetupStaticUI()
    {
        var blueprint = GetDisplayedBlueprint();

        if (_showIcon && _iconImage != null)
            _iconImage.sprite = blueprint != null ? blueprint.icon : null;

        if (_showName && _displayNameText != null)
            _displayNameText.text = blueprint != null ? blueprint.displayName : string.Empty;

        if (_showDescription && _descriptionText != null)
            _descriptionText.text = blueprint != null ? blueprint.description : string.Empty;

        // Allow derived classes to populate additional static UI elements
        SetupAdditionalStaticUI();
    }

    /// <summary>
    /// Hook for derived classes to populate additional static UI (e.g. gain text, timer bars).
    /// Called after base static UI (icon/name/description) is applied.
    /// </summary>
    protected virtual void SetupAdditionalStaticUI() { }

    protected virtual void RefreshDynamicUI()
    {
        // Base dynamic UI updates: times purchased and cost text
        var blueprint = GetDisplayedBlueprint();

        if (blueprint == null)
        {
            return;
        }

        // Apply prerequisite-based visibility first
        ApplyPrerequisiteVisibility();

        int timesPurchased = blueprint.GetPurchaseCount();

        string timesText = GetTimesPurchasedDisplayText(timesPurchased);
        if (_showTimesPurchased && _timesPurchasedText != null)
        {
            _timesPurchasedText.text = timesText;
        }

        bool isMaxed = blueprint.IsMaxedOut();

        if (_showCost && _costText != null)
        {
            _costText.gameObject.SetActive(!isMaxed);

            if (!isMaxed)
            {
                ResourceAmountPair cost = blueprint.GetCurrentCostSafe();
                string costString = GetCostDisplayText(cost);
                _costText.text = costString;
            }
        }

        // Update modifier gain text (visible for all StatPurchasables, showing progression or final state)
        if (_showModifierGain && _modifierGainText != null)
        {
            bool shouldShowModifier = blueprint is StatPurchasable;

            _modifierGainText.gameObject.SetActive(shouldShowModifier);

            if (_modifierGainContainer != null)
            {
                _modifierGainContainer.SetActive(shouldShowModifier);
            }

            if (shouldShowModifier)
            {
                string modifierText = GetModifierGainDisplayText(isMaxed);
                _modifierGainText.text = modifierText;
            }
        }

        CanPurchaseSet();
    }

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

    /// <summary>
    /// Default button text logic for purchasables. Concrete displays can override this
    /// to provide custom wording (e.g. "Summon" instead of "Purchase").
    /// Default states:
    /// - "Maxxed": when purchasable is maxed out
    /// - "Can't Afford": when player cannot afford the next purchase
    /// - "Purchase": when available to buy
    /// </summary>
    protected virtual string GetButtonText()
    {
        var blueprint = GetDisplayedBlueprint();
        if (blueprint == null)
            return string.Empty;

        if (blueprint.IsMaxedOut())
            return "Maxxed";

        if (!blueprint.CanAfford())
            return "Can't Afford";

        return "Purchase";
    }

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
        var blueprint = GetDisplayedBlueprint();
        bool canPurchase = blueprint != null && !blueprint.IsMaxedOut() && CanPurchase();

        if (_showButton && _purchaseButton != null)
            _purchaseButton.interactable = canPurchase;

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

    /// <summary>
    /// Start the periodic refresh coroutine
    /// Should be called by parent panels when they open (e.g., PurchasablePanel.OnOpen)
    /// </summary>
    public void StartPeriodicRefresh()
    {
        if (_refreshInterval <= 0f)
        {
            return; // Periodic refresh disabled
        }

        StopPeriodicRefresh(); // Stop existing coroutine if any
        _periodicRefreshCoroutine = StartCoroutine(PeriodicRefreshCoroutine());
    }

    /// <summary>
    /// Stop the periodic refresh coroutine
    /// Should be called by parent panels when they close (e.g., PurchasablePanel.OnClose)
    /// </summary>
    public void StopPeriodicRefresh()
    {
        if (_periodicRefreshCoroutine != null)
        {
            StopCoroutine(_periodicRefreshCoroutine);
            _periodicRefreshCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that periodically refreshes the display
    /// This ensures UI updates when resources change (e.g., becoming unaffordable after purchase)
    /// Managed by parent panels via StartPeriodicRefresh/StopPeriodicRefresh
    /// </summary>
    private IEnumerator PeriodicRefreshCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_refreshInterval);

        while (true)
        {
            yield return wait;
            RefreshDynamicUI();
        }
    }

    private void OnDestroy()
    {
        StopPurchaseLoop();
        StopPeriodicRefresh();
    }

    [Topic(PurchasableEventIds.ON_PURCHASABLE_PURCHASED)]
    public void OnPurchasablePurchased(object sender, BasePurchasable blueprint, BasePurchasableRuntimeData runtimeData)
    {
        var myBlueprint = GetDisplayedBlueprint();

        if (myBlueprint == null)
        {
            return;
        }

        if (blueprint == null)
        {
            return;
        }

        if (myBlueprint.purchasableId == blueprint.purchasableId)
        {
            // Refresh UI when the purchasable this display represents was bought
            RefreshDynamicUI();
        }
        else
        {
            // Also refresh visibility when ANY purchasable is purchased
            // This ensures displays update when their prerequisites are met
            ApplyPrerequisiteVisibility();
        }
    }

    [Topic(PurchasableEventIds.ON_PURCHASABLES_INITIALIZED)]
    public void OnPurchasablesInitialized(object sender)
    {
        // Re-evaluate dynamic UI after purchasable runtime data is loaded
        RefreshDynamicUI();
    }

    protected string GetCostDisplayText(ResourceAmountPair cost)
    {
        if (cost == null || cost.amount.isZero)
            return "Free";

        return $"{cost.resource.GetResourceSpriteText()} {cost.amount.FormatWithDecimals()}";
    }

    protected string GetModifierGainDisplayText(bool isMaxed)
    {
        BasePurchasable blueprint = GetDisplayedBlueprint();

        // Only StatPurchasables provide modifiers
        if (blueprint is StatPurchasable statPurchasable)
        {
            if (isMaxed)
            {
                // Show current (max) modifiers when maxed
                List<StatModifier> currentModifiers = statPurchasable.GetCurrentModifiers();

                if (currentModifiers == null || currentModifiers.Count == 0)
                {
                    return string.Empty;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                foreach (var modifier in currentModifiers)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("\n");
                    }

                    string statName = PurchasableExtensions.FormatStatName(modifier.statType);
                    string valueText;

                    if (modifier.modifierType == ModifierType.Multiplicative)
                    {
                        valueText = $"+{modifier.multiplicativeValue.FormatAsPercentageCompact()}";
                    }
                    else // Additive
                    {
                        valueText = modifier.additiveValue.ToCompactString();
                    }

                    sb.Append($"{statName}: {valueText}");
                }

                return sb.ToString();
            }
            else
            {
                // Get current and next tier modifiers for progression display
                List<StatModifier> currentModifiers = statPurchasable.GetCurrentModifiers();
                List<StatModifier> nextModifiers = statPurchasable.GetNextModifiers();

                if (nextModifiers == null || nextModifiers.Count == 0)
                {
                    return string.Empty;
                }

                // Format each modifier as progression
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                foreach (var nextModifier in nextModifiers)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("\n");
                    }

                    // Find the corresponding current modifier for this stat type
                    StatModifier currentModifier = currentModifiers?.Find(m => m.statType == nextModifier.statType);

                    string statName = PurchasableExtensions.FormatStatName(nextModifier.statType);
                    string currentValueText;
                    string nextValueText;

                    if (nextModifier.modifierType == ModifierType.Multiplicative)
                    {
                        float currentValue = currentModifier != null ? currentModifier.multiplicativeValue : 0f;
                        currentValueText = $"+{currentValue.FormatAsPercentageCompact()}";
                        nextValueText = $"+{nextModifier.multiplicativeValue.FormatAsPercentageCompact()}";
                    }
                    else // Additive
                    {
                        AlphabeticNotation currentValue = currentModifier != null ? currentModifier.additiveValue : AlphabeticNotation.zero;
                        currentValueText = currentValue.ToCompactString();
                        nextValueText = nextModifier.additiveValue.ToCompactString();
                    }

                    sb.Append($"{statName}: {currentValueText} -> {nextValueText}");
                }

                return sb.ToString();
            }
        }

        // EventPurchasables don't have modifiers
        return string.Empty;
    }

    protected string GetTimesPurchasedDisplayText(int timesPurchased)
    {
        BasePurchasable blueprint = GetDisplayedBlueprint();

        if (blueprint.IsMaxedOut())
            return "Maxxed";

        if (blueprint.purchaseType == PurchaseType.OneTime)
            return timesPurchased == 0 ? "Not Purchased" : "Purchased";

        if (blueprint.purchaseType == PurchaseType.Infinite)
            return $"Lv.{timesPurchased}";

        return $"Lv.{timesPurchased}/{blueprint.maxPurchases}";
    }
}