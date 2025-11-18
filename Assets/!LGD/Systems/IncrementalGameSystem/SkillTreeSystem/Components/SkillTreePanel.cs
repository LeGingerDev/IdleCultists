using LGD.Core;
using LGD.Core.Events;
using LGD.UIelements.Panels;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main controller for a skill tree panel
/// Manages skill node displays, connection lines, and tree state
/// </summary>
public class SkillTreePanel : SlidePanel
{
    [FoldoutGroup("Configuration")]
    [Tooltip("Configuration asset for this skill tree")]
    [SerializeField] private SkillTreeConfiguration _configuration;

    [FoldoutGroup("Line Settings")]
    [Tooltip("Parent transform for all connection lines")]
    [SerializeField] private Transform _connectionLinesParent;

    [FoldoutGroup("Line Settings")]
    [Tooltip("Prefab for connection lines (must have Image and SkillConnectionLine components)")]
    [SerializeField] private GameObject _connectionLinePrefab;

    [FoldoutGroup("Line Settings")]
    [Tooltip("Sprite to use for connection lines")]
    [SerializeField] private Sprite _lineSprite;

    [FoldoutGroup("Refresh Settings")]
    [Tooltip("How often to refresh node states (in seconds). Set to 0 to disable periodic refresh.")]
    [Range(0f, 1f)]
    [SerializeField] private float _refreshInterval = 0.2f;

    [FoldoutGroup("Debug")]
    [SerializeField, ReadOnly] private List<SkillNodeDisplay> _skillNodes = new List<SkillNodeDisplay>();

    [FoldoutGroup("Debug")]
    [SerializeField, ReadOnly] private List<SkillConnectionLine> _connectionLines = new List<SkillConnectionLine>();

    private bool _isInitialized = false;
    private Coroutine _periodicRefreshCoroutine;

    protected override void Start()
    {
        base.Start();
        InitializeTree();
    }


    [Button("Initialize Tree"), FoldoutGroup("Debug")]
    private void InitializeTree()
    {
        if (_isInitialized)
            return;

        DebugManager.Log($"[SkillTreePanel] Initializing skill tree: {GetTreeName()}");

        // Find all skill node displays in children
        FindAllSkillNodes();

        // Create connection lines based on prerequisites
        CreateConnectionLines();

        // Apply display mode to all nodes
        ApplyDisplayMode();

        // Refresh initial state
        RefreshTreeState();

        _isInitialized = true;

        // Start periodic refresh if interval is set
        StartPeriodicRefresh();

        DebugManager.Log($"[SkillTreePanel] Initialized with {_skillNodes.Count} nodes and {_connectionLines.Count} connections");
    }

    private void OnDisable()
    {
        StopPeriodicRefresh();
    }

    private void OnDestroy()
    {
        StopPeriodicRefresh();
    }

    private void FindAllSkillNodes()
    {
        _skillNodes.Clear();
        _skillNodes.AddRange(GetComponentsInChildren<SkillNodeDisplay>(true));
        DebugManager.Log($"[SkillTreePanel] Found {_skillNodes.Count} skill nodes");
    }

    private void CreateConnectionLines()
    {
        if (_connectionLinePrefab == null)
        {
            DebugManager.Error("[SkillTreePanel] No connection line prefab assigned!");
            return;
        }

        if (_connectionLinesParent == null)
        {
            DebugManager.Warning("[SkillTreePanel] No connection lines parent assigned, using self");
            _connectionLinesParent = transform;
        }

        // Clear existing lines
        ClearConnectionLines();

        // Create line for each prerequisite relationship
        foreach (SkillNodeDisplay node in _skillNodes)
        {
            BasePurchasable blueprint = node.GetBlueprint();
            if (blueprint == null)
                continue;

            // Check each prerequisite
            foreach (BasePurchasable prerequisite in blueprint.prerequisitePurchasables)
            {
                if (prerequisite == null)
                {
                    DebugManager.Warning($"[SkillTreePanel] Prerequisite is NULL for skill: {blueprint.purchasableId}");
                    continue;
                }

                // Find the node that represents this prerequisite
                SkillNodeDisplay prerequisiteNode = FindNodeForBlueprint(prerequisite);
                if (prerequisiteNode == null)
                {
                    DebugManager.Warning($"[SkillTreePanel] Could not find node for prerequisite: {prerequisite.purchasableId}");
                    continue;
                }
                DebugManager.Log($"[SkillTreePanel] Creating line from {prerequisiteNode.name} to {node.name}");
                // Create connection line
                CreateConnectionLine(prerequisiteNode, node);
            }
        }

        DebugManager.Log($"[SkillTreePanel] Created {_connectionLines.Count} connection lines");
    }

