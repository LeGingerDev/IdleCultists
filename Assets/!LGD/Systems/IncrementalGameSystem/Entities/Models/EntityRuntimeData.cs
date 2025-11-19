using LargeNumbers;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntityRuntimeData
{
    [FoldoutGroup("Identity")]
    public string uniqueId;

    [FoldoutGroup("Identity")]
    public string entityName;

    [FoldoutGroup("Identity")]
    public string blueprintId;

    [FoldoutGroup("State")]
    public EntityState currentState = EntityState.Idle;

    [FoldoutGroup("Location")]
    public string currentZoneId;

    [FoldoutGroup("Location")]
    public SerializableVector3 worldPosition;

    [FoldoutGroup("Location")]
    public SerializableVector3 eulerAngles;

    [FoldoutGroup("Stats")]
    public List<RuntimeStat> runtimeStats = new List<RuntimeStat>();

    // Parameterless constructor for JSON deserialization
    public EntityRuntimeData() { }
    public EntityRuntimeData()
    public void Initialise(EntityBlueprint blueprint)
    {
        uniqueId = Guid.NewGuid().ToString();
        blueprintId = blueprint.id;
        runtimeStats = blueprint.CopyDataToRuntime();
        entityName = blueprint.GetRandomName();
        currentState = EntityState.Idle;
        currentZoneId = null;
        worldPosition = new SerializableVector3(0, 0, 0);
        eulerAngles = new SerializableVector3(0, 0, 0);
    }

    public AlphabeticNotation GetStatValue(StatType statType)
    {
        RuntimeStat stat = runtimeStats.Find(s => s.statType == statType);
        return stat != null ? stat.currentValue : AlphabeticNotation.zero;
    }

    public void SetStatValue(StatType statType, AlphabeticNotation value)
    {
        RuntimeStat stat = runtimeStats.Find(s => s.statType == statType);
        if (stat != null)
        {
            stat.currentValue = value;
        }
    }

    public bool IsAssignedToZone()
    {
        return currentState == EntityState.Assigned && !string.IsNullOrEmpty(currentZoneId);
    }

    public bool CanContributeStats()
    {
        return currentState == EntityState.Assigned;
    }

    public void UpdatePositionFromTransform(Transform transform)
    {
        worldPosition = new SerializableVector3(transform.position);
        eulerAngles = new SerializableVector3(transform.eulerAngles);
    }
}