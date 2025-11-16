using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AchievementUtilities
{
    private static RegistryProviderBase<AchievementData> _registry;

    private static RegistryProviderBase<AchievementData> GetRegistry()
    {
        if (_registry == null)
        {
            _registry = RegistryManager.Instance.GetRegistry<AchievementData>();
        }
        return _registry;
    }

    public static AchievementData GetAchievementData(string achievementId)
    {
        var registry = GetRegistry();
        if (registry == null)
        {
            DebugManager.Error("[Achievement] Achievement registry not found!");
            return null;
        }

        return registry.GetItemById(achievementId);
    }

    public static AchievementTrackingType GetTrackingType(string achievementId)
    {
        var data = GetAchievementData(achievementId);
        return data != null ? data.trackingType : AchievementTrackingType.None;
    }

    public static AchievementTrackingMetric GetTrackingMetric(string achievementId)
    {
        var data = GetAchievementData(achievementId);
        return data != null ? data.trackingMetric : AchievementTrackingMetric.Current;
    }

    public static Resource GetTrackedResource(string achievementId)
    {
        var data = GetAchievementData(achievementId);
        return data != null ? data.trackedResource : null;
    }

    public static bool IsTrackingResource(string achievementId, Resource resource)
    {
        var data = GetAchievementData(achievementId);
        if (data == null) return false;

        return data.trackingType == AchievementTrackingType.resourceCollected
               && data.trackedResource == resource;
    }

    public static bool IsTrackingType(string achievementId, AchievementTrackingType trackingType)
    {
        var data = GetAchievementData(achievementId);
        return data != null && data.trackingType == trackingType;
    }

    #region AchievementRuntimeData Extensions

    /// <summary>
    /// Get the full AchievementData for this runtime instance
    /// </summary>
    public static AchievementData GetData(this AchievementRuntimeData runtime)
    {
        return GetAchievementData(runtime.id);
    }

    /// <summary>
    /// Get the tracking type for this achievement
    /// </summary>
    public static AchievementTrackingType GetTrackingType(this AchievementRuntimeData runtime)
    {
        return GetTrackingType(runtime.id);
    }

    /// <summary>
    /// Get the tracking metric for this achievement
    /// </summary>
    public static AchievementTrackingMetric GetTrackingMetric(this AchievementRuntimeData runtime)
    {
        return GetTrackingMetric(runtime.id);
    }

    /// <summary>
    /// Get the tracked resource for this achievement (null if not tracking a resource)
    /// </summary>
    public static Resource GetTrackedResource(this AchievementRuntimeData runtime)
    {
        return GetTrackedResource(runtime.id);
    }

    /// <summary>
    /// Check if this achievement is tracking a specific resource
    /// </summary>
    public static bool IsTrackingResource(this AchievementRuntimeData runtime, Resource resource)
    {
        return IsTrackingResource(runtime.id, resource);
    }

    /// <summary>
    /// Check if this achievement is tracking a specific type
    /// </summary>
    public static bool IsTrackingType(this AchievementRuntimeData runtime, AchievementTrackingType trackingType)
    {
        return IsTrackingType(runtime.id, trackingType);
    }

    /// <summary>
    /// Get completion percentage (0-1)
    /// </summary>
    public static float GetCompletionPercentage(this AchievementRuntimeData runtime)
    {
        if (runtime.goal.isZero) return 0f;
        return (float)(runtime.progress / runtime.goal);
    }

    /// <summary>
    /// Get completion percentage as 0-100
    /// </summary>
    public static float GetCompletionPercent(this AchievementRuntimeData runtime)
    {
        return runtime.GetCompletionPercentage() * 100f;
    }

    /// <summary>
    /// Check if this achievement is complete (may not be unlocked yet due to async)
    /// </summary>
    public static bool IsComplete(this AchievementRuntimeData runtime)
    {
        return runtime.progress >= runtime.goal;
    }

    #endregion

    #region Resource Extensions

    /// <summary>
    /// Get all achievements that track this specific resource
    /// </summary>
    public static List<AchievementRuntimeData> GetAchievementsTrackingResource(this Resource resource)
    {
        var allAchievements = AchievementManager.Instance.GetAllAchievements();
        return allAchievements.Where(a => a.IsTrackingResource(resource)).ToList();
    }

    /// <summary>
    /// Get all unlocked achievements for this resource
    /// </summary>
    public static List<AchievementRuntimeData> GetUnlockedAchievementsForResource(this Resource resource)
    {
        return resource.GetAchievementsTrackingResource()
            .Where(a => a.isUnlocked)
            .ToList();
    }

    #endregion

    #region Collection Extensions

    /// <summary>
    /// Filter achievements by tracking type
    /// </summary>
    public static List<AchievementRuntimeData> WithTrackingType(
        this IEnumerable<AchievementRuntimeData> achievements,
        AchievementTrackingType trackingType)
    {
        return achievements.Where(a => a.IsTrackingType(trackingType)).ToList();
    }

    /// <summary>
    /// Filter achievements by tracking metric
    /// </summary>
    public static List<AchievementRuntimeData> WithTrackingMetric(
        this IEnumerable<AchievementRuntimeData> achievements,
        AchievementTrackingMetric trackingMetric)
    {
        return achievements.Where(a => a.GetTrackingMetric() == trackingMetric).ToList();
    }

    /// <summary>
    /// Filter achievements tracking a specific resource
    /// </summary>
    public static List<AchievementRuntimeData> TrackingResource(
        this IEnumerable<AchievementRuntimeData> achievements,
        Resource resource)
    {
        return achievements.Where(a => a.IsTrackingResource(resource)).ToList();
    }

    /// <summary>
    /// Get achievements that are close to completion (within percentage)
    /// </summary>
    public static List<AchievementRuntimeData> NearCompletion(
        this IEnumerable<AchievementRuntimeData> achievements,
        float percentageThreshold = 0.8f)
    {
        return achievements
            .Where(a => !a.isUnlocked && a.GetCompletionPercentage() >= percentageThreshold)
            .ToList();
    }

    /// <summary>
    /// Order achievements by completion percentage (descending)
    /// </summary>
    public static List<AchievementRuntimeData> OrderByProgress(
        this IEnumerable<AchievementRuntimeData> achievements)
    {
        return achievements.OrderByDescending(a => a.GetCompletionPercentage()).ToList();
    }

    #endregion
}