    private void CreateConnectionLine(SkillNodeDisplay fromNode, SkillNodeDisplay toNode)
    {
        // Instantiate line
        GameObject lineObj = Instantiate(_connectionLinePrefab, _connectionLinesParent);
        lineObj.name = $"Line_{fromNode.name}_to_{toNode.name}";

        // Get components
        Image lineImage = lineObj.GetComponent<Image>();
        if (lineImage != null && _lineSprite != null)
        {
            lineImage.sprite = _lineSprite;
        }

        SkillConnectionLine connectionLine = lineObj.GetComponent<SkillConnectionLine>();
        if (connectionLine == null)
        {
            connectionLine = lineObj.AddComponent<SkillConnectionLine>();
        }

        // Get line thickness from config
        float lineThickness = _configuration != null ? _configuration.lineThickness : 5f;

        // Initialize
        connectionLine.Initialize(fromNode, toNode, lineImage, lineThickness);
        connectionLine.UpdateLineTransform();

        // Apply tree color if configured
        if (_configuration != null && lineImage != null)
        {
            Color inactiveColor = _configuration.treeColor * 0.3f;
            inactiveColor.a = 0.5f;
            lineImage.color = inactiveColor;
        }

        _connectionLines.Add(connectionLine);
    }

    private SkillNodeDisplay FindNodeForBlueprint(BasePurchasable blueprint)
    {
        return _skillNodes.FirstOrDefault(node => node.GetBlueprint().purchasableId == blueprint.purchasableId);
    }

    private void ClearConnectionLines()
    {
        foreach (SkillConnectionLine line in _connectionLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }

        _connectionLines.Clear();
    }

    private void ApplyDisplayMode()
    {
        if (_configuration == null)
            return;

        SkillNodeDisplayMode mode = _configuration.displayMode;

        foreach (SkillNodeDisplay node in _skillNodes)
        {
            bool shouldBeVisible = node.ShouldBeVisible(mode);
            node.SetNodeVisible(shouldBeVisible);
        }
    }

    /// <summary>
    /// Refresh the state of the entire tree (called after purchases or stat changes)
    /// </summary>
    public void RefreshTreeState()
    {
        DebugManager.Log($"[SkillTreePanel] Refreshing tree state for: {GetTreeName()}");

        // Update all node UI states (includes visual state, button state, costs, etc.)
        foreach (SkillNodeDisplay node in _skillNodes)
        {
            if (node != null)
            {
                // Refresh updates all UI elements including visual state
                node.Refresh();

                // Re-evaluate visibility if using HideUntilUnlocked mode
                if (_configuration != null && _configuration.displayMode == SkillNodeDisplayMode.HideUntilUnlocked)
                {
                    bool shouldBeVisible = node.ShouldBeVisible(_configuration.displayMode);
                    node.SetNodeVisible(shouldBeVisible);
                }
            }
        }

        // Update all connection lines
        foreach (SkillConnectionLine line in _connectionLines)
        {
            if (line != null)
            {
                line.UpdateVisualState();
                line.UpdateLineTransform();

                // Update visibility based on node visibility
                bool shouldBeVisible = line.ShouldBeVisible();
                line.SetVisible(shouldBeVisible);
            }
        }

        // Publish refresh event
        ServiceBus.Publish(SkillTreeEventIds.ON_SKILL_TREE_REFRESHED, this, GetTreeId());
    }

