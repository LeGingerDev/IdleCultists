using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Makes an EntityController pickupable and droppable.
/// Handles validation and state tracking for pickup/drop operations.
/// </summary>
[RequireComponent(typeof(EntityController))]
public class EntityPickup : MonoBehaviour
{
    public UnityEvent OnPickup;
    public UnityEvent OnDrop;

    [SerializeField, FoldoutGroup("Accepted Zones")]
    private DropZoneType _acceptedZoneTypes = DropZoneType.Any;

    [SerializeField, FoldoutGroup("Debug")]
    private bool _enableDebugLogs = true;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private bool _isCurrentlyPickedUp;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private string _currentZoneId;

    private EntityController _entityController;
    private IDropZone _currentZone;
    private IDropZone _previousZoneBeforePickup;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _entityController = GetComponent<EntityController>();
    }

    #region Public Getters

    public EntityController GetEntityController() => _entityController;

    public EntityRuntimeData GetRuntimeData() => _entityController?.RuntimeData;

    public string GetIdOfEntity() => _entityController?.RuntimeData?.uniqueId;

    public DropZoneType GetAcceptedZoneTypes() => _acceptedZoneTypes;

    public bool IsPickedUp() => _isCurrentlyPickedUp;

    public IDropZone GetCurrentZone() => _currentZone;

    public Vector3 GetOriginalPosition() => _originalPosition;

    #endregion

    #region Validation

    public bool CanPickup()
    {
        if (_entityController == null || _entityController.RuntimeData == null)
        {
            DebugLog("EntityPickup:CanPickup: FAIL - EntityController or RuntimeData is null");
            return false;
        }

        EntityRuntimeData runtimeData = _entityController.RuntimeData;

        // Can't pick up if sacrificing or in other locked states
        if (runtimeData.currentState == EntityState.Sacrificing)
        {
            DebugLog($"EntityPickup:CanPickup: FAIL - Entity in Sacrificing state");
            return false;
        }

        DebugLog($"EntityPickup:CanPickup: SUCCESS");
        return true;
    }

    public bool CanDrop(IDropZone dropZone)
    {
        if (dropZone == null)
        {
            DebugLog("EntityPickup:CanDrop: FAIL - dropZone is null");
            return false;
        }

        DebugLog($"EntityPickup:=== ENTITY VALIDATION ===");
        DebugLog($"EntityPickup:Entity ID: {GetIdOfEntity()}");
        DebugLog($"EntityPickup:Entity Accepts: {_acceptedZoneTypes}");
        DebugLog($"EntityPickup:Zone Type: {dropZone.GetZoneType()}");

        DropZoneType zoneType = dropZone.GetZoneType();

        // If entity accepts ANY zone, it can drop anywhere
        if (_acceptedZoneTypes == DropZoneType.Any)
        {
            DebugLog("EntityPickup:CanDrop: SUCCESS - Entity accepts ANY zones");
            return true;
        }

        // Check if entity's accepted types include this zone type
        bool accepts = _acceptedZoneTypes.HasFlag(zoneType);

        if (accepts)
        {
            DebugLog($"EntityPickup:CanDrop: SUCCESS - Entity accepts zone type {zoneType}");
        }
        else
        {
            DebugLog($"EntityPickup:CanDrop: FAIL - Entity does NOT accept zone type {zoneType}");
        }

        return accepts;
    }

    #endregion

    #region Pickup/Drop Callbacks

    public void OnPickedUp()
    {
        _isCurrentlyPickedUp = true;
        _originalPosition = transform.position;

        EntityRuntimeData runtimeData = _entityController.RuntimeData;

        // If entity was in a zone, notify zone of removal and remember it
        if (_currentZone != null)
        {
            _currentZone.OnEntityRemoved(this);
            _previousZoneBeforePickup = _currentZone;  // Remember for invalid drops
            _currentZone = null;  // Clear current zone (truly in transit)
        }

        // Update state
        runtimeData.currentState = EntityState.InTransit;
        runtimeData.currentZoneId = null;
        OnPickup?.Invoke();
        _currentZoneId = null;

        // Update position for save
        _entityController.ForcePositionUpdate();

        DebugLog($"EntityPickup:{GetIdOfEntity()} picked up");
    }

    public void OnDropped(IDropZone dropZone)
    {
        _isCurrentlyPickedUp = false;

        EntityRuntimeData runtimeData = _entityController.RuntimeData;

        // Update state
        runtimeData.currentState = EntityState.Assigned;
        runtimeData.currentZoneId = dropZone.GetZoneId();
        _currentZoneId = dropZone.GetZoneId();
        _currentZone = dropZone;
        _previousZoneBeforePickup = null;  // Clear previous zone
        transform.position = new Vector3(transform.position.x, transform.position.y, -0.05f);
        OnDrop?.Invoke();

        // Update position for save
        _entityController.ForcePositionUpdate();

        DebugLog($"EntityPickup:{GetIdOfEntity()} dropped in zone {dropZone.GetZoneId()}");
    }

    public void OnInvalidDrop()
    {
        _isCurrentlyPickedUp = false;

        EntityRuntimeData runtimeData = _entityController.RuntimeData;

        // Return to idle state if no previous zone
        if (_previousZoneBeforePickup == null)
        {
            runtimeData.currentState = EntityState.Idle;
            runtimeData.currentZoneId = null;
            _currentZoneId = null;
        }
        else
        {
            // Re-assign to previous zone
            runtimeData.currentState = EntityState.Assigned;
            runtimeData.currentZoneId = _previousZoneBeforePickup.GetZoneId();
            _currentZoneId = _previousZoneBeforePickup.GetZoneId();
            _currentZone = _previousZoneBeforePickup;  // Restore current zone

            // FIX: Use OnEntityReturned instead of OnEntityAssigned to avoid retriggering animations
            _currentZone.OnEntityReturned(this);

            _previousZoneBeforePickup = null;  // Clear previous zone
        }
        OnDrop?.Invoke();

        // Update position for save
        _entityController.ForcePositionUpdate();

        DebugLog($"EntityPickup:{GetIdOfEntity()} invalid drop - returned to origin");
    }

    public void ClearCurrentZone()
    {
        _currentZone = null;
        _previousZoneBeforePickup = null;
    }

    /// <summary>
    /// Reconnect this entity to a zone during save/load restoration.
    /// Sets internal zone reference and state WITHOUT publishing events or invoking UnityEvents.
    /// Use this during game load to restore entity-zone relationships.
    /// </summary>
    public void ReconnectToZone(IDropZone zone)
    {
        if (zone == null)
        {
            Debug.LogWarning($"[EntityPickup] Cannot reconnect {GetIdOfEntity()} - zone is null");
            return;
        }

        // Set internal state silently
        _currentZone = zone;
        _currentZoneId = zone.GetZoneId();
        _previousZoneBeforePickup = null;

        DebugLog($"Reconnected to zone {zone.GetZoneId()} (restoration mode - no events fired)");
    }

    #endregion

    #region Debug Helpers

    private void DebugLog(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[EntityPickup:{GetIdOfEntity()}] {message}");
        }
    }

    #endregion
}