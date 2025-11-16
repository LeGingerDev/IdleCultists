using LargeNumbers;
using LGD.Core.Singleton;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using LGD.Core;

/// <summary>
/// Tracks stats related to entity existence (not work).
/// Counts capacity usage across ALL entities regardless of their state or zone.
/// </summary>
public class EntityCapacityTracker : BaseBehaviour, IStatProvider
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private AlphabeticNotation _currentCapacityUsed;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private int _totalEntitiesTracked;

    private void Start()
    {
        RegisterWithStatManager();
    }

    private void OnDestroy()
    {
        UnregisterFromStatManager();
    }

    #region IStatProvider Implementation

    public List<StatModifier> GetModifiersForStat(StatType statType)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        // Only handle CapacityCount stat
        if (statType != StatType.CapacityCount)
            return modifiers;

        // Get ALL entities from EntityManager
        IReadOnlyList<EntityRuntimeData> allEntities = EntityManager.Instance.GetAllEntityRuntimeData();

        AlphabeticNotation totalCapacity = AlphabeticNotation.zero;

        foreach (var entity in allEntities)
        {
            // Each entity contributes their CapacityCount value
            AlphabeticNotation entityCapacity = entity.GetStatValue(StatType.CapacityCount);
            totalCapacity += entityCapacity;
        }

        // Cache for debug display
        _currentCapacityUsed = totalCapacity;
        _totalEntitiesTracked = allEntities.Count;

        // Return capacity as additive modifier
        if (!totalCapacity.isZero)
        {
            modifiers.Add(new StatModifier(
                StatType.CapacityCount,
                totalCapacity,
                "entity_capacity_tracker"
            ));
        }

        return modifiers;
    }

    #endregion

    #region StatManager Registration

    private void RegisterWithStatManager()
    {
        if (StatManager.Instance != null)
        {
            StatManager.Instance.RegisterStatProvider(this);
            Debug.Log("[EntityCapacityTracker] Registered with StatManager");
        }
    }

    private void UnregisterFromStatManager()
    {
        if (StatManager.Instance != null)
        {
            StatManager.Instance.UnregisterStatProvider(this);
        }
    }

    #endregion

    #region Topic Listeners

    [Topic(EntityEventIds.ON_ENTITY_RUNTIME_DATA_REGISTERED)]
    public void OnEntityRegistered(object sender, EntityRuntimeData runtimeData)
    {
        // Trigger recalculation when entity spawns
        Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);

        Debug.Log($"[EntityCapacityTracker] Entity registered, recalculating capacity");
    }

    [Topic(EntityEventIds.ON_ENTITY_RUNTIME_DATA_UNREGISTERED)]
    public void OnEntityUnregistered(object sender, EntityRuntimeData runtimeData)
    {
        // Trigger recalculation when entity destroyed
        Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);

        Debug.Log($"[EntityCapacityTracker] Entity unregistered, recalculating capacity");
    }

    #endregion

    #region Debug Helpers

    [Button("Calculate Current Capacity"), FoldoutGroup("Debug")]
    private void DebugCalculateCapacity()
    {
        var modifiers = GetModifiersForStat(StatType.CapacityCount);

        Debug.Log($"=== ENTITY CAPACITY TRACKER ===");
        Debug.Log($"Total Entities: {_totalEntitiesTracked}");
        Debug.Log($"Capacity Used: {_currentCapacityUsed}");

        if (modifiers.Count > 0)
        {
            Debug.Log($"Modifier Value: {modifiers[0].additiveValue}");
        }
    }

    [Button("Log All Entity Capacities"), FoldoutGroup("Debug")]
    private void DebugLogEntityCapacities()
    {
        var allEntities = EntityManager.Instance.GetAllEntityRuntimeData();

        Debug.Log($"=== ENTITY CAPACITY BREAKDOWN ===");
        foreach (var entity in allEntities)
        {
            var capacity = entity.GetStatValue(StatType.CapacityCount);
            Debug.Log($"{entity.entityName} (ID: {entity.uniqueId}): {capacity} capacity");
        }
    }

    #endregion
}