    /// <summary>
    /// Manually refresh all line positions (useful if nodes move)
    /// </summary>
    [Button("Refresh Line Positions"), FoldoutGroup("Debug")]
    public void RefreshLinePositions()
    {
        foreach (SkillConnectionLine line in _connectionLines)
        {
            if (line != null)
            {
                line.UpdateLineTransform();
            }
        }
    }

    /// <summary>
    /// Start the periodic refresh coroutine
    /// </summary>
    private void StartPeriodicRefresh()
    {
        if (_refreshInterval <= 0f)
        {
            DebugManager.Log($"[SkillTreePanel] Periodic refresh disabled (interval = {_refreshInterval})");
            return;
        }

        StopPeriodicRefresh(); // Stop existing coroutine if any
        _periodicRefreshCoroutine = StartCoroutine(PeriodicRefreshCoroutine());
        DebugManager.Log($"[SkillTreePanel] Started periodic refresh every {_refreshInterval}s");
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
            DebugManager.Log($"[SkillTreePanel] Stopped periodic refresh");
        }
    }

    /// <summary>
    /// Coroutine that periodically refreshes all nodes
    /// This ensures nodes update their state even when changes happen off-screen
    /// </summary>
    private IEnumerator PeriodicRefreshCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_refreshInterval);

        while (true)
        {
            yield return wait;

            // Only refresh if the panel is active
            if (gameObject.activeInHierarchy && _isInitialized)
            {
                RefreshTreeState();
            }
        }
    }

    /// <summary>
    /// Listen for purchasable purchases to refresh tree
    /// </summary>
    [Topic(PurchasableEventIds.ON_PURCHASABLE_PURCHASED)]
    public void OnPurchasablePurchased(object sender, BasePurchasable blueprint, BasePurchasableRuntimeData runtimeData)
    {
        // Check if this purchase affects our tree
        if (IsSkillInThisTree(blueprint))
        {
            DebugManager.Log($"[SkillTreePanel] Skill purchased in tree {GetTreeName()}, refreshing...");
            RefreshTreeState();
        }
    }

    /// <summary>
    /// Listen for skill node purchases specifically
    /// </summary>
    [Topic(SkillTreeEventIds.ON_SKILL_NODE_PURCHASED)]
    public void OnSkillNodePurchased(object sender, BasePurchasable blueprint)
    {
        DebugManager.Log($"[SkillTreePanel] Skill node purchased: {blueprint?.displayName}");
        // Tree should already be refreshed by the node itself, but we can add extra logic here if needed
    }

    private bool IsSkillInThisTree(BasePurchasable blueprint)
    {
        return _skillNodes.Any(node => node.GetBlueprint() == blueprint);
    }

    private string GetTreeName()
    {
        return _configuration != null ? _configuration.displayName : gameObject.name;
    }

    private string GetTreeId()
    {
        return _configuration != null ? _configuration.treeId : gameObject.name;
    }

    /// <summary>
    /// Get the display mode for this tree
    /// </summary>
    public SkillNodeDisplayMode GetDisplayMode()
    {
        return _configuration != null ? _configuration.displayMode : SkillNodeDisplayMode.AlwaysShowLocked;
    }

    protected override void OnOpen()
    {
        RefreshTreeState();
        StartPeriodicRefresh();
    }

    protected override void OnClose()
    {
        StopPeriodicRefresh();
    }

#if UNITY_EDITOR
    [Button("Recreate Lines"), FoldoutGroup("Debug")]
    private void RecreateLines()
    {
        FindAllSkillNodes();
        CreateConnectionLines();
        RefreshTreeState();
    }

    private void OnValidate()
    {
        // Auto-find connection lines parent if not set
        if (_connectionLinesParent == null)
        {
            Transform linesTransform = transform.Find("ConnectionLines");
            if (linesTransform != null)
            {
                _connectionLinesParent = linesTransform;
            }
        }
    }
#endif
}
