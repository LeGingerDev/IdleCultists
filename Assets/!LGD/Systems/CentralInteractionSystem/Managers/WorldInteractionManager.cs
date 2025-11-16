using UnityEngine;
using Sirenix.OdinInspector;
using LGD.Core.Events;
using UnityEngine.EventSystems;

namespace LGD.InteractionSystem
{
    public enum RaycastMode
    {
        Physics3D,
        Physics2D,
        Both
    }

    public class WorldInteractionManager : MonoBehaviour
    {
        [FoldoutGroup("Raycast Settings"), SerializeField]
        private RaycastMode _raycastMode = RaycastMode.Physics3D;

        [FoldoutGroup("Raycast Settings"), SerializeField]
        private LayerMask _interactableLayers;

        [FoldoutGroup("Raycast Settings"), SerializeField]
        private float _maxRayDistance = 100f;

        [FoldoutGroup("References"), SerializeField]
        private Camera _mainCamera;

        private Transform _currentHovered;
        private Transform _lastHovered;
        private bool _hasHoverChanged;

        private bool _isMouseHeld;
        private Vector3 _dragStartPos;

        private RaycastHit _hitInfo3D;
        private RaycastHit2D _hitInfo2D;
        private bool _interactionBlocked;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_interactionBlocked)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            HoverCheck();
            ClickDownCheck();
            ClickUpCheck();

