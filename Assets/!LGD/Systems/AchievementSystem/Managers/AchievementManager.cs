using LGD.Core.Singleton;
using LGD.ResourceSystem.Models;
using LargeNumbers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementManager : MonoSingleton<AchievementManager>
{
    [SerializeField]
    private List<AchievementRuntimeData> _runtimeData = new List<AchievementRuntimeData>();

    [SerializeField]
    private bool _autoSave = true;

    private SaveLoadProviderBase<AchievementRuntimeData> _saveProvider;

    #region Initialization

    public void Start()
    {
        StartCoroutine(InitializeAchievements());
    }

    private IEnumerator InitializeAchievements()
    {
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<AchievementRuntimeData>();

        if (_saveProvider != null)
        {
            yield return _saveProvider.Load();

            // Sync with registry to add any new achievements that were added after the save file was created
            AchievementSaveProvider achievementProvider = _saveProvider as AchievementSaveProvider;
            if (achievementProvider != null)
            {
                yield return achievementProvider.SyncWithRegistry();
            }

            _runtimeData = _saveProvider.GetData();
            DebugManager.Log($"[Achievement] <color=cyan>Achievement Manager initialized:</color> {_runtimeData.Count} achievements loaded");
        }
        else
        {
            DebugManager.Error("[Achievement] Achievement save provider not found! Make sure AchievementSaveProvider is in the scene.");
        }
    }

    #endregion

    #region Public Update Methods

    /// <summary>
    /// Update all achievements of a specific tracking type with a value
    /// Automatically handles Current/MaxReached/Cumulative logic based on the achievement's metric
    /// </summary>
    public void UpdateAchievementsByType(AchievementTrackingType trackingType, AlphabeticNotation value)
    {
        if (trackingType == AchievementTrackingType.None)
        {
            DebugManager.Warning("[Achievement] Cannot update achievements with TrackingType.None");
            return;
        }

        // Find all achievements matching this tracking type
        var matchingAchievements = _runtimeData
            .Where(a => AchievementUtilities.GetTrackingType(a.id) == trackingType)
            .ToList();

        if (matchingAchievements.Count == 0)
            return;

        foreach (var achievement in matchingAchievements)
        {
            UpdateAchievementByMetric(achievement, value);
        }
    }

    /// <summary>
    /// Update resource achievements based on current resource amount
    /// Handles Current/MaxReached metrics (use AddToResourceAchievements for Cumulative)
    /// </summary>
    public void UpdateResourceAchievements(Resource resource, AlphabeticNotation currentAmount)
    {
        if (resource == null) return;

        var matchingAchievements = _runtimeData
            .Where(a => AchievementUtilities.IsTrackingResource(a.id, resource))
            .ToList();

        foreach (var achievement in matchingAchievements)
        {
            AchievementTrackingMetric metric = AchievementUtilities.GetTrackingMetric(achievement.id);

            // Only handle Current and MaxReached here
            if (metric == AchievementTrackingMetric.Cumulative)
                continue;

            UpdateAchievementByMetric(achievement, currentAmount);
        }
    }

    /// <summary>
    /// Add to cumulative resource achievements when resources are gained
    /// </summary>
    public void AddToResourceAchievements(Resource resource, AlphabeticNotation addedAmount)
    {
        if (resource == null) return;

        var cumulativeAchievements = _runtimeData
            .Where(a => AchievementUtilities.IsTrackingResource(a.id, resource) &&
                       AchievementUtilities.GetTrackingMetric(a.id) == AchievementTrackingMetric.Cumulative)
            .ToList();

        foreach (var achievement in cumulativeAchievements)
        {
            achievement.progress += addedAmount;
            MarkDirtyAndCheckUnlock(achievement);
        }
    }

    #endregion

    #region Internal Logic

    private void UpdateAchievementByMetric(AchievementRuntimeData achievement, AlphabeticNotation value)
    {
        AchievementTrackingMetric metric = AchievementUtilities.GetTrackingMetric(achievement.id);

        switch (metric)
        {
            case AchievementTrackingMetric.Current:
                achievement.progress = value;
                break;

            case AchievementTrackingMetric.MaxReached:
                if (value > achievement.progress)
                {
                    achievement.progress = value;
                }
                break;

            case AchievementTrackingMetric.Cumulative:
                achievement.progress += value;
                break;
        }

        MarkDirtyAndCheckUnlock(achievement);
    }

    private void MarkDirtyAndCheckUnlock(AchievementRuntimeData achievement)
    {
        if (_saveProvider != null)
        {
            _saveProvider.MarkDirty();
        }

        if (!achievement.isUnlocked && achievement.progress >= achievement.goal)
        {
            UnlockAchievement(achievement.id);
        }
        else
        {
            Publish(AchievementEventIds.ON_ACHIEVEMENT_UPDATED, achievement);
        }
    }

    private void UnlockAchievement(string achievementId)
    {
        AchievementRuntimeData achievement = GetAchievement(achievementId);

        if (achievement != null && !achievement.isUnlocked)
        {
            achievement.isUnlocked = true;
            achievement.progress = achievement.goal;
            DebugManager.Log($"[Achievement] <color=yellow>Achievement Unlocked:</color> {achievementId}");

            // Process rewards (resources, achievement points, unlocked purchasables)
            ProcessAchievementRewards(achievementId);

            Publish(AchievementEventIds.ON_ACHIEVEMENT_UNLOCKED, achievement);

            StartCoroutine(SaveOnUnlock());
        }
    }

    /// <summary>
    /// Process and grant all rewards for an achievement
    /// </summary>
    private void ProcessAchievementRewards(string achievementId)
    {
        AchievementData achievementData = AchievementUtilities.GetAchievementData(achievementId);

        if (achievementData == null)
        {
            DebugManager.Warning($"[Achievement] Could not find achievement data for: {achievementId}");
            return;
        }

        // Grant resource rewards
        if (achievementData.rewards != null && achievementData.rewards.Count > 0)
        {
            foreach (var reward in achievementData.rewards)
            {
                if (reward.resource != null && ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddResource(reward);
                    DebugManager.Log($"[Achievement] <color=green>Granted reward:</color> {reward.amount.FormatWithDecimals()} {reward.resource.displayName}");
                }
            }
        }

        // Grant achievement points (if any)
        if (achievementData.achievementPointReward > 0)
        {
            // Find the achievement points resource
            Resource achievementPointsResource = FindAchievementPointsResource();

            if (achievementPointsResource != null && ResourceManager.Instance != null)
            {
                ResourceAmountPair pointsReward = new ResourceAmountPair(
                    achievementPointsResource,
                    new AlphabeticNotation(achievementData.achievementPointReward)
                );

                ResourceManager.Instance.AddResource(pointsReward);
                DebugManager.Log($"[Achievement] <color=green>Granted achievement points:</color> {achievementData.achievementPointReward}");
            }
            else
            {
                DebugManager.Warning("[Achievement] Achievement Points resource not found! Cannot grant points.");
            }
        }

        // Unlock purchasables (e.g., boombox tracks, features)
        if (achievementData.unlockedPurchasables != null && achievementData.unlockedPurchasables.Count > 0)
        {
            foreach (var purchasable in achievementData.unlockedPurchasables)
            {
                if (purchasable != null && PurchasableManager.Instance != null)
                {
                    // Execute the purchasable with no cost (achievement reward)
                    bool success = purchasable.ExecutePurchase(removeCost: false);

                    if (success)
                    {
                        DebugManager.Log($"[Achievement] <color=green>Unlocked purchasable:</color> {purchasable.displayName}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Find the Achievement Points resource (looks for resource with "achievement" and "point" in name)
    /// </summary>
    private Resource FindAchievementPointsResource()
    {
        if (ResourceManager.Instance == null)
            return null;

        // Try to find a resource with "achievement" and "point" in the name (case insensitive)
        var allResources = ResourceManager.Instance.GetAllResources();

        foreach (var kvp in allResources)
        {
            Resource resource = kvp.Key;
            if (resource != null && resource.displayName != null)
            {
                string name = resource.displayName.ToLower();
                if (name.Contains("achievement") && name.Contains("point"))
                {
                    return resource;
                }
            }
        }

        return null;
    }

    #endregion

    #region Save Methods

    private IEnumerator SaveOnUnlock()
    {
        if (_saveProvider != null)
        {
            _saveProvider.MarkDirty();
            yield return _saveProvider.Save();
        }
    }

    private IEnumerator SaveAchievements()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SetData(_runtimeData);

            if (_autoSave)
            {
                yield return _saveProvider.Save();
            }
        }
    }

    public IEnumerator ManualSave()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SetData(_runtimeData);
            yield return _saveProvider.Save();
        }
    }

    public IEnumerator SaveIfDirty()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SaveIfDirty();
        }
    }

    #endregion

    #region Getters

    public AchievementRuntimeData GetAchievement(string achievementId)
    {
        return _runtimeData.FirstOrDefault(a => a.id == achievementId);
    }

    public List<AchievementRuntimeData> GetAllAchievements()
    {
        return _runtimeData;
    }

    public List<AchievementRuntimeData> GetUnlockedAchievements()
    {
        return _runtimeData.Where(a => a.isUnlocked).ToList();
    }

    public List<AchievementRuntimeData> GetLockedAchievements()
    {
        return _runtimeData.Where(a => !a.isUnlocked).ToList();
    }

    public int GetTotalAchievementCount()
    {
        return _runtimeData.Count;
    }

    public int GetUnlockedAchievementCount()
    {
        return _runtimeData.Count(a => a.isUnlocked);
    }

    #endregion
}
