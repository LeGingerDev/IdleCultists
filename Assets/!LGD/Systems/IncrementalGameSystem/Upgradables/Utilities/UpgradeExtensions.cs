using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension helpers for UpgradeBlueprint and UpgradeRuntimeData.
/// Null-safe wrappers for common upgrade operations.
/// </summary>
public static class UpgradeExtensions
{
    public static int GetCurrentTier(this UpgradeBlueprint blueprint)
    {
        if (blueprint == null) return 0;
        return UpgradeManager.Instance != null ? UpgradeManager.Instance.GetUpgradeTier(blueprint.upgradeId) : 0;
    }

    public static int GetCurrentTier(this string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return 0;
        return UpgradeManager.Instance != null ? UpgradeManager.Instance.GetUpgradeTier(upgradeId) : 0;
    }

    public static int GetNextTierIndex(this UpgradeBlueprint blueprint)
    {
        if (blueprint == null) return 1;
        return blueprint.GetCurrentTier() + 1;
    }

    public static ResourceAmountPair GetCostForTierSafe(this UpgradeBlueprint blueprint, int tier)
    {
        if (blueprint == null) return new ResourceAmountPair(null, AlphabeticNotation.zero);
        return blueprint.GetCostWithResourceForTier(tier);
    }

    public static ResourceAmountPair GetCostForNextTier(this UpgradeBlueprint blueprint)
    {
        if (blueprint == null) return new ResourceAmountPair(null, AlphabeticNotation.zero);
        int nextTier = blueprint.GetNextTierIndex();
        return blueprint.GetCostWithResourceForTier(nextTier);
    }

    public static bool CanAffordNextTier(this UpgradeBlueprint blueprint)
    {
        if (blueprint == null) return false;
        ResourceAmountPair cost = blueprint.GetCostForNextTier();
        ResourceManager rm = ResourceManager.Instance;
        return rm != null && rm.CanSpend(cost);
    }

    public static bool IsMaxedOut(this UpgradeBlueprint blueprint)
    {
        if (blueprint == null) return true;

        if (blueprint.upgradeType == UpgradeType.Infinite)
            return false;

        int current = blueprint.GetCurrentTier();
        return blueprint.maxTier != -1 && current >= blueprint.maxTier;
    }

    /// <summary>
    /// Executes a purchase for the next tier. Optionally removes cost via ResourceManager.
    /// Returns true on success.
    /// </summary>
    public static bool ExecutePurchaseTier(this UpgradeBlueprint blueprint, bool removeCost = true)
    {
        if (blueprint == null) return false;

        UpgradeManager um = UpgradeManager.Instance;
        if (um == null) return false;

        int nextTier = blueprint.GetNextTierIndex();

        if (removeCost)
        {
            ResourceManager rm = ResourceManager.Instance;
            if (rm == null) return false;

            ResourceAmountPair cost = blueprint.GetCostForTierSafe(nextTier);
            if (!rm.CanSpend(cost)) return false;

            if (!rm.RemoveResource(cost))
                return false;
        }

        return um.PurchaseUpgradeTier(blueprint.upgradeId);
    }

    public static UpgradeBlueprint GetBlueprint(this UpgradeRuntimeData runtimeData)
    {
        if (runtimeData == null || string.IsNullOrEmpty(runtimeData.upgradeId)) return null;

        UpgradeRegistry registry = RegistryManager.Instance.GetRegistry<UpgradeBlueprint>() as UpgradeRegistry;
        return registry?.GetItemById(runtimeData.upgradeId);
    }

    public static List<UpgradeBlueprint> ToBlueprints(this IEnumerable<UpgradeRuntimeData> runtimes)
    {
        List<UpgradeBlueprint> result = new List<UpgradeBlueprint>();
        if (runtimes == null) return result;

        UpgradeRegistry registry = RegistryManager.Instance.GetRegistry<UpgradeBlueprint>() as UpgradeRegistry;
        if (registry == null) return result;

        foreach (var runtime in runtimes)
        {
            if (runtime == null) continue;
            var bp = registry.GetItemById(runtime.upgradeId);
            if (bp != null) result.Add(bp);
        }

        return result;
    }

    public static List<UpgradeRuntimeData> ToRuntimeData(this IEnumerable<UpgradeBlueprint> blueprints)
    {
        List<UpgradeRuntimeData> result = new List<UpgradeRuntimeData>();
        if (blueprints == null) return result;

        foreach (var bp in blueprints)
        {
            if (bp == null) continue;
            result.Add(new UpgradeRuntimeData(bp.upgradeId));
        }

        return result;
    }
}
