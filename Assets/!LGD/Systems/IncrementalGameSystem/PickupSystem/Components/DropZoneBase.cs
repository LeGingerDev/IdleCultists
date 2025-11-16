using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LGD.PickupSystem
{
    /// <summary>
    /// Base implementation for all drop zones.
    /// Handles capacity tracking, visual feedback, and entity registration.
    /// </summary>
    public abstract class DropZoneBase : BaseBehaviour, IDropZone
    {
        [SerializeField, FoldoutGroup("Identity")]
        private string _zoneId;

        [SerializeField, FoldoutGroup("Identity")]
        private DropZoneType _zoneType;

        [SerializeField, FoldoutGroup("Capacity")]
        private int _maxCapacity = 5;

        [SerializeField, FoldoutGroup("Accepted Types")]
        private DropZoneType _acceptedEntityTypes = DropZoneType.Any;

        [SerializeField, FoldoutGroup("Visual Feedback")]
        private GameObject _visualFeedbackObject;

        [SerializeField, FoldoutGroup("Debug")]
        private bool _enableDebugLogs = true;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private List<string> _assignedEntityIds = new List<string>();
        private List<IDropZoneCapacityDisplayer> _dropZoneCapacityDisplayers = new List<IDropZoneCapacityDisplayer>();
        private Collider2D _col;

        protected virtual void Awake()
        {
            // Find capacity displayers in children
            _dropZoneCapacityDisplayers.AddRange(GetComponentsInChildren<IDropZoneCapacityDisplayer>(true));
            // Initial capacity update
            foreach (var displayer in _dropZoneCapacityDisplayers)
            {
                displayer.UpdateCapacityDisplay(GetCurrentCapacity(), GetMaxCapacity());
            }
        }

        public void Start()
        {
            GenerateZoneIdIfNeeded();
            HideVisualFeedback();
            _col = GetComponent<Collider2D>();
            _col.enabled = false;
        }

        #region IDropZone Implementation

        public string GetZoneId() => _zoneId;
        public DropZoneType GetZoneType() => _zoneType;
        public int GetCurrentCapacity() => _assignedEntityIds.Count;
        public int GetMaxCapacity() => _maxCapacity;
        public List<string> GetAssignedEntityIds() => new List<string>(_assignedEntityIds);

        public virtual bool CanAcceptEntity(EntityPickup entity)
        {
            if (entity == null)
            {
                DebugLog("CanAcceptEntity: FAIL - entity is null");
                return false;
            }

            DebugLog($"=== ZONE VALIDATION: {_zoneId} ===");
            DebugLog($"Zone Type: {_zoneType}");
            DebugLog($"Zone Accepts: {_acceptedEntityTypes}");
            DebugLog($"Current Capacity: {_assignedEntityIds.Count}/{_maxCapacity}");
            DebugLog($"Entity Accepts: {entity.GetAcceptedZoneTypes()}");

            // Check capacity
            if (_assignedEntityIds.Count >= _maxCapacity)
            {
                DebugLog($"CanAcceptEntity: FAIL - Zone at capacity ({_assignedEntityIds.Count}/{_maxCapacity})");
                return false;
            }

            // Check type compatibility
            DropZoneType entityAcceptedTypes = entity.GetAcceptedZoneTypes();

            // If entity accepts ANY zone type, it can go anywhere
            if (entityAcceptedTypes == DropZoneType.Any)
            {
                DebugLog("CanAcceptEntity: SUCCESS - Entity accepts ANY zones");
                return true;
            }

            // If zone accepts ANY entity type, accept all entities
            if (_acceptedEntityTypes == DropZoneType.Any)
            {
                DebugLog("CanAcceptEntity: SUCCESS - Zone accepts ANY entities");
                return true;
            }

            // Check if entity's accepted types include this zone's type
            bool entityAcceptsThisZone = entityAcceptedTypes.HasFlag(_zoneType);
            DebugLog($"Entity accepts this zone type ({_zoneType}): {entityAcceptsThisZone}");

            // Check if zone's accepted types include entity's types
            bool zoneAcceptsThisEntity = _acceptedEntityTypes.HasFlag(entityAcceptedTypes);
            DebugLog($"Zone accepts entity types ({entityAcceptedTypes}): {zoneAcceptsThisEntity}");

            bool isCompatible = entityAcceptsThisZone && zoneAcceptsThisEntity;

            if (isCompatible)
            {
                DebugLog("CanAcceptEntity: SUCCESS - Types compatible");
            }
            else
            {
                DebugLog("CanAcceptEntity: FAIL - Type mismatch");
            }

            return isCompatible;
        }

        public abstract Vector3 GetDropPosition(Vector3 worldPos);

        public virtual void OnEntityAssigned(EntityPickup entity)
        {
            string entityId = entity.GetIdOfEntity();

            if (_assignedEntityIds.Contains(entityId))
            {
                DebugManager.Warning($"[IncrementalGame] Entity {entityId} already assigned to zone {_zoneId}");
                return;
            }

            _assignedEntityIds.Add(entityId);
            Publish(PickupEventIds.ON_ENTITY_ASSIGNED_TO_ZONE, entity.GetRuntimeData(), _zoneId);

            DebugLog($"Entity {entityId} assigned to zone {_zoneId}. Capacity: {_assignedEntityIds.Count}/{_maxCapacity}");
        }

        public virtual void OnEntityRemoved(EntityPickup entity)
        {
            string entityId = entity.GetIdOfEntity();

            if (!_assignedEntityIds.Contains(entityId))
            {
                DebugManager.Warning($"[IncrementalGame] Entity {entityId} not found in zone {_zoneId}");
                return;
            }

            _assignedEntityIds.Remove(entityId);
            Publish(PickupEventIds.ON_ENTITY_REMOVED_FROM_ZONE, entity.GetRuntimeData(), _zoneId);

            DebugLog($"Entity {entityId} removed from zone {_zoneId}. Capacity: {_assignedEntityIds.Count}/{_maxCapacity}");
        }

        /// <summary>
        /// Reconnect an entity to this zone during save/load restoration.
        /// Unlike OnEntityAssigned, this does NOT publish events (entity is already assigned).
        /// Use this during game load to rebuild zone tracking without triggering gameplay events.
        /// </summary>
        public virtual void OnEntityReconnected(EntityPickup entity)
        {
            string entityId = entity.GetIdOfEntity();

            if (_assignedEntityIds.Contains(entityId))
            {
                DebugLog($"Entity {entityId} already tracked during reconnection (skipping duplicate)");
                return;
            }

            _assignedEntityIds.Add(entityId);

            // Publish reconnection event for visual/audio systems (animations, barks)
            // This does NOT trigger stat recalculation or gameplay logic
            Publish(PickupEventIds.ON_ENTITY_RECONNECTED_TO_ZONE, entity.GetRuntimeData(), _zoneId);

            DebugLog($"Entity {entityId} reconnected to zone {_zoneId}. Capacity: {_assignedEntityIds.Count}/{_maxCapacity}");
        }

        /// <summary>
        /// Return an entity to this zone after invalid drop.
        /// Similar to OnEntityAssigned but publishes a different event for behaviors that don't want animations.
        /// </summary>
        public virtual void OnEntityReturned(EntityPickup entity)
        {
            string entityId = entity.GetIdOfEntity();

            if (_assignedEntityIds.Contains(entityId))
            {
                DebugLog($"Entity {entityId} already tracked during return (skipping duplicate)");
                return;
            }

            _assignedEntityIds.Add(entityId);
            Publish(PickupEventIds.ON_ENTITY_RETURNED_TO_ZONE, entity.GetRuntimeData(), _zoneId);

            DebugLog($"Entity {entityId} returned to zone {_zoneId}. Capacity: {_assignedEntityIds.Count}/{_maxCapacity}");
        }

        public int GetAvailableCapacity()
        {
            return _maxCapacity - _assignedEntityIds.Count;
        }

        public virtual void ShowVisualFeedback()
        {
            if (_visualFeedbackObject != null)
            {
                _visualFeedbackObject.SetActive(true);
                if (!_col)
                    _col = GetComponent<Collider2D>();
                _col.enabled = true;
                _dropZoneCapacityDisplayers.ForEach(d => d.UpdateCapacityDisplay(GetCurrentCapacity(), GetMaxCapacity()));
            }
        }

        public virtual void HideVisualFeedback()
        {
            if (_visualFeedbackObject != null)
            {
                _visualFeedbackObject.SetActive(false);
                if (!_col)
                    _col = GetComponent<Collider2D>();
                _col.enabled = false;
            }
        }

        #endregion

        #region Topic Listeners

        [Topic(PickupEventIds.ON_ENTITY_PICKED_UP)]
        public void OnEntityPickedUpEvent(object sender, EntityPickup entity)
        {
            if (entity == null)
                return;

            // Check if this zone can accept this entity type
            DropZoneType entityTypes = entity.GetAcceptedZoneTypes();

            bool isCompatible = _acceptedEntityTypes == DropZoneType.Any ||
                                entityTypes == DropZoneType.Any ||
                                (entityTypes.HasFlag(_zoneType) && _acceptedEntityTypes.HasFlag(entityTypes));

            if (isCompatible && CanAcceptEntity(entity))
            {
                ShowVisualFeedback();
            }
        }

        [Topic(PickupEventIds.ON_ENTITY_DROPPED)]
        public void OnEntityDroppedEvent(object sender, EntityPickup entity)
        {
            HideVisualFeedback();
        }

        #endregion

        #region Helpers

        private void GenerateZoneIdIfNeeded()
        {
            if (string.IsNullOrEmpty(_zoneId))
            {
                _zoneId = $"{_zoneType}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[DropZone:{_zoneId}] {message}");
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [Button("Generate New Zone ID"), FoldoutGroup("Identity")]
        private void EditorGenerateNewId()
        {
            _zoneId = $"{_zoneType}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        #endregion
    }
}

public interface IDropZoneCapacityDisplayer
{
    void UpdateCapacityDisplay(int currentCapacity, int maxCapacity);
}