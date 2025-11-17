using LGD.Extensions;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension methods and utilities for the Skill Tree system
/// </summary>
public static class SkillTreeExtensions
{
    #region Skill Query Extensions

    /// <summary>
    /// Check if this purchasable is a skill (OneTime or Capped purchase type)
    /// </summary>
    public static bool IsSkill(this BasePurchasable purchasable)
    {
        if (purchasable == null)
            return false;

        // Skills are typically OneTime or Capped (not Infinite purchases of resources)
        return purchasable.purchaseType == PurchaseType.OneTime ||
               purchasable.purchaseType == PurchaseType.Capped;
    }

    /// <summary>
    /// Get the current level of this skill (0 if not purchased)
    /// </summary>
    public static int GetSkillLevel(this BasePurchasable purchasable)
    {
        return purchasable != null ? purchasable.GetPurchaseCount() : 0;
    }

    /// <summary>
    /// Check if this skill is at max level
    /// </summary>
    public static bool IsSkillMaxed(this BasePurchasable purchasable)
    {
        return purchasable != null && purchasable.IsMaxedOut();
    }

    /// <summary>
    /// Check if this skill is unlocked (purchased at least once)
    /// </summary>
    public static bool IsSkillUnlocked(this BasePurchasable purchasable)
    {
        return purchasable != null && purchasable.GetPurchaseCount() > 0;
    }

    /// <summary>
    /// Check if this skill is available for purchase (prerequisites met)
    /// </summary>
    public static bool IsSkillAvailable(this BasePurchasable purchasable)
    {
        if (purchasable == null)
            return false;

        return purchasable.ArePrerequisitesMet() && !purchasable.IsMaxedOut();
    }

    #endregion

    #region Prerequisite Chain Extensions

    /// <summary>
    /// Get all direct prerequisites for this skill
    /// </summary>
    public static List<BasePurchasable> GetPrerequisites(this BasePurchasable purchasable)
    {
        if (purchasable == null || purchasable.prerequisitePurchasables == null)
            return new List<BasePurchasable>();

        return purchasable.prerequisitePurchasables.Where(p => p != null).ToList();
    }

    /// <summary>
    /// Get all skills that depend on this one (reverse prerequisites)
    /// </summary>
    public static List<BasePurchasable> GetDependentSkills(this BasePurchasable purchasable, List<BasePurchasable> allSkills)
    {
        if (purchasable == null || allSkills == null)
            return new List<BasePurchasable>();

        return allSkills.Where(skill =>
            skill != null &&
            skill.prerequisitePurchasables != null &&
            skill.prerequisitePurchasables.Contains(purchasable)
        ).ToList();
    }

    /// <summary>
    /// Get all prerequisites recursively (entire chain up to root)
    /// </summary>
    public static List<BasePurchasable> GetAllPrerequisitesRecursive(this BasePurchasable purchasable)
    {
        HashSet<BasePurchasable> allPrereqs = new HashSet<BasePurchasable>();
        CollectPrerequisitesRecursive(purchasable, allPrereqs);
        return allPrereqs.ToList();
    }

    private static void CollectPrerequisitesRecursive(BasePurchasable purchasable, HashSet<BasePurchasable> collected)
    {
        if (purchasable == null || purchasable.prerequisitePurchasables == null)
            return;

        foreach (BasePurchasable prereq in purchasable.prerequisitePurchasables)
        {
            if (prereq != null && !collected.Contains(prereq))
            {
                collected.Add(prereq);
                CollectPrerequisitesRecursive(prereq, collected);
            }
        }
    }

    /// <summary>
    /// Check if unlocking this skill would unlock any dependent skills
    /// </summary>
    public static bool WouldUnlockDependents(this BasePurchasable purchasable, List<BasePurchasable> allSkills)
    {
        var dependents = purchasable.GetDependentSkills(allSkills);

        foreach (BasePurchasable dependent in dependents)
        {
            // Check if this is the only missing prerequisite
            if (IsLastMissingPrerequisite(purchasable, dependent))
                return true;
        }

        return false;
    }

