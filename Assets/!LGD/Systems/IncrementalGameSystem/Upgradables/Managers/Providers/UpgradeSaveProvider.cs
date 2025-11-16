using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeSaveProvider : SaveLoadProviderBase<UpgradeRuntimeData>
{
    protected override string GetSaveFileName()
    {
        return "upgrades.json";
    }

    protected override IEnumerator CreateDefault()
    {
        _data.Clear();

        RegistryProviderBase<UpgradeBlueprint> registry = RegistryManager.Instance.GetRegistry<UpgradeBlueprint>();

        if (registry != null)
        {
            List<UpgradeBlueprint> allUpgrades = registry.GetAllItems();

            foreach (UpgradeBlueprint upgrade in allUpgrades)
            {
                if (upgrade != null)
                {
                    // Create default runtime data: tier 0, inactive
                    UpgradeRuntimeData runtimeData = new UpgradeRuntimeData(upgrade.upgradeId);
                    _data.Add(runtimeData);
                }
            }

            DebugManager.Log($"[IncrementalGame] <color=green>Created default upgrade data:</color> {_data.Count} upgrades");
        }
        else
        {
            Debug.LogError("UpgradeRegistry not found! Cannot create default upgrade data");
        }

        yield return null;
    }

    /// <summary>
    /// Syncs new upgrades from the registry into the existing save data.
    /// Call this after loading to ensure any newly added upgrades are included.
    /// </summary>
    public IEnumerator SyncWithRegistry()
    {
        RegistryProviderBase<UpgradeBlueprint> registry = RegistryManager.Instance.GetRegistry<UpgradeBlueprint>();

        if (registry != null)
        {
            List<UpgradeBlueprint> allUpgrades = registry.GetAllItems();
            int addedCount = 0;

            foreach (UpgradeBlueprint upgrade in allUpgrades)
            {
                if (upgrade == null) continue;

                // Check if this upgrade already exists in save data
                bool exists = _data.Exists(runtime => runtime.upgradeId == upgrade.upgradeId);

                if (!exists)
                {
                    // New upgrade detected - add it with default values
                    UpgradeRuntimeData runtimeData = new UpgradeRuntimeData(upgrade.upgradeId);
                    _data.Add(runtimeData);
                    addedCount++;
                    DebugManager.Log($"[IncrementalGame] <color=yellow>New upgrade detected:</color> {upgrade.upgradeId}");
                }
            }

            if (addedCount > 0)
                {
                    MarkDirty();
                    yield return Save(); // Save immediately to persist new upgrades
                    DebugManager.Log($"[IncrementalGame] <color=green>Synced {addedCount} new upgrade(s) from registry</color>");
                }
                else
                {
                    DebugManager.Log($"[IncrementalGame] <color=cyan>Registry sync complete:</color> No new upgrades to add");
                }
        }
        else
        {
            DebugManager.Error("[IncrementalGame] UpgradeRegistry not found! Cannot sync with registry");
        }

        yield return null;
    }
}