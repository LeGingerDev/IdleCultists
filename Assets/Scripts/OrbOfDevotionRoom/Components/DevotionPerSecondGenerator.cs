using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.PickupSystem;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

/// <summary>
/// Generates devotion resources based on DevotionPerSecond stat.
/// Queries StatManager and adds resources to ResourceManager.
/// </summary>
public class DevotionPerSecondGenerator : BaseBehaviour
{
    [SerializeField, FoldoutGroup("Settings")]
    private Resource _devotionResource;

    [SerializeField, FoldoutGroup("Settings")]
    private float _updateInterval = 0.1f;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private AlphabeticNotation _currentDevotionPerSecond;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private AlphabeticNotation _totalDevotionGenerated;

    private float _timeSinceLastGeneration;

    private IEnumerator Start()
    {
        while (true)
        {
            _currentDevotionPerSecond = StatManager.Instance.QueryStat(StatType.DevotionPerSecond);
            yield return new WaitForSeconds(0.2f);
            yield return null;
        }
    }

    private void Update()
    {
        _timeSinceLastGeneration += Time.deltaTime;

        if (_timeSinceLastGeneration >= _updateInterval)
        {
            GenerateDevotion(_timeSinceLastGeneration);
            _timeSinceLastGeneration = 0f;
        }
    }

    private void GenerateDevotion(float deltaTime)
    {
        _currentDevotionPerSecond = StatManager.Instance.QueryStat(StatType.DevotionPerSecond);

        if (_currentDevotionPerSecond.isZero)
            return;

        AlphabeticNotation amountToGenerate = _currentDevotionPerSecond * deltaTime;
        ResourceManager.Instance.AddResource(_devotionResource, amountToGenerate);
        _totalDevotionGenerated += amountToGenerate;
    }

    // Listen to stats recalculation
    [Topic(StatEventIds.ON_STATS_RECALCULATED)]
    public void OnStatsRecalculated(object sender)
    {
        _currentDevotionPerSecond = StatManager.Instance.QueryStat(StatType.DevotionPerSecond);
    }

    // ALSO listen to entity assignment events to catch immediate changes
    [Topic(PickupEventIds.ON_ENTITY_ASSIGNED_TO_ZONE)]
    public void OnEntityAssignedToZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        // Refresh rate immediately when entity assigned
        _currentDevotionPerSecond = StatManager.Instance.QueryStat(StatType.DevotionPerSecond);
    }

    [Topic(PickupEventIds.ON_ENTITY_REMOVED_FROM_ZONE)]
    public void OnEntityRemovedFromZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        // Refresh rate immediately when entity removed
        _currentDevotionPerSecond = StatManager.Instance.QueryStat(StatType.DevotionPerSecond);
    }

    #region Debug Helpers

    [Button("Force Generate (1 second)"), FoldoutGroup("Debug")]
    private void DebugForceGenerate()
    {
        GenerateDevotion(1f);
        Debug.Log($"Generated {_currentDevotionPerSecond} devotion");
    }

    [Button("Log Current Rate"), FoldoutGroup("Debug")]
    private void DebugLogRate()
    {
        AlphabeticNotation rate = StatManager.Instance.QueryStat(StatType.DevotionPerSecond);
        Debug.Log($"=== DEVOTION GENERATION ===");
        Debug.Log($"Current Rate: {rate}/second");
        Debug.Log($"Total Generated This Session: {_totalDevotionGenerated}");
    }

    #endregion
}