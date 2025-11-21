using LargeNumbers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Achievement_",menuName ="LGD/Achievements/Create Achievement")]
public class AchievementData : ScriptableObject
{
    [FoldoutGroup("Identity")]
    public string id;
    [FoldoutGroup("Identity")]
    public string title;
    [FoldoutGroup("Identity")]
    public string description;
    [FoldoutGroup("Identity")]
    public Sprite icon;


    [FoldoutGroup("Data")]
    public List<AchievementData> requiredAchievements = new List<AchievementData>();
    [FoldoutGroup("Data")]
    public AchievementTrackingType trackingType;
    [FoldoutGroup("Data")]
    public AchievementTrackingMetric trackingMetric;
    [FoldoutGroup("Data")]
    [ShowIf("@trackingType == AchievementTrackingType.resourceCollected")]
    public Resource trackedResource; // Only used when tracking specific resources
    [FoldoutGroup("Data")]
    public AlphabeticNotation goal;

    [FoldoutGroup("Rewards")]
    public List<ResourceAmountPair> rewards = new List<ResourceAmountPair>();

    [FoldoutGroup("Rewards")]
    [Tooltip("Achievement points granted when this achievement is unlocked")]
    public int achievementPointReward = 0;

    [FoldoutGroup("Rewards")]
    [Tooltip("Purchasables that are unlocked when this achievement is completed (e.g., boombox tracks, features)")]
    public List<BasePurchasable> unlockedPurchasables = new List<BasePurchasable>();

    [FoldoutGroup("Visual")]
    public Color hexColor = Color.white;

    public AchievementRuntimeData CreateRuntime() => new AchievementRuntimeData()
    {
        id = this.id,
        isUnlocked = false,
        progress = new AlphabeticNotation(0),
        goal = this.goal
    };

#if UNITY_EDITOR
    [Button("Rename Asset to Match Display Name"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        if (string.IsNullOrEmpty(title))
        {
            DebugManager.Warning("[Achievement] Display name is empty. Cannot rename asset.");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string newName = $"Achievement_{title}";
        string result = UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);

        if (string.IsNullOrEmpty(result))
        {
            DebugManager.Log($"[Achievement] Successfully renamed asset to: {newName}");
        }
        else
        {
            DebugManager.Error($"[Achievement] Failed to rename asset: {result}");
        }
    }
#endif
}
