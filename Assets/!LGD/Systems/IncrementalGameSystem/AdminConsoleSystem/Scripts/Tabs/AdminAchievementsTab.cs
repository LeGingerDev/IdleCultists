using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Admin tab for viewing and managing achievements
/// </summary>
public class AdminAchievementsTab : AdminTabBase
{
    private List<AchievementRuntimeData> _allAchievements = new List<AchievementRuntimeData>();
    private Dictionary<string, AchievementBlueprint> _achievementBlueprintCache = new Dictionary<string, AchievementBlueprint>();

    public override void RefreshData()
    {
        if (AchievementManager.Instance != null && AchievementManager.Instance.IsInitialized())
        {
            _allAchievements = AchievementManager.Instance.GetAllAchievements();

            // Cache blueprints
            var achievementRegistry = RegistryManager.Instance?.GetRegistry<AchievementBlueprint>() as AchievementRegistry;
            if (achievementRegistry != null)
            {
                _achievementBlueprintCache.Clear();
                foreach (var runtime in _allAchievements)
                {
                    var blueprint = achievementRegistry.GetItemById(runtime.achievementId);
                    if (blueprint != null)
                    {
                        _achievementBlueprintCache[runtime.achievementId] = blueprint;
                    }
                }
            }
        }
    }

    public override void DrawTab()
    {
        GUILayout.Label("Achievement Management", HeaderStyle);
        GUILayout.Space(5);

        if (AchievementManager.Instance == null)
        {
            GUILayout.Label("AchievementManager not found.");
            return;
        }

        if (_allAchievements.Count == 0)
        {
            GUILayout.Label("No achievements found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", ButtonStyle))
        {
            RefreshData();
        }
        if (GUILayout.Button("Unlock All", ButtonStyle))
        {
            UnlockAllAchievements();
        }
        if (GUILayout.Button("Lock All", ButtonStyle))
        {
            LockAllAchievements();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Stats
        int completedCount = _allAchievements.Count(a => a.isComplete);
        GUILayout.Label($"Completed: {completedCount} / {_allAchievements.Count}");

        GUILayout.Space(10);

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(400));

        foreach (var runtimeData in _allAchievements)
        {
            if (!_achievementBlueprintCache.TryGetValue(runtimeData.achievementId, out AchievementBlueprint blueprint))
                continue;

            GUILayout.BeginVertical(BoxStyle);

            GUILayout.Label($"{blueprint.displayName}", HeaderStyle);
            GUILayout.Label($"ID: {blueprint.achievementId}");
            GUILayout.Label($"Status: {(runtimeData.isComplete ? "COMPLETED" : "LOCKED")}");

            if (runtimeData.isComplete)
            {
                GUILayout.Label($"Completed At: {runtimeData.completionTime}");
            }

            GUILayout.BeginHorizontal();

            if (runtimeData.isComplete)
            {
                if (GUILayout.Button("Lock", SmallButtonStyle, GUILayout.Width(80)))
                {
                    LockAchievement(runtimeData);
                }
            }
            else
            {
                if (GUILayout.Button("Unlock", SmallButtonStyle, GUILayout.Width(80)))
                {
                    UnlockAchievement(runtimeData);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }

    private void UnlockAchievement(AchievementRuntimeData runtime)
    {
        AchievementManager.Instance.CompleteAchievement(runtime.achievementId);
        RefreshData();
        DebugManager.Log($"[Admin] Unlocked achievement: {runtime.achievementId}");
    }

    private void LockAchievement(AchievementRuntimeData runtime)
    {
        runtime.isComplete = false;
        runtime.completionTime = System.DateTime.MinValue;
        Console.StartCoroutine(AchievementManager.Instance.ManualSave());
        RefreshData();
        DebugManager.Log($"[Admin] Locked achievement: {runtime.achievementId}");
    }

    private void UnlockAllAchievements()
    {
        foreach (var achievement in _allAchievements)
        {
            if (!achievement.isComplete)
            {
                AchievementManager.Instance.CompleteAchievement(achievement.achievementId);
            }
        }
        RefreshData();
        DebugManager.Log("[Admin] Unlocked all achievements");
    }

    private void LockAllAchievements()
    {
        foreach (var achievement in _allAchievements)
        {
            achievement.isComplete = false;
            achievement.completionTime = System.DateTime.MinValue;
        }
        Console.StartCoroutine(AchievementManager.Instance.ManualSave());
        RefreshData();
        DebugManager.Log("[Admin] Locked all achievements");
    }
}
