using LGD.Core;
using LGD.PickupSystem;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all zone behaviors that contribute stats.
/// Auto-registers with StatManager on Start and unregisters on destroy.
/// Subclasses implement GetModifiersForStat() to provide their specific contributions.
/// </summary>
[RequireComponent(typeof(IDropZone))]
public abstract class ZoneBehaviorBase : BaseBehaviour, IStatProvider
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    protected string _zoneId;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    protected DropZoneType _zoneType;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    protected bool _isRegisteredWithStatManager;

    protected IDropZone _dropZone;

    protected virtual void Awake()
    {
        _dropZone = GetComponent<IDropZone>();

        if (_dropZone == null)
        {
            DebugManager.Error($"[IncrementalGame] [{GetType().Name}] No IDropZone component found on {gameObject.name}!");
            return;
        }

        _zoneId = _dropZone.GetZoneId();
        _zoneType = _dropZone.GetZoneType();
    }

    protected virtual void Start()
    {
        RegisterWithStatManager();
    }

    protected virtual void OnDestroy()
    {
        UnregisterFromStatManager();
    }

    #region StatManager Registration

    private void RegisterWithStatManager()
    {
        if (StatManager.Instance == null)
        {
            DebugManager.Warning($"[IncrementalGame] [{GetType().Name}] StatManager not available for registration");
            return;
        }

        StatManager.Instance.RegisterStatProvider(this);
        _isRegisteredWithStatManager = true;

        DebugManager.Log($"[IncrementalGame] [{GetType().Name}] Registered with StatManager - Zone: {_zoneId}");
    }

    private void UnregisterFromStatManager()
    {
        if (StatManager.Instance != null && _isRegisteredWithStatManager)
        {
            StatManager.Instance.UnregisterStatProvider(this);
            _isRegisteredWithStatManager = false;

            DebugManager.Log($"[IncrementalGame] [{GetType().Name}] Unregistered from StatManager - Zone: {_zoneId}");
        }
    }

    #endregion

    #region Helper Methods for Subclasses

    /// <summary>
    /// Get all entities assigned to this zone.
    /// Subclasses use this to calculate their stat contributions.
    /// </summary>
    protected List<EntityRuntimeData> GetAssignedEntities()
    {
        if (EntityManager.Instance == null)
        {
            DebugManager.Warning($"[IncrementalGame] [{GetType().Name}] EntityManager not available");
            return new List<EntityRuntimeData>();
        }

        return EntityManager.Instance.GetEntitiesInZone(_zoneId);
    }

    /// <summary>
    /// Get the current number of entities in this zone.
    /// </summary>
    protected int GetAssignedEntityCount()
    {
        return _dropZone?.GetCurrentCapacity() ?? 0;
    }

    #endregion

    #region IStatProvider Implementation (Abstract)

    /// <summary>
    /// Subclasses implement this to provide their specific stat contributions.
    /// Return a list of StatModifiers based on assigned entities.
    /// </summary>
    public abstract List<StatModifier> GetModifiersForStat(StatType statType);

    #endregion

    #region Debug Helpers

    [Button("Log Assigned Entities"), FoldoutGroup("Debug")]
    protected void DebugLogAssignedEntities()
    {
        var entities = GetAssignedEntities();
        DebugManager.Log($"[IncrementalGame] === {GetType().Name} - ASSIGNED ENTITIES ({entities.Count}) ===");

        foreach (var entity in entities)
        {
            DebugManager.Log($"[IncrementalGame]   - {entity.entityName} (ID: {entity.uniqueId})");
        }
    }

    [Button("Test GetModifiersForStat"), FoldoutGroup("Debug")]
    protected void DebugTestModifiers(StatType statType)
    {
        var modifiers = GetModifiersForStat(statType);
        DebugManager.Log($"[IncrementalGame] === {GetType().Name} - MODIFIERS FOR {statType} ({modifiers.Count}) ===");

        foreach (var modifier in modifiers)
        {
            string value = modifier.modifierType == ModifierType.Additive
                ? modifier.additiveValue.ToString()
                : $"{modifier.multiplicativeValue * 100}%";

            DebugManager.Log($"[IncrementalGame]   - {modifier.statType}: {value} ({modifier.modifierType})");
        }
    }

    #endregion
}