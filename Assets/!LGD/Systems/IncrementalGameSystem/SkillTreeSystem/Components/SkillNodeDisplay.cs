using LGD.Core.Events;
using LGD.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display component for a skill tree node
/// Extends BasePurchasableDisplay to handle skill-specific visualization and state management
/// </summary>
public class SkillNodeDisplay : BasePurchasableDisplay
{
    [SerializeField, FoldoutGroup("Skill Node Settings")]
    [Tooltip("The purchasable blueprint representing this skill")]
    private BasePurchasable _skillBlueprint;

    [SerializeField, FoldoutGroup("Skill Node Settings/Lock Overlay")]
    [Tooltip("Lock overlay shown when prerequisites not met (only visible in AlwaysShowLocked mode)")]
    private GameObject _lockOverlay;

    [SerializeField, FoldoutGroup("Skill Node Settings/Icon")]
    [Tooltip("Icon image that shows skill content (hidden when lock overlay is visible)")]
    private GameObject _iconObject;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Target image to apply state sprites to")]
    private Image _stateImage;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Sprite when skill is locked (prerequisites not met)")]
    private Sprite _lockedSprite;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Sprite when can't afford (prerequisites met but insufficient resources)")]
    private Sprite _cantAffordSprite;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Sprite when skill is purchasable (can afford and prerequisites met)")]
    private Sprite _purchasableSprite;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Sprite when skill is purchased")]
    private Sprite _purchasedSprite;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Sprite when skill is maxed out")]
    private Sprite _maxedSprite;

    private SkillTreePanel _parentTree;

    protected override BasePurchasable GetDisplayedBlueprint()
    {
        return _skillBlueprint;
    }

    /// <summary>
    /// Public accessor for blueprint (used by tooltip and connection lines)
    /// </summary>
    public BasePurchasable GetBlueprint() => _skillBlueprint;

    private void Start()
    {
        // Find parent tree
        _parentTree = GetComponentInParent<SkillTreePanel>();

        // Initialize the display
        Initialise();
    }

    protected override void OnPurchaseClicked()
    {
        if (_skillBlueprint == null)
        {
            DebugManager.Warning("[SkillNodeDisplay] No blueprint assigned!");
            return;
        }

        // Check if can purchase
        if (!_skillBlueprint.CanAfford())
        {
            DebugManager.Log($"[SkillNodeDisplay] Cannot afford {_skillBlueprint.displayName}");
            return;
        }

        if (!_skillBlueprint.ArePrerequisitesMet())
        {
            DebugManager.Log($"[SkillNodeDisplay] Prerequisites not met for {_skillBlueprint.displayName}");
            return;
        }

        if (_skillBlueprint.IsMaxedOut())
        {
            DebugManager.Log($"[SkillNodeDisplay] {_skillBlueprint.displayName} is already maxed out");
            return;
        }

        DebugManager.Log($"[SkillNodeDisplay] Purchasing skill: {_skillBlueprint.displayName}");

        // Execute purchase
        bool success = _skillBlueprint.ExecutePurchase();

        if (success)
        {
            DebugManager.Log($"[SkillNodeDisplay] Successfully purchased {_skillBlueprint.displayName}");

            // Refresh this display
            RefreshDynamicUI();

            // Publish skill-specific event
            ServiceBus.Publish(SkillTreeEventIds.ON_SKILL_NODE_PURCHASED, this, _skillBlueprint);

            // Request tree refresh
            if (_parentTree != null)
            {
                _parentTree.RefreshTreeState();
            }
        }
        else
        {
            DebugManager.Warning($"[SkillNodeDisplay] Failed to purchase {_skillBlueprint.displayName}");
        }
    }

    protected override bool CanPurchase()
    {
        if (_skillBlueprint == null)
            return false;

        return _skillBlueprint.ArePrerequisitesMet() &&
               _skillBlueprint.CanAfford() &&
               !_skillBlueprint.IsMaxedOut();
    }

