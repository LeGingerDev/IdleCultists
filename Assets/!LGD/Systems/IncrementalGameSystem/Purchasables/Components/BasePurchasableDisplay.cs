using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LGD.Core;
using LargeNumbers;
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

    [SerializeField, FoldoutGroup("Refresh Settings")]
    [Tooltip("How often to refresh UI state (in seconds). Set to 0 to disable periodic refresh.")]
    [Range(0f, 1f)]
    protected float _refreshInterval = 0.2f;

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
    private Coroutine _periodicRefreshCoroutine;

    // Allow derived classes to expose the blueprint this display represents.
    // Concrete displays should override this to return their blueprint field.
    protected virtual BasePurchasable GetDisplayedBlueprint() { return null; }

    protected virtual void OnEnable()
    {
        base.OnEnable(); // This registers event handlers with ServiceBus
        DebugManager.Log($"[IncrementalGame] OnEnable called for {this.GetType().Name} on {gameObject.name} - Event handlers should now be registered");
        StartPeriodicRefresh();
    }

    protected virtual void OnDisable()
    {
        base.OnDisable(); // This unregisters event handlers
        DebugManager.Log($"[IncrementalGame] OnDisable called for {this.GetType().Name} on {gameObject.name} - Event handlers unregistered");
        StopPeriodicRefresh();
    }

    public virtual void Initialise()
    {
        DebugManager.Log($"[IncrementalGame] Initialising BasePurchasableDisplay on {gameObject.name}");
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
        }

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
        DebugManager.Log($"[IncrementalGame] ========== RefreshDynamicUI CALLED on {gameObject.name} ({this.GetType().Name}) ==========");

        // Base dynamic UI updates: times purchased and cost text
        var blueprint = GetDisplayedBlueprint();

        if (blueprint == null)
        {
            DebugManager.Warning($"[IncrementalGame] RefreshDynamicUI: blueprint is NULL on {gameObject.name}");
            return;
        }

        int timesPurchased = blueprint.GetPurchaseCount();
        DebugManager.Log($"[IncrementalGame] Blueprint {blueprint.purchasableId} has been purchased {timesPurchased} times");

        string timesText = GetTimesPurchasedDisplayText(timesPurchased);
        if (_showTimesPurchased && _timesPurchasedText != null)
        {
            DebugManager.Log($"[IncrementalGame] Setting timesPurchasedText on {gameObject.name}: '{timesText}'");
            _timesPurchasedText.text = timesText;
        }
        else if (_showTimesPurchased && _timesPurchasedText == null)
        {
            DebugManager.Warning($"[IncrementalGame] timesPurchasedText is NULL on {gameObject.name} but _showTimesPurchased is TRUE");
        }

        if (_showCost)
        {
            ResourceAmountPair cost = blueprint.GetCurrentCostSafe();
            string costString = GetCostDisplayText(cost);
            if (_costText != null)
            {
                DebugManager.Log($"[IncrementalGame] Setting costText on {gameObject.name}: '{costString}'");
                _costText.text = costString;
            }
            else
            {
                DebugManager.Warning($"[IncrementalGame] costText is NULL on {gameObject.name} but _showCost is TRUE");
            }
        }

        CanPurchaseSet();
        DebugManager.Log($"[IncrementalGame] ========== RefreshDynamicUI COMPLETED on {gameObject.name} ==========");
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

    /// <summary>
    /// Start the periodic refresh coroutine (only when display is active)
    /// </summary>
    private void StartPeriodicRefresh()
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
    /// </summary>
    private void StopPeriodicRefresh()
    {
        if (_periodicRefreshCoroutine != null)
        {
            StopCoroutine(_periodicRefreshCoroutine);
            _periodicRefreshCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that periodically refreshes the display
    /// This ensures the UI updates when resources change off-screen
    /// Only runs when the display is active in hierarchy (OnEnable/OnDisable manages this)
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

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Cost\n");
        sb.Append($"{cost.amount.FormatWithDecimals()} {cost.resource.displayName}");
        return sb.ToString();
    }

    protected string GetTimesPurchasedDisplayText(int timesPurchased)
    {
        BasePurchasable blueprint = GetDisplayedBlueprint();

        if(blueprint.IsMaxedOut())
            return "Maxxed";
        
        return $"{timesPurchased}/{blueprint.maxPurchases}";
    }
}