    private static bool IsLastMissingPrerequisite(BasePurchasable prerequisite, BasePurchasable dependent)
    {
        if (dependent.prerequisitePurchasables == null)
            return false;

        int unmetCount = 0;
        foreach (BasePurchasable prereq in dependent.prerequisitePurchasables)
        {
            if (prereq == null)
                continue;

            if (prereq.GetPurchaseCount() == 0)
            {
                unmetCount++;

                // If there's more than one unmet prerequisite (and it's not our target), return false
                if (unmetCount > 1 || prereq != prerequisite)
                    return false;
            }
        }

        return unmetCount == 1;
    }

    #endregion

    #region Tree Analysis Extensions

    /// <summary>
    /// Get all root skills (skills with no prerequisites)
    /// </summary>
    public static List<BasePurchasable> GetRootSkills(this IEnumerable<BasePurchasable> skills)
    {
        return skills.Where(s =>
            s != null &&
            (s.prerequisitePurchasables == null || s.prerequisitePurchasables.Count == 0)
        ).ToList();
    }

    /// <summary>
    /// Get all leaf skills (skills with no dependents)
    /// </summary>
    public static List<BasePurchasable> GetLeafSkills(this List<BasePurchasable> skills)
    {
        return skills.Where(skill =>
            skill != null &&
            !skills.Any(other =>
                other != null &&
                other.prerequisitePurchasables != null &&
                other.prerequisitePurchasables.Contains(skill)
            )
        ).ToList();
    }

    /// <summary>
    /// Calculate the depth/tier of a skill in the tree (0 = root, 1 = first tier, etc.)
    /// </summary>
    public static int GetSkillDepth(this BasePurchasable purchasable)
    {
        if (purchasable == null)
            return -1;

        if (purchasable.prerequisitePurchasables == null || purchasable.prerequisitePurchasables.Count == 0)
            return 0; // Root skill

        // Depth is 1 + max depth of prerequisites
        int maxPrereqDepth = 0;
        foreach (BasePurchasable prereq in purchasable.prerequisitePurchasables)
        {
            if (prereq != null)
            {
                int prereqDepth = prereq.GetSkillDepth();
                maxPrereqDepth = System.Math.Max(maxPrereqDepth, prereqDepth);
            }
        }

        return maxPrereqDepth + 1;
    }

    /// <summary>
    /// Get the total skill points invested in a list of skills
    /// </summary>
    public static int GetTotalSkillPoints(this IEnumerable<BasePurchasable> skills)
    {
        int total = 0;
        foreach (BasePurchasable skill in skills)
        {
            if (skill != null)
            {
                total += skill.GetPurchaseCount();
            }
        }
        return total;
    }

    #endregion

    #region Validation Extensions

    /// <summary>
    /// Check if refunding this skill would break any dependent skills
    /// </summary>
    public static bool CanSafelyRefund(this BasePurchasable purchasable, List<BasePurchasable> allSkills)
    {
        if (purchasable == null)
            return false;

        // Get all skills that depend on this one
        var dependents = purchasable.GetDependentSkills(allSkills);

        // Check if any dependent is purchased
        foreach (BasePurchasable dependent in dependents)
        {
            if (dependent.GetPurchaseCount() > 0)
                return false; // Can't refund if it would break a purchased skill
        }

        return true;
    }

    /// <summary>
    /// Get all skills that would need to be refunded if this skill is refunded
    /// </summary>
    public static List<BasePurchasable> GetRefundChain(this BasePurchasable purchasable, List<BasePurchasable> allSkills)
    {
        List<BasePurchasable> refundChain = new List<BasePurchasable>();

        if (purchasable == null)
            return refundChain;

        // Recursively collect all dependent purchased skills
        CollectRefundChain(purchasable, allSkills, refundChain);

        return refundChain;
    }

    private static void CollectRefundChain(BasePurchasable skill, List<BasePurchasable> allSkills, List<BasePurchasable> chain)
    {
        var dependents = skill.GetDependentSkills(allSkills);

        foreach (BasePurchasable dependent in dependents)
        {
            if (dependent.GetPurchaseCount() > 0 && !chain.Contains(dependent))
            {
                chain.Add(dependent);
                CollectRefundChain(dependent, allSkills, chain);
            }
        }
    }

    #endregion
}
