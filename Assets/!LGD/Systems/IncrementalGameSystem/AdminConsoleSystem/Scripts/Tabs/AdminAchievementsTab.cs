using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Admin tab for viewing and managing achievements
/// </summary>
public class AdminAchievementsTab : AdminTabBase
{
    private List<AchievementRuntimeData> _allAchievements = new List<AchievementRuntimeData>();
    private Dictionary<string, AchievementData> _achievementDataCache = new Dictionary<string, AchievementData>();

    public override void RefreshData()
    {
        if (AchievementManager.Instance != null)
        {
            _allAchievements = AchievementManager.Instance.GetAllAchievements();

            // Cache achievement data (blueprints)
            var achievementRegistry = RegistryManager.Instance?.GetRegistry<AchievementData>();
            if (achievementRegistry != null)
            {
                _achievementDataCache.Clear();
                foreach (var runtime in _allAchievements)
                {
                    var data = achievementRegistry.GetItemById(runtime.id);
                    if (data != null)
                    {
                        _achievementDataCache[runtime.id] = data;
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
        int completedCount = _allAchievements.Count(a => a.isUnlocked);
        GUILayout.Label($"Unlocked: {completedCount} / {_allAchievements.Count}");

        GUILayout.Space(10);

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(400));

        foreach (var runtimeData in _allAchievements)
        {
            if (!_achievementDataCache.TryGetValue(runtimeData.id, out AchievementData data))
                continue;

            GUILayout.BeginVertical(BoxStyle);

            GUILayout.Label($"{data.title}", HeaderStyle);
            GUILayout.Label($"ID: {data.id}");
            GUILayout.Label($"Status: {(runtimeData.isUnlocked ? "UNLOCKED" : "LOCKED")}");
            GUILayout.Label($"Progress: {runtimeData.progress} / {runtimeData.goal} ({runtimeData.GetCompletionPercent():F1}%)");

            GUILayout.BeginHorizontal();

            if (runtimeData.isUnlocked)
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
        // UnlockAchievement is private, so manually set progress to trigger unlock
        runtime.progress = runtime.goal;
        runtime.isUnlocked = true;
        Console.StartCoroutine(AchievementManager.Instance.ManualSave());
        RefreshData();
        DebugManager.Log($"[Admin] Unlocked achievement: {runtime.id}");
    }

    private void LockAchievement(AchievementRuntimeData runtime)
    {
        runtime.isUnlocked = false;
        runtime.progress = new LargeNumbers.AlphabeticNotation(0);
        Console.StartCoroutine(AchievementManager.Instance.ManualSave());
        RefreshData();
        DebugManager.Log($"[Admin] Locked achievement: {runtime.id}");
    }

    private void UnlockAllAchievements()
    {
        foreach (var achievement in _allAchievements)
        {
            if (!achievement.isUnlocked)
            {
                // UnlockAchievement is private, so manually set progress and unlock flag
                achievement.progress = achievement.goal;
                achievement.isUnlocked = true;
            }
        }
        Console.StartCoroutine(AchievementManager.Instance.ManualSave());
        RefreshData();
        DebugManager.Log("[Admin] Unlocked all achievements");
    }

    private void LockAllAchievements()
    {
        foreach (var achievement in _allAchievements)
        {
            achievement.isUnlocked = false;
            achievement.progress = new LargeNumbers.AlphabeticNotation(0);
        }
        Console.StartCoroutine(AchievementManager.Instance.ManualSave());
        RefreshData();
        DebugManager.Log("[Admin] Locked all achievements");
    }
}
