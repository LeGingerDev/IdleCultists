/// <summary>
/// Event IDs for the Skill Tree system
/// </summary>
public static class SkillTreeEventIds
{
    // Fired when a skill node is purchased
    public const string ON_SKILL_NODE_PURCHASED = "on-skill-node-purchased";

    // Fired when a skill tree refreshes its state (e.g., after a purchase)
    public const string ON_SKILL_TREE_REFRESHED = "on-skill-tree-refreshed";

    // Fired when a skill becomes unlockable (prerequisites met)
    public const string ON_SKILL_UNLOCKED = "on-skill-unlocked";

    // Fired when a skill tree is reset/cleared
    public const string ON_SKILL_TREE_RESET = "on-skill-tree-reset";
}
