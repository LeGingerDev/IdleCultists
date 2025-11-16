using LargeNumbers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Extension methods for Entity-related classes.
/// Provides convenient access to common entity operations.
/// </summary>
public static class EntityExtensions
{
    #region EntityRuntimeData Extensions

    /// <summary>
    /// Attempts to find the EntityController GameObject for this runtime data.
    /// WARNING: This searches all EntityControllers in the scene - cache the result if using frequently!
    /// </summary>
    public static EntityController GetController(this EntityRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return null;

        EntityController[] allControllers = Object.FindObjectsOfType<EntityController>();

        foreach (EntityController controller in allControllers)
        {
            if (controller.RuntimeData != null && controller.RuntimeData.uniqueId == runtimeData.uniqueId)
            {
                return controller;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the EntityBlueprint for this runtime data from the registry.
    /// </summary>
    public static EntityBlueprint GetBlueprint(this EntityRuntimeData runtimeData)
    {
        if (runtimeData == null || string.IsNullOrEmpty(runtimeData.blueprintId))
            return null;

        EntityRegistry registry = RegistryManager.Instance.GetRegistry<EntityBlueprint>() as EntityRegistry;
        return registry?.GetItemById(runtimeData.blueprintId);
    }

    /// <summary>
    /// Checks if this entity is currently idle (not assigned to any zone).
    /// </summary>
    public static bool IsIdle(this EntityRuntimeData runtimeData)
    {
        return runtimeData != null && runtimeData.currentState == EntityState.Idle;
    }

    /// <summary>
    /// Checks if this entity is currently in transit (being dragged).
    /// </summary>
    public static bool IsInTransit(this EntityRuntimeData runtimeData)
    {
        return runtimeData != null && runtimeData.currentState == EntityState.InTransit;
    }

    /// <summary>
    /// Checks if this entity has a valid stat of the given type.
    /// </summary>
    public static bool HasStat(this EntityRuntimeData runtimeData, StatType statType)
    {
        if (runtimeData == null)
            return false;

        return runtimeData.runtimeStats.Exists(s => s.statType == statType);
    }

    /// <summary>
    /// Gets the distance from this entity to a world position.
    /// </summary>
    public static float GetDistanceToPosition(this EntityRuntimeData runtimeData, Vector3 position)
    {
        if (runtimeData == null)
            return float.MaxValue;

        return Vector3.Distance(runtimeData.worldPosition.ToVector3(), position);
    }

    /// <summary>
    /// Gets the distance from this entity to another entity.
    /// </summary>
    public static float GetDistanceToEntity(this EntityRuntimeData runtimeData, EntityRuntimeData otherEntity)
    {
        if (runtimeData == null || otherEntity == null)
            return float.MaxValue;

        return Vector3.Distance(runtimeData.worldPosition.ToVector3(), otherEntity.worldPosition.ToVector3());
    }

    #endregion

    #region EntityManager Extensions

    /// <summary>
    /// Gets all entities within a certain radius of a position.
    /// </summary>
    public static List<EntityRuntimeData> GetEntitiesInRadius(this EntityManager manager, Vector3 position, float radius)
    {
        if (manager == null)
            return new List<EntityRuntimeData>();

        IReadOnlyList<EntityRuntimeData> allEntities = manager.GetAllEntityRuntimeData();
        List<EntityRuntimeData> entitiesInRadius = new List<EntityRuntimeData>();

        float radiusSquared = radius * radius;

        foreach (EntityRuntimeData entity in allEntities)
        {
            float distanceSquared = (entity.worldPosition.ToVector3() - position).sqrMagnitude;

            if (distanceSquared <= radiusSquared)
            {
                entitiesInRadius.Add(entity);
            }
        }

        return entitiesInRadius;
    }

    /// <summary>
    /// Gets the closest entity to a position, optionally filtered by state.
    /// </summary>
    public static EntityRuntimeData GetClosestEntity(this EntityManager manager, Vector3 position, EntityState? filterByState = null)
    {
        if (manager == null)
            return null;

        IReadOnlyList<EntityRuntimeData> allEntities = manager.GetAllEntityRuntimeData();
        EntityRuntimeData closestEntity = null;
        float closestDistance = float.MaxValue;

        foreach (EntityRuntimeData entity in allEntities)
        {
            // Filter by state if specified
            if (filterByState.HasValue && entity.currentState != filterByState.Value)
                continue;

            float distance = Vector3.Distance(entity.worldPosition.ToVector3(), position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEntity = entity;
            }
        }

        return closestEntity;
    }

    /// <summary>
    /// Gets all idle entities (not assigned to zones).
    /// </summary>
    public static List<EntityRuntimeData> GetIdleEntities(this EntityManager manager)
    {
        if (manager == null)
            return new List<EntityRuntimeData>();

        return manager.GetEntitiesByState(EntityState.Idle);
    }

    /// <summary>
    /// Gets all assigned entities (working in zones).
    /// </summary>
    public static List<EntityRuntimeData> GetAssignedEntities(this EntityManager manager)
    {
        if (manager == null)
            return new List<EntityRuntimeData>();

        return manager.GetEntitiesByState(EntityState.Assigned);
    }

    /// <summary>
    /// Checks if any entities exist with the given blueprint ID.
    /// </summary>
    public static bool HasEntitiesOfType(this EntityManager manager, string blueprintId)
    {
        if (manager == null || string.IsNullOrEmpty(blueprintId))
            return false;

        return manager.GetEntitiesByBlueprintId(blueprintId).Count > 0;
    }

    /// <summary>
    /// Counts how many entities exist with the given blueprint ID.
    /// </summary>
    public static int CountEntitiesOfType(this EntityManager manager, string blueprintId)
    {
        if (manager == null || string.IsNullOrEmpty(blueprintId))
            return 0;

        return manager.GetEntitiesByBlueprintId(blueprintId).Count;
    }

    /// <summary>
    /// Gets the total value of a specific stat across all entities.
    /// </summary>
    public static AlphabeticNotation GetTotalStatValue(this EntityManager manager, StatType statType)
    {
        if (manager == null)
            return AlphabeticNotation.zero;

        IReadOnlyList<EntityRuntimeData> allEntities = manager.GetAllEntityRuntimeData();
        AlphabeticNotation total = AlphabeticNotation.zero;

        foreach (EntityRuntimeData entity in allEntities)
        {
            total += entity.GetStatValue(statType);
        }

        return total;
    }

    /// <summary>
    /// Gets the total value of a specific stat across entities in a specific state.
    /// </summary>
    public static AlphabeticNotation GetTotalStatValueByState(this EntityManager manager, StatType statType, EntityState state)
    {
        if (manager == null)
            return AlphabeticNotation.zero;

        List<EntityRuntimeData> entitiesInState = manager.GetEntitiesByState(state);
        AlphabeticNotation total = AlphabeticNotation.zero;

        foreach (EntityRuntimeData entity in entitiesInState)
        {
            total += entity.GetStatValue(statType);
        }

        return total;
    }

    #endregion

    #region Collection Extensions

    /// <summary>
    /// Filters entities by those that have a specific stat type.
    /// </summary>
    public static List<EntityRuntimeData> WithStat(this IEnumerable<EntityRuntimeData> entities, StatType statType)
    {
        return entities.Where(e => e.HasStat(statType)).ToList();
    }

    /// <summary>
    /// Filters entities by those in a specific state.
    /// </summary>
    public static List<EntityRuntimeData> InState(this IEnumerable<EntityRuntimeData> entities, EntityState state)
    {
        return entities.Where(e => e.currentState == state).ToList();
    }

    /// <summary>
    /// Filters entities by blueprint ID.
    /// </summary>
    public static List<EntityRuntimeData> OfType(this IEnumerable<EntityRuntimeData> entities, string blueprintId)
    {
        return entities.Where(e => e.blueprintId == blueprintId).ToList();
    }

    /// <summary>
    /// Sorts entities by distance to a position (closest first).
    /// </summary>
    public static List<EntityRuntimeData> SortedByDistanceTo(this IEnumerable<EntityRuntimeData> entities, Vector3 position)
    {
        return entities.OrderBy(e => e.GetDistanceToPosition(position)).ToList();
    }

    /// <summary>
    /// Gets the sum of a specific stat across this collection of entities.
    /// </summary>
    public static AlphabeticNotation SumStat(this IEnumerable<EntityRuntimeData> entities, StatType statType)
    {
        AlphabeticNotation total = AlphabeticNotation.zero;

        foreach (EntityRuntimeData entity in entities)
        {
            total += entity.GetStatValue(statType);
        }

        return total;
    }

    #endregion

    #region Static Utility Methods (Direct Instance Access)

    /// <summary>
    /// Gets all entities within a certain radius of a position.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static List<EntityRuntimeData> GetEntitiesInRadius(Vector3 position, float radius)
    {
        return EntityManager.Instance.GetEntitiesInRadius(position, radius);
    }

    /// <summary>
    /// Gets the closest entity to a position, optionally filtered by state.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static EntityRuntimeData GetClosestEntity(Vector3 position, EntityState? filterByState = null)
    {
        return EntityManager.Instance.GetClosestEntity(position, filterByState);
    }

    /// <summary>
    /// Gets all idle entities (not assigned to zones).
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static List<EntityRuntimeData> GetIdleEntities()
    {
        return EntityManager.Instance.GetIdleEntities();
    }

    /// <summary>
    /// Gets all assigned entities (working in zones).
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static List<EntityRuntimeData> GetAssignedEntities()
    {
        return EntityManager.Instance.GetAssignedEntities();
    }

    /// <summary>
    /// Checks if any entities exist with the given blueprint ID.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static bool HasEntitiesOfType(string blueprintId)
    {
        return EntityManager.Instance.HasEntitiesOfType(blueprintId);
    }

    /// <summary>
    /// Counts how many entities exist with the given blueprint ID.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static int CountEntitiesOfType(string blueprintId)
    {
        return EntityManager.Instance.CountEntitiesOfType(blueprintId);
    }

    /// <summary>
    /// Gets the total value of a specific stat across all entities.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static AlphabeticNotation GetTotalStatValue(StatType statType)
    {
        return EntityManager.Instance.GetTotalStatValue(statType);
    }

    /// <summary>
    /// Gets the total value of a specific stat across entities in a specific state.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static AlphabeticNotation GetTotalStatValueByState(StatType statType, EntityState state)
    {
        return EntityManager.Instance.GetTotalStatValueByState(statType, state);
    }

    /// <summary>
    /// Gets all entities of a specific blueprint type.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static List<EntityRuntimeData> GetEntitiesOfType(string blueprintId)
    {
        return EntityManager.Instance.GetEntitiesByBlueprintId(blueprintId);
    }

    /// <summary>
    /// Gets all entities in a specific state.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static List<EntityRuntimeData> GetEntitiesByState(EntityState state)
    {
        return EntityManager.Instance.GetEntitiesByState(state);
    }

    /// <summary>
    /// Gets an entity by its unique ID.
    /// Uses EntityManager.Instance directly.
    /// </summary>
    public static EntityRuntimeData GetEntityById(string uniqueId)
    {
        return EntityManager.Instance.GetEntityRuntimeDataById(uniqueId);
    }

    #endregion
}