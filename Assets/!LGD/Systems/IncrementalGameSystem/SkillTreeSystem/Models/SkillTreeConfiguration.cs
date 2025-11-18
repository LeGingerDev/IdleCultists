using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Configuration settings for a skill tree
/// Each skill tree (Shadow, Divine, Utility, etc.) should have its own configuration asset
/// </summary>
[CreateAssetMenu(fileName = "SkillTreeConfiguration", menuName = "Incremental Game/Skill Tree/Configuration")]
public class SkillTreeConfiguration : ScriptableObject
{
    [FoldoutGroup("Identity")]
    [Tooltip("Unique identifier for this skill tree (e.g., 'shadow-tree', 'divine-tree')")]
    public string treeId;

    [FoldoutGroup("Identity")]
    [Tooltip("Display name shown in UI (e.g., 'Shadow Arts', 'Divine Path')")]
    public string displayName;

    [FoldoutGroup("Identity")]
    [TextArea(2, 4)]
    [Tooltip("Description of what this skill tree represents")]
    public string description;

    [FoldoutGroup("Identity")]
    [Tooltip("Icon representing this skill tree")]
    public Sprite icon;

    [FoldoutGroup("Display Settings")]
    [Tooltip("How should nodes be displayed? Hidden until unlocked or always visible with locks?")]
    public SkillNodeDisplayMode displayMode = SkillNodeDisplayMode.AlwaysShowLocked;

    [FoldoutGroup("Visual Settings")]
    [Tooltip("Color theme for this tree's connections and highlights")]
    public Color treeColor = Color.white;

    [FoldoutGroup("Visual Settings")]
    [Tooltip("Thickness of connection lines between nodes")]
    [Range(1f, 20f)]
    public float lineThickness = 5f;

    [FoldoutGroup("Visual Settings")]
    [Tooltip("Optional background sprite for the tree panel")]
    public Sprite backgroundSprite;

    [FoldoutGroup("Progression Settings")]
    [Tooltip("Does this tree reset on ascension or persist forever?")]
    public bool resetOnAscension = true;

    [FoldoutGroup("Progression Settings")]
    [Tooltip("Minimum ascension level required to access this tree (0 = always available)")]
    public int minimumAscensionLevel = 0;
}