            if (_isMouseHeld)
                PublishDrag();
        }

        // ------------------------------
        // Hover Detection
        // ------------------------------
        private void HoverCheck()
        {
            bool hitSomething = PerformRaycast(Input.mousePosition, out Transform hitTransform);

            _currentHovered = hitSomething ? hitTransform : null;
            _hasHoverChanged = _currentHovered != _lastHovered;

            if (_hasHoverChanged)
            {
                if (_lastHovered != null)
                    PublishHoverExit(_lastHovered);

                if (_currentHovered != null)
                    PublishHoverEnter();
                else
                    PublishHoverCleared();

                _lastHovered = _currentHovered;
            }
        }

        // ------------------------------
        // Click Down / Up / Drag
        // ------------------------------
        private void ClickDownCheck()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            // Check if pickup system allows processing clicks
            if (!CanProcessWorldClick())
                return;

            _isMouseHeld = true;
            _dragStartPos = Input.mousePosition;

            bool hit = PerformRaycast(Input.mousePosition, out Transform hitTransform);

            if (hit)
                PublishMouseDown();
            else
                PublishMouseDownEmpty();
        }

        private void ClickUpCheck()
        {
            if (!Input.GetMouseButtonUp(0))
                return;

            _isMouseHeld = false;

            bool hit = PerformRaycast(Input.mousePosition, out Transform hitTransform);

            if (hit)
                PublishMouseUp();
            else
                PublishMouseUpEmpty();
        }

        private void PublishDrag()
        {
            bool hit = PerformRaycast(Input.mousePosition, out Transform hitTransform);
            InteractionHitData? hitData = hit ? GetHitData(true) : null;

            var dragData = new DragData(_dragStartPos, Input.mousePosition, hitData);
            ServiceBus.Publish(InteractionEventIds.ON_INTERACTION_DRAGGING, this, dragData);
        }

        // ------------------------------
        // RequestBus Check
        // ------------------------------
        private bool CanProcessWorldClick()
        {
            // Query all systems to see if world clicks are allowed
            // PickupEntityManager will respond with false if dragging
            return RequestBus.Request<bool>(InputRequestIds.CAN_PROCESS_WORLD_CLICK, this);
        }

        // ------------------------------
        // Raycast Logic
        // ------------------------------
        private bool PerformRaycast(Vector3 screenPosition, out Transform hitTransform)
        {
            hitTransform = null;

            if (_raycastMode == RaycastMode.Physics3D || _raycastMode == RaycastMode.Both)
            {
                if (TryRaycast3D(screenPosition, out hitTransform))
                    return true;
            }

            if (_raycastMode == RaycastMode.Physics2D || _raycastMode == RaycastMode.Both)
            {
                if (TryRaycast2D(screenPosition, out hitTransform))
                    return true;
            }

            return false;
        }

        private bool TryRaycast3D(Vector3 screenPosition, out Transform hitTransform)
        {
            hitTransform = null;
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out _hitInfo3D, _maxRayDistance, _interactableLayers))
            {
                hitTransform = _hitInfo3D.transform;
                return true;
            }

            return false;
        }

        private bool TryRaycast2D(Vector3 screenPosition, out Transform hitTransform)
        {
            hitTransform = null;
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(screenPosition);
            _hitInfo2D = Physics2D.Raycast(worldPos, Vector2.zero, _maxRayDistance, _interactableLayers);

            if (_hitInfo2D.collider != null)
            {
                hitTransform = _hitInfo2D.transform;
                return true;
            }

            return false;
        }

        private InteractionHitData? GetHitData(bool didHit)
        {
            if (!didHit)
                return null;

            if (_hitInfo3D.transform != null)
                return new InteractionHitData(_hitInfo3D);

            if (_hitInfo2D.collider != null)
                return new InteractionHitData(_hitInfo2D);

            return null;
        }

        // ------------------------------
        // Publish Helpers
        // ------------------------------
        private void PublishHoverEnter()
        {
            var data = new InteractionData(InteractionType.HoverEnter, GetHitData(true), Input.mousePosition);
            ServiceBus.Publish(InteractionEventIds.ON_INTERACTION_HOVER_ENTERED, this, data);
        }

        private void PublishHoverExit(Transform lastHovered)
        {
            var data = InteractionData.Empty(InteractionType.HoverExit, Input.mousePosition);
            ServiceBus.Publish(InteractionEventIds.ON_INTERACTION_HOVER_EXITED, this, data);
        }

        private void PublishHoverCleared()
        {
            var data = InteractionData.Empty(InteractionType.HoverCleared, Input.mousePosition);
            ServiceBus.Publish(InteractionEventIds.ON_INTERACTION_HOVER_CLEARED, this, data);
        }

        private void PublishMouseDown()
        {
            var data = new InteractionData(InteractionType.MouseDown, GetHitData(true), Input.mousePosition);
            ServiceBus.Publish(InteractionEventIds.ON_MOUSE_DOWN, this, data);
            ServiceBus.Publish(InteractionEventIds.ON_INTERACTABLE_CLICKED, this, data);
        }

        private void PublishMouseDownEmpty()
        {
            var data = InteractionData.Empty(InteractionType.MouseDown, Input.mousePosition);
            ServiceBus.Publish(InteractionEventIds.ON_MOUSE_DOWN_EMPTY, this, data);
            ServiceBus.Publish(InteractionEventIds.ON_WORLD_CLICKED_EMPTY, this, data);
        }

        private void PublishMouseUp()
        {
            var data = new InteractionData(InteractionType.MouseUp, GetHitData(true), Input.mousePosition);
            ServiceBus.Publish(InteractionEventIds.ON_MOUSE_UP, this, data);
        }

        private void PublishMouseUpEmpty()
        {
            var data = InteractionData.Empty(InteractionType.MouseUp, Input.mousePosition);
            ServiceBus.Publish(InteractionEventIds.ON_MOUSE_UP_EMPTY, this, data);
        }

        // ------------------------------
        // UI Blocking
        // ------------------------------
        // TODO: Replace with Topic System
        //[Topic(GameplayEventIds.ON_UI_OPENED)]
        public void OnUIOpened(object sender)
        {
            _interactionBlocked = true;
        }

        // TODO: Replace with Topic System
        //[Topic(GameplayEventIds.ON_UI_CLOSED)]
        public void OnUIClosed(object sender)
        {
            _interactionBlocked = false;
        }
    }
}