    protected override string GetButtonText()
    {
        if (_skillBlueprint == null)
            return string.Empty;

        if (_skillBlueprint.IsMaxedOut())
            return "Maxed";

        if (!_skillBlueprint.ArePrerequisitesMet())
            return "Locked";

        if (!_skillBlueprint.CanAfford())
            return "Can't Afford";

        // Check purchase type for button text
        int purchaseCount = _skillBlueprint.GetPurchaseCount();
        if (purchaseCount > 0 && _skillBlueprint.purchaseType != PurchaseType.OneTime)
            return "Upgrade";

        return "Purchase";
    }

    /// <summary>
    /// Update the visual state of the node based on its current status
    /// </summary>
    public void UpdateVisualState()
    {
        if (_skillBlueprint == null)
            return;

        SkillNodeState state = DetermineNodeState();
        ApplyVisualState(state);
        UpdateLockOverlay(state);
    }

    private SkillNodeState DetermineNodeState()
    {
        if (_skillBlueprint.IsMaxedOut())
            return SkillNodeState.Maxed;

        if (_skillBlueprint.GetPurchaseCount() > 0)
            return SkillNodeState.Purchased;

        if (!_skillBlueprint.ArePrerequisitesMet())
            return SkillNodeState.Locked;

        if (!_skillBlueprint.CanAfford())
            return SkillNodeState.CantAfford;

        return SkillNodeState.Purchasable;
    }

    private void ApplyVisualState(SkillNodeState state)
    {
        if (_stateImage == null)
            return;

        Sprite targetSprite = state switch
        {
            SkillNodeState.Locked => _lockedSprite,
            SkillNodeState.CantAfford => _cantAffordSprite,
            SkillNodeState.Purchasable => _purchasableSprite,
            SkillNodeState.Purchased => _purchasedSprite,
            SkillNodeState.Maxed => _maxedSprite,
            _ => null
        };

        if (targetSprite != null)
        {
            _stateImage.sprite = targetSprite;
        }
    }

    private void UpdateLockOverlay(SkillNodeState state)
    {
        // Only show lock overlay if:
        // 1. The state is Locked (prerequisites not met)
        // 2. The display mode is AlwaysShowLocked (otherwise the node would be hidden)
        bool showLock = state == SkillNodeState.Locked;

        if (_parentTree != null)
        {
            SkillNodeDisplayMode displayMode = _parentTree.GetDisplayMode();
            showLock = showLock && displayMode == SkillNodeDisplayMode.AlwaysShowLocked;
        }

        // Show/hide lock overlay
        if (_lockOverlay != null)
        {
            _lockOverlay.SetActive(showLock);
        }

        // Icon visibility is opposite of lock overlay
        // Icon shows when lock is hidden and vice versa
        if (_iconObject != null)
        {
            _iconObject.SetActive(!showLock);
        }
    }

    /// <summary>
    /// Refresh the display state (called by parent tree)
    /// </summary>
    protected override void RefreshDynamicUI()
    {
        base.RefreshDynamicUI();
        UpdateVisualState();
    }

    /// <summary>
    /// Determine if this node should be visible based on display mode
    /// </summary>
    public bool ShouldBeVisible(SkillNodeDisplayMode displayMode)
    {
        if (_skillBlueprint == null)
            return false;

        switch (displayMode)
        {
            case SkillNodeDisplayMode.AlwaysShowLocked:
                return true;

            case SkillNodeDisplayMode.HideUntilUnlocked:
                // Show if purchased OR prerequisites are met
                return _skillBlueprint.GetPurchaseCount() > 0 || _skillBlueprint.ArePrerequisitesMet();

            default:
                return true;
        }
    }

    /// <summary>
    /// Set the visibility of this node
    /// </summary>
    public void SetNodeVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}

/// <summary>
/// Visual states for skill nodes
/// </summary>
public enum SkillNodeState
{
    Locked,         // Prerequisites not met
    CantAfford,     // Prerequisites met but insufficient resources
    Purchasable,    // Can be purchased right now
    Purchased,      // Skill is owned
    Maxed           // Skill is maxed out
}
