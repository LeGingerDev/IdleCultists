/// <summary>
/// Determines how skill nodes are displayed in the skill tree
/// </summary>
public enum SkillNodeDisplayMode
{
    /// <summary>
    /// All skill nodes are always visible, locked nodes show a lock icon
    /// Best for strategic planning and seeing the full tree structure
    /// </summary>
    AlwaysShowLocked,

    /// <summary>
    /// Skill nodes are hidden until their prerequisites are met
    /// Creates a sense of discovery as the tree gradually reveals itself
    /// </summary>
    HideUntilUnlocked
}
