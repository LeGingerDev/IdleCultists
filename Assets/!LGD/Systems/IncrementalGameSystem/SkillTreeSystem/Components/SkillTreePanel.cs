using LGD.Core;
using LGD.Core.Events;
using LGD.UIelements.Panels;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Main controller for a skill tree panel
/// Manages skill node displays, connection lines, and tree state
/// </summary>
public class SkillTreePanel : PurchasablePanel
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

    [FoldoutGroup("Pan Settings")]
    [Tooltip("Enable right-click panning of the skill tree")]
    [SerializeField] private bool _enablePanning = true;

    [FoldoutGroup("Pan Settings")]
    [Tooltip("Content container (parent of all skill nodes)")]
    [SerializeField] private RectTransform _contentTransform;

    [FoldoutGroup("Pan Settings")]
    [Tooltip("Center point reference for node centering (viewport center)")]
    [SerializeField] private RectTransform _centerPoint;

    [FoldoutGroup("Pan Settings")]
    [Tooltip("Speed multiplier for panning")]
    [SerializeField] private float _panSpeed = 1f;

    [FoldoutGroup("Pan Settings")]
    [Tooltip("Use bounds checking to prevent panning too far")]
    [SerializeField] private bool _useBounds = true;

    [FoldoutGroup("Pan Settings")]
    [Tooltip("Extra padding around content bounds")]
    [SerializeField] private float _boundsPadding = 200f;

    [FoldoutGroup("Pan Settings/Zoom")]
    [Tooltip("Enable scroll wheel zooming")]
    [SerializeField] private bool _enableZoom = true;

    [FoldoutGroup("Pan Settings/Zoom")]
    [Tooltip("Minimum zoom scale")]
    [Range(0.1f, 1f)]
    [SerializeField] private float _minZoom = 0.5f;

    [FoldoutGroup("Pan Settings/Zoom")]
    [Tooltip("Maximum zoom scale")]
    [Range(1f, 5f)]
    [SerializeField] private float _maxZoom = 2f;

    [FoldoutGroup("Pan Settings/Zoom")]
    [Tooltip("Zoom speed multiplier")]
    [SerializeField] private float _zoomSpeed = 0.1f;

    [FoldoutGroup("Pan Settings/Centering")]
    [Tooltip("Center on node when purchased")]
    [SerializeField] private bool _centerOnPurchase = true;

    [FoldoutGroup("Pan Settings/Centering")]
    [Tooltip("Only center if node is beyond this distance from center (0 = always center)")]
    [SerializeField] private float _centerDistanceThreshold = 300f;

    [FoldoutGroup("Pan Settings/Centering")]
    [Tooltip("Duration of center animation")]
    [SerializeField] private float _centerAnimationDuration = 0.5f;

    [FoldoutGroup("Pan Settings/Centering")]
    [Tooltip("Ease type for center animation")]
    [SerializeField] private Ease _centerEaseType = Ease.OutCubic;

    [FoldoutGroup("Pan Settings/Centering")]
    [Tooltip("Reset pan/zoom to first node when panel opens (otherwise remembers last position)")]
    [SerializeField] private bool _resetOnOpen = true;

    [FoldoutGroup("Debug")]
    [SerializeField, ReadOnly] private List<SkillNodeDisplay> _skillNodes = new List<SkillNodeDisplay>();

    [FoldoutGroup("Debug")]
    [SerializeField, ReadOnly] private List<SkillConnectionLine> _connectionLines = new List<SkillConnectionLine>();

    private bool _isInitialized = false;
    private bool _isRightMouseDragging;
    private Vector2 _lastMousePosition;
    private Vector2 _minBounds;
    private Vector2 _maxBounds;
    private Tween _centerTween;

    // Stored pan/zoom state
    private Vector2 _storedPanPosition;
    private float _storedZoomScale = 1f;
    private Vector2 _initialContentPosition;

    protected override void Start()
    {
        base.Start();
        InitializeTree();
    }

    private void Update()
    {
        if (!_isInitialized || !_enablePanning)
            return;

        HandlePanningInput();
    }

    [Button("Initialize Tree"), FoldoutGroup("Debug")]
    private void InitializeTree()
    {
        if (_isInitialized)
            return;

        DebugManager.Log($"[SkillTreePanel] Initializing skill tree: {GetTreeName()}");

        // Store initial content position for reset
        if (_contentTransform != null)
        {
            _initialContentPosition = _contentTransform.anchoredPosition;
        }

        // Find all skill node displays in children
        FindAllSkillNodes();

        // Create connection lines based on prerequisites
        CreateConnectionLines();

        // Apply display mode to all nodes
        ApplyDisplayMode();

        // Calculate bounds for panning
        CalculateBounds();

        // Refresh initial state
        RefreshTreeState();

        _isInitialized = true;

        DebugManager.Log($"[SkillTreePanel] Initialized with {_skillNodes.Count} nodes and {_connectionLines.Count} connections");
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

    #region Panning & Zooming

    /// <summary>
    /// Handle right mouse button panning and scroll wheel zoom input
    /// </summary>
    private void HandlePanningInput()
    {
        if (_contentTransform == null)
            return;

        // Handle zoom
        if (_enableZoom)
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                HandleZoom(scrollDelta);
            }
        }

        // Start dragging
        if (Input.GetMouseButtonDown(1))
        {
            _isRightMouseDragging = true;
            _lastMousePosition = Input.mousePosition;
        }
        // Update drag
        else if (Input.GetMouseButton(1) && _isRightMouseDragging)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 delta = currentMousePosition - _lastMousePosition;

            // Apply pan speed
            delta *= _panSpeed;

            // Move content
            _contentTransform.anchoredPosition += delta;

            // Clamp to bounds if enabled
            if (_useBounds)
            {
                _contentTransform.anchoredPosition = ClampToBounds(_contentTransform.anchoredPosition);
            }

            _lastMousePosition = currentMousePosition;
        }
        // Stop dragging
        else if (Input.GetMouseButtonUp(1))
        {
            _isRightMouseDragging = false;
        }
    }

    /// <summary>
    /// Handle zoom input with zoom-to-mouse-cursor behavior
    /// Zooms toward the point under the mouse cursor for natural interaction
    /// </summary>
    private void HandleZoom(float scrollDelta)
    {
        if (_contentTransform == null)
            return;

        // Calculate new scale
        float oldScale = _contentTransform.localScale.x;
        float newScale = oldScale + (scrollDelta * _zoomSpeed);
        newScale = Mathf.Clamp(newScale, _minZoom, _maxZoom);

        // If scale didn't change, don't do anything
        if (Mathf.Approximately(oldScale, newScale))
            return;

        // Get mouse position and convert to content's local space BEFORE scaling
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _contentTransform,
            Input.mousePosition,
            null,
            out localPoint
        );

        // Store the mouse screen position
        Vector2 mouseScreenPos = Input.mousePosition;

        // Apply scale
        _contentTransform.localScale = Vector3.one * newScale;

        // After scaling, calculate where that local point is now in screen space
        Vector2 newScreenPoint = RectTransformUtility.WorldToScreenPoint(
            null,
            _contentTransform.TransformPoint(localPoint)
        );

        // Calculate the offset and adjust content position to keep point under mouse
        Vector2 screenDelta = mouseScreenPos - newScreenPoint;

        // Get the viewport to account for canvas scaling
        RectTransform viewport = _contentTransform.parent as RectTransform;
        if (viewport != null)
        {
            _contentTransform.anchoredPosition += screenDelta / viewport.lossyScale.x;
        }

        // Clamp to bounds if enabled
        if (_useBounds)
        {
            _contentTransform.anchoredPosition = ClampToBounds(_contentTransform.anchoredPosition);
        }
    }

    /// <summary>
    /// Calculate bounds based on all skill node positions
    /// </summary>
    [Button("Recalculate Bounds"), FoldoutGroup("Debug")]
    private void CalculateBounds()
    {
        if (_contentTransform == null || _skillNodes.Count == 0)
        {
            DebugManager.Warning("[SkillTreePanel] Cannot calculate bounds: missing content or no nodes");
            return;
        }

        // Find the min/max positions of all nodes
        Vector2 min = Vector2.one * float.MaxValue;
        Vector2 max = Vector2.one * float.MinValue;

        foreach (SkillNodeDisplay node in _skillNodes)
        {
            if (node == null)
                continue;

            RectTransform nodeRect = node.GetComponent<RectTransform>();
            if (nodeRect == null)
                continue;

            Vector2 pos = nodeRect.anchoredPosition;
            min = Vector2.Min(min, pos);
            max = Vector2.Max(max, pos);
        }

        // Add padding
        min -= Vector2.one * _boundsPadding;
        max += Vector2.one * _boundsPadding;

        _minBounds = min;
        _maxBounds = max;

        DebugManager.Log($"[SkillTreePanel] Calculated bounds: Min={_minBounds}, Max={_maxBounds}");
    }

    /// <summary>
    /// Clamp position to stay within bounds
    /// Prevents panning beyond the edges of the content
    /// </summary>
    private Vector2 ClampToBounds(Vector2 position)
    {
        if (_contentTransform == null)
            return position;

        // Get the viewport (parent of content)
        RectTransform viewport = _contentTransform.parent as RectTransform;
        if (viewport == null)
            return position;

        // Get current scale
        float scale = _contentTransform.localScale.x;

        // Calculate scaled content bounds
        Vector2 scaledMin = _minBounds * scale;
        Vector2 scaledMax = _maxBounds * scale;

        // Viewport size
        Vector2 viewportSize = viewport.rect.size;

        // Calculate content size
        Vector2 contentSize = (scaledMax - scaledMin);

        // Calculate max allowed offset in each direction
        // If content is smaller than viewport, don't clamp (allow centering)
        float maxOffsetX = Mathf.Max(0, (contentSize.x - viewportSize.x) * 0.5f);
        float maxOffsetY = Mathf.Max(0, (contentSize.y - viewportSize.y) * 0.5f);

        // Calculate the center offset (where content would be centered)
        Vector2 contentCenter = (scaledMin + scaledMax) * 0.5f;

        // Clamp position relative to content center
        position.x = Mathf.Clamp(position.x, -contentCenter.x - maxOffsetX, -contentCenter.x + maxOffsetX);
        position.y = Mathf.Clamp(position.y, -contentCenter.y - maxOffsetY, -contentCenter.y + maxOffsetY);

        return position;
    }

    /// <summary>
    /// Center the view on a specific skill node
    /// </summary>
    public void CenterOnNode(SkillNodeDisplay node, bool animated = true)
    {
        if (_contentTransform == null || _centerPoint == null || node == null)
        {
            DebugManager.Warning("[SkillTreePanel] Cannot center: missing references");
            return;
        }

        // Get the node's RectTransform
        RectTransform nodeRect = node.GetComponent<RectTransform>();
        if (nodeRect == null)
            return;

        // Calculate the offset needed to move the node to the center point
        // The node's position is relative to the content transform
        Vector2 nodeLocalPos = nodeRect.anchoredPosition;

        // The center point's position in the content's local space
        Vector2 targetOffset = -nodeLocalPos;

        // Clamp to bounds if enabled
        if (_useBounds)
        {
            targetOffset = ClampToBounds(targetOffset);
        }

        // Kill any existing center animation
        _centerTween?.Kill();

        if (animated)
        {
            // Animate to the target position
            _centerTween = _contentTransform.DOAnchorPos(targetOffset, _centerAnimationDuration)
                .SetEase(_centerEaseType);
        }
        else
        {
            // Instant move
            _contentTransform.anchoredPosition = targetOffset;
        }

        DebugManager.Log($"[SkillTreePanel] Centering on node: {node.name}");
    }

    /// <summary>
    /// Check if a node is far from center and needs centering
    /// </summary>
    private bool ShouldCenterOnNode(SkillNodeDisplay node)
    {
        if (!_centerOnPurchase || _centerPoint == null || node == null)
            return false;

        // If threshold is 0, always center
        if (_centerDistanceThreshold <= 0f)
            return true;

        // Get the node's RectTransform
        RectTransform nodeRect = node.GetComponent<RectTransform>();
        if (nodeRect == null)
            return false;

        // Calculate distance from node to center point in screen space
        Vector3 nodeScreenPos = RectTransformUtility.WorldToScreenPoint(null, nodeRect.position);
        Vector3 centerScreenPos = RectTransformUtility.WorldToScreenPoint(null, _centerPoint.position);

        float distance = Vector2.Distance(nodeScreenPos, centerScreenPos);

        return distance > _centerDistanceThreshold;
    }

    #endregion

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

        // Check if we should center on this node
        if (sender is SkillNodeDisplay nodeDisplay)
        {
            if (ShouldCenterOnNode(nodeDisplay))
            {
                CenterOnNode(nodeDisplay, animated: true);
            }
        }
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
        base.OnOpen(); // Start periodic refresh and purchase loops for all child displays
        RefreshTreeState();

        // Handle pan/zoom state on open
        if (_contentTransform != null)
        {
            if (_resetOnOpen)
            {
                // Reset to first node or initial position
                ResetPanAndZoom();
            }
            else
            {
                // Restore last position/zoom
                RestorePanAndZoom();
            }
        }
    }

    protected override void OnClose()
    {
        base.OnClose(); // Stop periodic refresh and purchase loops for all child displays

        // Store current pan/zoom state
        StorePanAndZoom();
    }

    /// <summary>
    /// Store current pan and zoom state
    /// </summary>
    private void StorePanAndZoom()
    {
        if (_contentTransform != null)
        {
            _storedPanPosition = _contentTransform.anchoredPosition;
            _storedZoomScale = _contentTransform.localScale.x;
            DebugManager.Log($"[SkillTreePanel] Stored pan/zoom: pos={_storedPanPosition}, scale={_storedZoomScale}");
        }
    }

    /// <summary>
    /// Restore previously stored pan and zoom state
    /// </summary>
    private void RestorePanAndZoom()
    {
        if (_contentTransform != null)
        {
            _contentTransform.anchoredPosition = _storedPanPosition;
            _contentTransform.localScale = Vector3.one * _storedZoomScale;
            DebugManager.Log($"[SkillTreePanel] Restored pan/zoom: pos={_storedPanPosition}, scale={_storedZoomScale}");
        }
    }

    /// <summary>
    /// Reset pan to first node and zoom to 1.0
    /// </summary>
    private void ResetPanAndZoom()
    {
        if (_contentTransform == null)
            return;

        // Reset zoom to 1.0
        _contentTransform.localScale = Vector3.one;

        // Center on first node if available
        if (_skillNodes != null && _skillNodes.Count > 0)
        {
            SkillNodeDisplay firstNode = _skillNodes[0];
            if (firstNode != null)
            {
                CenterOnNode(firstNode, animated: false);
                DebugManager.Log($"[SkillTreePanel] Reset to first node: {firstNode.name}");
                return;
            }
        }

        // Otherwise reset to initial position
        _contentTransform.anchoredPosition = _initialContentPosition;
        DebugManager.Log($"[SkillTreePanel] Reset to initial position: {_initialContentPosition}");
    }

    private void OnDestroy()
    {
        // Clean up any active tweens
        _centerTween?.Kill();
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
