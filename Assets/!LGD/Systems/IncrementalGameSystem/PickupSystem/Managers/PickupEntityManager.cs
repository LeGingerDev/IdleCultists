using DG.Tweening;
using LGD.Core.Events;
using LGD.Core.Singleton;
using LGD.InteractionSystem;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace LGD.PickupSystem
{
    /// <summary>
    /// Central manager for entity pickup and drop operations.
    /// Listens to WorldInteractionManager events and handles drag/drop flow.
    /// Uses RequestBus to prevent click-through to other systems while dragging.
    /// </summary>
    public class PickupEntityManager : MonoSingleton<PickupEntityManager>
    {
        [SerializeField, FoldoutGroup("Layers")]
        private LayerMask _dropZoneLayer;

        [SerializeField, FoldoutGroup("Drag Settings")]
        private float _dragFollowSpeed = 15f;

        [SerializeField, FoldoutGroup("Drag Settings")]
        private Vector3 _dragOffset = new Vector3(0, 0.2f, 0);

        [SerializeField, FoldoutGroup("Animation")]
        private float _dropAnimationDuration = 0.3f;

        [SerializeField, FoldoutGroup("Animation")]
        private Ease _dropEaseType = Ease.OutQuad;

        [SerializeField, FoldoutGroup("Debug")]
        private bool _enableDebugLogs = true;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private bool _isDragging;

        private EntityPickup _currentPickedEntity;
        private Vector3 _originalPosition;
        private Camera _mainCamera;
        private Coroutine _dragCoroutine;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        #region RequestBus Providers

        // TODO: Replace with Topic System
        [Provider(InputRequestIds.CAN_PROCESS_WORLD_CLICK, priority: 100)]
        public bool CanProcessWorldClick(object requester)
        {
            // Block world clicks while dragging an entity
            return _currentPickedEntity == null;
        }

        // TODO: Replace with Topic System
        [Provider(InputRequestIds.CAN_PROCESS_PICKUP, priority: 100)]
        public bool CanProcessPickup(object requester)
        {
            // Can start pickup if not already dragging
            return _currentPickedEntity == null;
        }

        #endregion

        #region Topic Listeners

        [Topic(InteractionEventIds.ON_MOUSE_DOWN)]
        public void OnMouseDownEvent(object sender, InteractionData data)
        {
            // Check if pickup is allowed by ALL systems
            if (!this.CanProcessPickup())
                return;

            if (_currentPickedEntity != null)
                return;

            if (!data.HasTarget)
                return;

            EntityPickup pickup = data.Target.GetComponent<EntityPickup>();
            if (pickup == null)
                return;

            if (!pickup.CanPickup())
            {
                DebugLog($"Cannot pickup entity {pickup.GetIdOfEntity()} - CanPickup() returned false");
                return;
            }

            StartPickup(pickup);
        }

        [Topic(InteractionEventIds.ON_MOUSE_UP)]
        public void OnMouseUpEvent(object sender, InteractionData data)
        {
            if (_currentPickedEntity == null)
                return;

            // Do our OWN raycast for drop zones (ignore WorldInteractionManager's hit data)
            IDropZone dropZone = RaycastForDropZone();
            Vector3 dropPosition = GetMouseWorldPosition();

            DebugLog($"=== DROP VALIDATION START ===");
            DebugLog($"Entity: {_currentPickedEntity.GetIdOfEntity()}");
            DebugLog($"Entity AcceptedZoneTypes: {_currentPickedEntity.GetAcceptedZoneTypes()}");

            if (dropZone == null)
            {
                DebugLog("FAIL: No drop zone found at mouse position");
                HandleInvalidDrop();
                return;
            }

            DebugLog($"Drop Zone Found: {dropZone.GetZoneId()} (Type: {dropZone.GetZoneType()})");
            DebugLog($"Zone Current Capacity: {dropZone.GetCurrentCapacity()}/{dropZone.GetMaxCapacity()}");

            bool canEntityDrop = _currentPickedEntity.CanDrop(dropZone);
            DebugLog($"Entity.CanDrop(zone): {canEntityDrop}");

            if (!canEntityDrop)
            {
                DebugLog("FAIL: Entity.CanDrop() returned false");
                HandleInvalidDrop();
                return;
            }

            bool canZoneAccept = dropZone.CanAcceptEntity(_currentPickedEntity);
            DebugLog($"Zone.CanAcceptEntity(entity): {canZoneAccept}");

            if (!canZoneAccept)
            {
                DebugLog("FAIL: Zone.CanAcceptEntity() returned false");
                HandleInvalidDrop();
                return;
            }

            DebugLog("SUCCESS: All validations passed!");
            DebugLog($"=== DROP VALIDATION END ===");

            PerformDrop(dropZone, dropPosition);
        }

        [Topic(InteractionEventIds.ON_MOUSE_UP_EMPTY)]
        public void OnMouseUpEmptyEvent(object sender, InteractionData data)
        {
            if (_currentPickedEntity == null)
                return;

            DebugLog("Mouse released on empty space - invalid drop");
            HandleInvalidDrop();
        }

        // TODO: Replace with Topic System
        //[Topic(GameplayEventIds.ON_UI_OPENED)]
        public void OnUIOpened(object sender)
        {
            // If dragging when UI opens, cancel the drag
            if (_isDragging)
            {
                HandleInvalidDrop();
            }
        }

        #endregion

        #region Pickup Flow

        private void StartPickup(EntityPickup entity)
        {
            _currentPickedEntity = entity;
            _originalPosition = entity.transform.position;

            entity.OnPickedUp();
            StartDragging();

            Publish(PickupEventIds.ON_ENTITY_PICKED_UP, entity);

            DebugLog($"Started pickup of entity {entity.GetIdOfEntity()}");
        }

        private void StartDragging()
        {
            _isDragging = true;

            if (_dragCoroutine != null)
            {
                StopCoroutine(_dragCoroutine);
            }

            _dragCoroutine = StartCoroutine(DragCoroutine());
        }

        private void StopDragging()
        {
            _isDragging = false;

            if (_dragCoroutine != null)
            {
                StopCoroutine(_dragCoroutine);
                _dragCoroutine = null;
            }
        }

        private IEnumerator DragCoroutine()
        {
            while (_isDragging && _currentPickedEntity != null)
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                Vector3 targetPosition = mouseWorldPos + _dragOffset;

                Transform entityTransform = _currentPickedEntity.transform;
                entityTransform.position = Vector3.Lerp(
                    entityTransform.position,
                    targetPosition,
                    Time.deltaTime * _dragFollowSpeed
                );

                yield return null;
            }
        }

        #endregion

        #region Drop Flow

        private void PerformDrop(IDropZone dropZone, Vector3 worldPosition)
        {
            StopDragging();

            Vector3 finalDropPosition = dropZone.GetDropPosition(worldPosition);
            AnimateToDropPosition(finalDropPosition, dropZone);
        }

        private void AnimateToDropPosition(Vector3 dropPosition, IDropZone dropZone)
        {
            Transform entityTransform = _currentPickedEntity.transform;

            entityTransform.DOMove(dropPosition, _dropAnimationDuration)
                .SetEase(_dropEaseType)
                .OnComplete(() =>
                {
                    CompleteDrop(dropZone);
                });
        }

        private void CompleteDrop(IDropZone dropZone)
        {
            _currentPickedEntity.OnDropped(dropZone);

            dropZone.OnEntityAssigned(_currentPickedEntity);

            Publish(PickupEventIds.ON_ENTITY_DROPPED, _currentPickedEntity);

            DebugLog($"Completed drop of entity {_currentPickedEntity.GetIdOfEntity()} in zone {dropZone.GetZoneId()}");

            _currentPickedEntity = null;
        }

        private void HandleInvalidDrop()
        {
            StopDragging();

            // Publish drop event IMMEDIATELY so zones hide visual feedback right away
            Publish(PickupEventIds.ON_ENTITY_DROPPED, _currentPickedEntity);
            Publish(PickupEventIds.ON_ENTITY_INVALID_DROP, _currentPickedEntity);
            Transform entityTransform = _currentPickedEntity.transform;

            entityTransform.DOMove(_originalPosition, _dropAnimationDuration)
                .SetEase(_dropEaseType)
                .OnComplete(() =>
                {
                    _currentPickedEntity.OnInvalidDrop();
                    _currentPickedEntity = null;
                });

            DebugLog("Invalid drop - returning entity to original position");
        }

        #endregion

        #region Raycast Helpers

        private IDropZone RaycastForDropZone()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();

            // Raycast ONLY on drop zone layer - ignores all other clickable objects
            RaycastHit2D hit = Physics2D.Raycast(
                mouseWorldPos,
                Vector2.zero,
                Mathf.Infinity,
                _dropZoneLayer
            );

            if (hit.collider != null)
            {
                return hit.transform.GetComponent<IDropZone>();
            }

            return null;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(_mainCamera.transform.position.z);
            return _mainCamera.ScreenToWorldPoint(mousePos);
        }

        #endregion

        #region Debug Helpers

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
            {
                    DebugManager.Log($"[IncrementalGame] [PickupManager] {message}");
            }
        }

        #endregion
    }
}