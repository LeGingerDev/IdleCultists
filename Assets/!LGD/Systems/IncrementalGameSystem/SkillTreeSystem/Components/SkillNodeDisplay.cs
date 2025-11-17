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
    [Tooltip("Image overlay shown when skill is locked (for AlwaysShowLocked mode)")]
    private GameObject _lockOverlay;

    [SerializeField, FoldoutGroup("Skill Node Settings/Lock Overlay")]
    [Tooltip("Icon sprite for locked state")]
    private Image _lockIcon;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Color tint when skill is purchased")]
    private Color _purchasedColor = Color.white;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Color tint when skill is purchasable")]
    private Color _purchasableColor = Color.yellow;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Color tint when prerequisites not met")]
    private Color _lockedRequirementsColor = Color.red;

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Color tint when prerequisites met but can't afford")]
    private Color _lockedCostColor = new Color(1f, 0.5f, 0f); // Orange

    [SerializeField, FoldoutGroup("Skill Node Settings/Visual States")]
    [Tooltip("Image component to apply state colors to")]
    private Image _stateIndicator;

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
        if (_skillBlueprint.GetPurchaseCount() > 0)
            return SkillNodeState.Purchased;

        if (!_skillBlueprint.ArePrerequisitesMet())
            return SkillNodeState.LockedRequirements;

        if (!_skillBlueprint.CanAfford())
            return SkillNodeState.LockedCost;

        return SkillNodeState.Purchasable;
    }

    private void ApplyVisualState(SkillNodeState state)
    {
        if (_stateIndicator == null)
            return;

        Color targetColor = state switch
        {
            SkillNodeState.Purchased => _purchasedColor,
            SkillNodeState.Purchasable => _purchasableColor,
            SkillNodeState.LockedCost => _lockedCostColor,
            SkillNodeState.LockedRequirements => _lockedRequirementsColor,
            _ => Color.white
        };

        _stateIndicator.color = targetColor;
    }

    private void UpdateLockOverlay(SkillNodeState state)
    {
        if (_lockOverlay == null)
            return;

        // Show lock only if locked and not purchased
        bool showLock = (state == SkillNodeState.LockedRequirements || state == SkillNodeState.LockedCost);
        _lockOverlay.SetActive(showLock);
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
    Purchased,              // Green - Skill is owned
    Purchasable,            // Yellow - Can be purchased now
    LockedCost,             // Orange - Prerequisites met but can't afford
    LockedRequirements      // Red - Prerequisites not met
}
