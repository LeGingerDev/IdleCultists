using LargeNumbers;
using LGD.ResourceSystem.Models;

/// <summary>
/// Data transfer object for skill node tooltips
/// Contains all information needed to display a skill tooltip
/// </summary>
public class SkillNodeData
{
    public string skillName;
    public string description;
    public int currentLevel;
    public int maxLevel; // -1 for infinite
    public ResourceAmountPair cost;
    public string currentBonusText;
    public string nextBonusText;
    public bool isPurchased;
    public bool isMaxedOut;
    public bool canAfford;
    public bool prerequisitesMet;

    public SkillNodeData(
        string skillName,
        string description,
        int currentLevel,
        int maxLevel,
        ResourceAmountPair cost,
        string currentBonusText,
        string nextBonusText,
        bool isPurchased,
        bool isMaxedOut,
        bool canAfford,
        bool prerequisitesMet)
    {
        this.skillName = skillName;
        this.description = description;
        this.currentLevel = currentLevel;
        this.maxLevel = maxLevel;
        this.cost = cost;
        this.currentBonusText = currentBonusText ?? string.Empty;
        this.nextBonusText = nextBonusText ?? string.Empty;
        this.isPurchased = isPurchased;
        this.isMaxedOut = isMaxedOut;
        this.canAfford = canAfford;
        this.prerequisitesMet = prerequisitesMet;
    }
}
