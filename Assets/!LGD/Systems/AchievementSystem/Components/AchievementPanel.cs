using LGD.Core.Events;
using LGD.UIelements.Panels;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementPanel : SlidePanel
{
    [SerializeField, FoldoutGroup("References")]
    private AchievementVisualDisplay _achievementDisplayPrefab;
    [SerializeField, FoldoutGroup("References")]
    private Transform _achievementDisplayContainer;

    private List<AchievementVisualDisplay> _visualDisplays = new List<AchievementVisualDisplay>();
    private bool _hasInitialised = false;

    protected override void OnOpen()
    {
        if (!_hasInitialised)
            Initialise();
        else
            UpdateExisting();
    }

    protected override void OnClose()
    {
        UpdateExisting();
    }

    public void Initialise()
    {
        List<AchievementRuntimeData> achievements = AchievementManager.Instance.GetAllAchievements();
        achievements.ForEach(a =>
        {
            AchievementVisualDisplay display = Instantiate(_achievementDisplayPrefab, _achievementDisplayContainer);
            display.Initialise(a);
            _visualDisplays.Add(display);
        });

        _hasInitialised = true;
    }

    public void UpdateExisting()
    {
        _visualDisplays.ForEach(d => d.UpdateDisplay());
    }

    public List<AchievementRuntimeData> GetOrderedRuntimeAchievements()
    {
        List<AchievementRuntimeData> achievements = AchievementManager.Instance.GetAllAchievements();

        // Group by tracking type first
        var groupedByType = achievements.GroupBy(a => a.GetTrackingType());

        List<AchievementRuntimeData> orderedAchievements = new List<AchievementRuntimeData>();

        foreach (var group in groupedByType.OrderBy(g => g.Key))
        {
            // Topologically sort within each tracking type group
            List<AchievementRuntimeData> sortedGroup = TopologicalSort(group.ToList());
            sortedGroup.Reverse();
            orderedAchievements.AddRange(sortedGroup);
        }

        return orderedAchievements;
    }

    private List<AchievementRuntimeData> TopologicalSort(List<AchievementRuntimeData> achievements)
    {
        List<AchievementRuntimeData> sorted = new List<AchievementRuntimeData>();
        HashSet<string> visited = new HashSet<string>();
        HashSet<string> visiting = new HashSet<string>();

        foreach (var achievement in achievements)
        {
            if (!visited.Contains(achievement.id))
            {
                VisitAchievement(achievement, achievements, visited, visiting, sorted);
            }
        }

        return sorted;
    }

    private void VisitAchievement(
        AchievementRuntimeData achievement,
        List<AchievementRuntimeData> allAchievements,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<AchievementRuntimeData> sorted)
    {
        if (visiting.Contains(achievement.id))
        {
            DebugManager.Warning($"[Achievement] Circular dependency detected in achievement: {achievement.id}");
            return;
        }

        if (visited.Contains(achievement.id))
            return;

        visiting.Add(achievement.id);

        // Get the achievement data to check prerequisites
        AchievementData achievementData = achievement.GetData();
        if (achievementData != null && achievementData.requiredAchievements != null)
        {
            foreach (var requiredData in achievementData.requiredAchievements)
            {
                // Find the runtime version of this required achievement
                AchievementRuntimeData requiredRuntime = allAchievements.FirstOrDefault(a => a.id == requiredData.id);
                if (requiredRuntime != null && !visited.Contains(requiredRuntime.id))
                {
                    VisitAchievement(requiredRuntime, allAchievements, visited, visiting, sorted);
                }
            }
        }

        visiting.Remove(achievement.id);
        visited.Add(achievement.id);
        sorted.Add(achievement);
    }


    [Topic(AchievementEventIds.ON_ACHIEVEMENT_UNLOCKED)]
    public void OnAchievementUnlocked(object sender, AchievementRuntimeData runtimeData)
    {
        AchievementVisualDisplay display = _visualDisplays.FirstOrDefault(d => d.Data.id == runtimeData.id);
        if (display != null)
        {
            display.UpdateDisplay();
        }
    }
}
