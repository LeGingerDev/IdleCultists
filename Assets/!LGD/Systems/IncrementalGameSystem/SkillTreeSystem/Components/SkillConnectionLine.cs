using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual connection line between two skill nodes
/// Represents a prerequisite relationship (fromNode is required for toNode)
/// </summary>
public class SkillConnectionLine : MonoBehaviour
{
    [FoldoutGroup("References")]
    [Tooltip("The prerequisite skill node")]
    [SerializeField] private SkillNodeDisplay _fromNode;

    [FoldoutGroup("References")]
    [Tooltip("The dependent skill node")]
    [SerializeField] private SkillNodeDisplay _toNode;

    [FoldoutGroup("References")]
    [Tooltip("The UI Image component used to draw the line")]
    [SerializeField] private Image _lineImage;

    [FoldoutGroup("Visual Settings")]
    [Tooltip("Color when the prerequisite is not purchased")]
    [SerializeField] private Color _inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [FoldoutGroup("Visual Settings")]
    [Tooltip("Color when the prerequisite is purchased")]
    [SerializeField] private Color _activeColor = Color.white;

    [FoldoutGroup("Visual Settings")]
    [Tooltip("Line thickness in pixels")]
    [SerializeField] private float _lineThickness = 3f;

    private RectTransform _rectTransform;

    public SkillNodeDisplay FromNode => _fromNode;
    public SkillNodeDisplay ToNode => _toNode;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            _rectTransform = gameObject.AddComponent<RectTransform>();
        }
    }

    /// <summary>
    /// Initialize the connection line with two nodes
    /// </summary>
    public void Initialize(SkillNodeDisplay fromNode, SkillNodeDisplay toNode, Image lineImage)
    {
        _fromNode = fromNode;
        _toNode = toNode;
        _lineImage = lineImage;

        UpdateVisualState();
    }

    /// <summary>
    /// Position and rotate the line to connect the two nodes
    /// Simple stretched image approach
    /// </summary>
    public void UpdateLineTransform()
    {
        if (_fromNode == null || _toNode == null || _rectTransform == null)
            return;

        RectTransform fromRect = _fromNode.GetComponent<RectTransform>();
        RectTransform toRect = _toNode.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null)
            return;

        // Get positions
        Vector2 fromPos = fromRect.anchoredPosition;
        Vector2 toPos = toRect.anchoredPosition;

        // Calculate center point
        Vector2 center = (fromPos + toPos) / 2f;
        _rectTransform.anchoredPosition = center;

        // Calculate direction and distance
        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Set size and rotation
        _rectTransform.sizeDelta = new Vector2(distance, _lineThickness);
        _rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Update the visual state of the line based on prerequisite status
    /// </summary>
    public void UpdateVisualState()
    {
        if (_lineImage == null || _fromNode == null)
            return;

        // Line is active if the prerequisite is purchased
        BasePurchasable fromBlueprint = _fromNode.GetBlueprint();
        if (fromBlueprint == null)
        {
            SetLineActive(false);
            return;
        }

        bool isPrerequisitePurchased = fromBlueprint.GetPurchaseCount() > 0;
        SetLineActive(isPrerequisitePurchased);
    }

    /// <summary>
    /// Set whether the line appears active or inactive
    /// </summary>
    public void SetLineActive(bool active)
    {
        if (_lineImage != null)
        {
            _lineImage.color = active ? _activeColor : _inactiveColor;
        }
    }

    /// <summary>
    /// Show or hide the line
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_lineImage != null)
        {
            _lineImage.enabled = visible;
        }
    }

    /// <summary>
    /// Check if this line should be hidden based on node visibility
    /// </summary>
    public bool ShouldBeVisible()
    {
        if (_fromNode == null || _toNode == null)
            return false;

        // Line is visible if both nodes are active
        return _fromNode.gameObject.activeSelf && _toNode.gameObject.activeSelf;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw gizmo line in editor for debugging
        if (_fromNode != null && _toNode != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_fromNode.transform.position, _toNode.transform.position);
        }
    }
#endif
}
