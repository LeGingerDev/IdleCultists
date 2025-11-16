using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchievementSaveProvider : SaveLoadProviderBase<AchievementRuntimeData>
{
    protected override string GetSaveFileName()
    {
        return "achievements.json";
    }

    protected override IEnumerator CreateDefault()
    {
        _data.Clear();

        RegistryProviderBase<AchievementData> registry = RegistryManager.Instance.GetRegistry<AchievementData>();

        if (registry != null)
        {
            List<AchievementData> allAchievements = registry.GetAllItems();

            foreach (AchievementData achievementData in allAchievements)
            {
                AchievementRuntimeData runtimeData = achievementData.CreateRuntime();
                _data.Add(runtimeData);
            }

            DebugManager.Log($"[Achievement] <color=green>Created default achievement data:</color> {_data.Count} achievements");
        }
        else
        {
            DebugManager.Error("[Achievement] Could not find AchievementData registry to create default data");
        }

        yield return null;
    }

    /// <summary>
    /// Syncs new achievements from the registry into the existing save data.
    /// Call this after loading to ensure any newly added achievements are included.
    /// </summary>
    public IEnumerator SyncWithRegistry()
    {
        RegistryProviderBase<AchievementData> registry = RegistryManager.Instance.GetRegistry<AchievementData>();

        if (registry != null)
        {
            List<AchievementData> allAchievements = registry.GetAllItems();
            int addedCount = 0;

            foreach (AchievementData achievementData in allAchievements)
            {
                // Check if this achievement already exists in save data
                bool exists = _data.Exists(runtime => runtime.id == achievementData.id);

                if (!exists)
                {
                    // New achievement detected - add it with default values
                    AchievementRuntimeData runtimeData = achievementData.CreateRuntime();
                    _data.Add(runtimeData);
                    addedCount++;
                    DebugManager.Log($"[Achievement] <color=yellow>New achievement detected:</color> {achievementData.id}");
                }
            }

                if (addedCount > 0)
            {
                MarkDirty();
                yield return Save(); // Save immediately to persist new achievements
                DebugManager.Log($"[Achievement] <color=green>Synced {addedCount} new achievement(s) from registry</color>");
            }
            else
            {
                DebugManager.Log($"[Achievement] <color=cyan>Registry sync complete:</color> No new achievements to add");
            }
        }
        else
        {
            DebugManager.Error("[Achievement] Could not find AchievementData registry to sync");
        }

        yield return null;
    }
}