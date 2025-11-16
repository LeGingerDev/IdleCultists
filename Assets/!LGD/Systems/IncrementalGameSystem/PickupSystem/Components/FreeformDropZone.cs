using Sirenix.OdinInspector;
using UnityEngine;

namespace LGD.PickupSystem
{
    /// <summary>
    /// Drop zone that allows free placement within a defined area.
    /// Entities are placed exactly where you release the mouse (no snapping).
    /// Uses a Collider2D to define the valid drop area bounds.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FreeformDropZone : DropZoneBase
    {
        [SerializeField, FoldoutGroup("Freeform Settings")]
        private bool _clampToBounds = true;

        [SerializeField, FoldoutGroup("Freeform Settings")]
        private Vector3 _positionOffset = Vector3.zero;

        private Collider2D _bounds;

        protected override void Awake()
        {
            base.Awake();

            _bounds = GetComponent<Collider2D>();

            if (_bounds == null)
            {
                Debug.LogError($"FreeformDropZone on {gameObject.name} requires a Collider2D component!");
            }
        }

        public override Vector3 GetDropPosition(Vector3 worldPos)
        {
            if (_bounds == null)
            {
                Debug.LogWarning($"No bounds collider on zone {GetZoneId()}");
                return worldPos + _positionOffset;
            }

            Vector3 finalPosition = worldPos;

            // Clamp to bounds if enabled
            if (_clampToBounds)
            {
                finalPosition = ClampToBounds(worldPos);
            }

            return finalPosition + _positionOffset;
        }

        public override bool CanAcceptEntity(EntityPickup entity)
        {
            if (!base.CanAcceptEntity(entity))
                return false;

            // Additional validation: ensure we have valid bounds
            if (_bounds == null)
                return false;

            return true;
        }

        private Vector3 ClampToBounds(Vector3 worldPos)
        {
            Bounds bounds = _bounds.bounds;

            float clampedX = Mathf.Clamp(worldPos.x, bounds.min.x, bounds.max.x);
            float clampedY = Mathf.Clamp(worldPos.y, bounds.min.y, bounds.max.y);
            float clampedZ = worldPos.z;

            return new Vector3(clampedX, clampedY, clampedZ);
        }

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_bounds == null)
                _bounds = GetComponent<Collider2D>();

            if (_bounds != null)
            {
                // Draw bounds area
                Gizmos.color = new Color(0, 1, 0, 0.3f);

                Bounds bounds = _bounds.bounds;
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;

                // Draw filled area
                Gizmos.DrawCube(center, size);

                // Draw outline
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(center, size);
            }
        }
#endif

        #endregion
    }
}