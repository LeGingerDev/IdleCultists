using System.Collections.Generic;
using UnityEngine;

public static class UpgradeUtilities
{
    public static UpgradeBlueprint GetBlueprintById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        UpgradeRegistry registry = RegistryManager.Instance.GetRegistry<UpgradeBlueprint>() as UpgradeRegistry;
        return registry?.GetItemById(id);
    }

    public static UpgradeRuntimeData GetRuntimeData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return UpgradeManager.Instance != null ? UpgradeManager.Instance.GetUpgradeRuntimeData(id) : null;
    }

    public static List<UpgradeBlueprint> GetAllBlueprints()
    {
        UpgradeRegistry registry = RegistryManager.Instance.GetRegistry<UpgradeBlueprint>() as UpgradeRegistry;
        return registry != null ? registry.GetAllItems() : new List<UpgradeBlueprint>();
    }

    public static bool PurchaseUpgradeById(string upgradeId, bool removeCost = true)
    {
        if (string.IsNullOrEmpty(upgradeId)) return false;

        UpgradeBlueprint bp = GetBlueprintById(upgradeId);
        if (bp == null) return false;

        return bp.ExecutePurchaseTier(removeCost);
    }
}
