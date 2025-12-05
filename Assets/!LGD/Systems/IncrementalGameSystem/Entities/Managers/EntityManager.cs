using LGD.Core.Events;
using LGD.Core.Singleton;
using LGD.PickupSystem;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure entity database. Tracks all entity runtime data.
/// Does NOT contribute stats - that's handled by zone behaviors.
/// Think of this like UpgradeManager - it just tracks entity data.
/// </summary>
public class EntityManager : MonoSingleton<EntityManager>
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private List<EntityRuntimeData> _allEntityRuntimeData = new List<EntityRuntimeData>();

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private int _totalEntityCount = 0;

    [SerializeField, FoldoutGroup("Settings")]
    private bool _autoSave = true;

    private SaveLoadProviderBase<EntityRuntimeData> _saveProvider;
    private bool _isInitialized = false;
    private EntityRegistry _entityRegistry;

    #region Initialization

    private void Start()
    {
        StartCoroutine(InitializeEntities());
    }

    /// <summary>
    /// Phase 1: Silent load - loads entity data from save file but DOES NOT spawn GameObjects
    /// Call this automatically during bootstrap scene loading
    /// </summary>
    private IEnumerator InitializeEntities()
    {
        _entityRegistry = RegistryManager.Instance.GetRegistry<EntityBlueprint>() as EntityRegistry;
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<EntityRuntimeData>();

        if (_saveProvider != null)
        {
            yield return _saveProvider.Load();

            _allEntityRuntimeData = _saveProvider.GetData();
            _totalEntityCount = _allEntityRuntimeData.Count;
            DebugManager.Log($"[IncrementalGame] <color=cyan>Entity Manager initialized:</color> {_totalEntityCount} entities loaded (silent mode - no GameObjects spawned yet)");
        }
        else
        {
            DebugManager.Warning("[IncrementalGame] Entity save provider not found! Entities will not be saved. Make sure EntitySaveProvider is in the scene.");
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Phase 2: Manual restoration - spawns GameObjects for all loaded entities
    /// Call this AFTER game scene is ready (after zones exist, before zone reconnection)
    /// </summary>
    public IEnumerator RestoreEntities()
    {
        if (_allEntityRuntimeData.Count == 0)
        {
            DebugManager.Log("[IncrementalGame] <color=cyan>No entities to restore</color>");
            yield break;
        }

        DebugManager.Log($"[IncrementalGame] <color=yellow>Restoring {_allEntityRuntimeData.Count} entity GameObject(s)...</color>");

        int restoredCount = 0;

        // Make a copy to avoid modification during iteration
        List<EntityRuntimeData> entitiesToRestore = new List<EntityRuntimeData>(_allEntityRuntimeData);

        foreach (EntityRuntimeData runtimeData in entitiesToRestore)
        {
            // Get blueprint from registry
            EntityBlueprint blueprint = _entityRegistry.GetItemById(runtimeData.blueprintId);

            if (blueprint == null)
            {
                Debug.LogError($"Blueprint not found for entity {runtimeData.uniqueId} (blueprintId: {runtimeData.blueprintId})");
                continue;
            }

            if (blueprint.prefab == null)
            {
                Debug.LogError($"Blueprint {blueprint.id} has no prefab assigned!");
                continue;
            }

            // Spawn entity GameObject at saved position
            EntityController controller = Instantiate(
                blueprint.prefab,
                runtimeData.worldPosition.ToVector3(),
                Quaternion.Euler(runtimeData.eulerAngles.ToVector3())
            );

            // Initialize with loaded runtime data
            controller.Initialise(runtimeData, blueprint);

            restoredCount++;

            // Small yield to prevent frame hitches if spawning many entities
            if (restoredCount % 10 == 0)
            {
                yield return null;
            }
        }

        DebugManager.Log($"[IncrementalGame] <color=green>Entity restoration complete:</color> {restoredCount} GameObject(s) spawned");
    }

    #endregion

    #region Registration

    public void RegisterEntityRuntimeData(EntityRuntimeData runtimeData)
    {
        bool alreadyExists = _allEntityRuntimeData.Contains(runtimeData);

        if (!alreadyExists)
        {
            // FRESH SPAWN MODE: New entity, add to list
            _allEntityRuntimeData.Add(runtimeData);
            MarkDirty(); // Only mark dirty for NEW entities
        }

        // ALWAYS update count and publish event (works for both modes)
        _totalEntityCount = _allEntityRuntimeData.Count;
        ServiceBus.Publish(EntityEventIds.ON_ENTITY_RUNTIME_DATA_REGISTERED, this, runtimeData);

        string mode = alreadyExists ? "Restored" : "Registered NEW";
        DebugManager.Log($"[IncrementalGame] [EntityManager] {mode} entity: {runtimeData.blueprintId} (Total: {_totalEntityCount})");
    }

    public void UnregisterEntityRuntimeData(EntityRuntimeData runtimeData)
    {
        if (!_allEntityRuntimeData.Contains(runtimeData))
        {
            DebugManager.Warning($"[IncrementalGame] [EntityManager] Attempted to unregister non-existent runtime data: {runtimeData.uniqueId}");
            return;
        }

        _allEntityRuntimeData.Remove(runtimeData);
        _totalEntityCount = _allEntityRuntimeData.Count;

        MarkDirty();

        ServiceBus.Publish(EntityEventIds.ON_ENTITY_RUNTIME_DATA_UNREGISTERED, this, runtimeData);

        DebugManager.Log($"[IncrementalGame] [EntityManager] Unregistered entity: {runtimeData.blueprintId} (Total: {_totalEntityCount})");
    }

    #endregion

    #region Query Methods

    public IReadOnlyList<EntityRuntimeData> GetAllEntityRuntimeData() => _allEntityRuntimeData.AsReadOnly();

    public int GetEntityCount() => _totalEntityCount;

    public EntityRuntimeData GetEntityRuntimeDataById(string uniqueId)
    {
        return _allEntityRuntimeData.Find(data => data.uniqueId == uniqueId);
    }

    public List<EntityRuntimeData> GetEntitiesByBlueprintId(string blueprintId)
    {
        return _allEntityRuntimeData.FindAll(data => data.blueprintId == blueprintId);
    }

    public List<EntityRuntimeData> GetEntitiesByState(EntityState state)
    {
        return _allEntityRuntimeData.FindAll(data => data.currentState == state);
    }

    /// <summary>
    /// Get all entities assigned to a specific zone.
    /// This is the key method that zone behaviors use to calculate their contributions.
    /// </summary>
    public List<EntityRuntimeData> GetEntitiesInZone(string zoneId)
    {
        return _allEntityRuntimeData.FindAll(data =>
            data.currentState == EntityState.Assigned &&
            data.currentZoneId == zoneId
        );
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    #endregion

    #region Topic Listeners

    [Topic(EntityEventIds.ON_ENTITY_SPAWNED)]
    public void OnEntitySpawned(object sender, EntityRuntimeData runtimeData, bool fromLoading)
    {
        RegisterEntityRuntimeData(runtimeData);
    }

    [Topic(EntityEventIds.ON_ENTITY_DESTROYED)]
    public void OnEntityDestroyed(object sender, EntityRuntimeData runtimeData)
    {
        UnregisterEntityRuntimeData(runtimeData);
    }

    [Topic(PickupEventIds.ON_ENTITY_ASSIGNED_TO_ZONE)]
    public void OnEntityAssignedToZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        DebugManager.Log($"[IncrementalGame] [EntityManager] Entity {runtimeData.uniqueId} assigned to zone {zoneId}");

        MarkDirty();

        // Trigger stat recalculation - zones will query us for their assigned entities
        Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
    }

    [Topic(PickupEventIds.ON_ENTITY_REMOVED_FROM_ZONE)]
    public void OnEntityRemovedFromZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        DebugManager.Log($"[IncrementalGame] [EntityManager] Entity {runtimeData.uniqueId} removed from zone {zoneId}");

        MarkDirty();

        // Trigger stat recalculation - zones will query us for their assigned entities
        Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
    }

    #endregion

    #region Position Tracking

    /// <summary>
    /// Update an entity's saved position. Call this when entity moves or is assigned to zone.
    /// </summary>
    public void UpdateEntityPosition(EntityRuntimeData runtimeData, Transform transform)
    {
        runtimeData.UpdatePositionFromTransform(transform);
        MarkDirty();
    }

    #endregion

    #region Save Methods

    private void MarkDirty()
    {
        if (_saveProvider != null)
        {
            _saveProvider.MarkDirty();
        }
    }

    public IEnumerator ManualSave()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SetData(_allEntityRuntimeData);
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

    #region Debug Helpers

    [Button("Log All Entities"), FoldoutGroup("Debug")]
    private void DebugLogAllEntities()
    {
        DebugManager.Log($"[IncrementalGame] === ALL ENTITIES ({_totalEntityCount}) ===");
        foreach (var entity in _allEntityRuntimeData)
        {
            DebugManager.Log($"[IncrementalGame] ID: {entity.uniqueId} | Name: {entity.entityName} | State: {entity.currentState} | Zone: {entity.currentZoneId ?? "None"} | Pos: {entity.worldPosition}");
        }
    }

    [Button("Log Entities By Zone"), FoldoutGroup("Debug")]
    private void DebugLogEntitiesByZone(string zoneId)
    {
        var entities = GetEntitiesInZone(zoneId);
        DebugManager.Log($"[IncrementalGame] === ENTITIES IN ZONE: {zoneId} ({entities.Count}) ===");
        foreach (var entity in entities)
        {
            DebugManager.Log($"[IncrementalGame] ID: {entity.uniqueId} | Name: {entity.entityName}");
        }
    }

    [Button("Manual Save"), FoldoutGroup("Debug")]
    private void DebugManualSave()
    {
        StartCoroutine(ManualSave());
    }

    [Button("Force Restore Entities"), FoldoutGroup("Debug")]
    private void DebugForceRestore()
    {
        StartCoroutine(RestoreEntities());
    }

    #endregion
}