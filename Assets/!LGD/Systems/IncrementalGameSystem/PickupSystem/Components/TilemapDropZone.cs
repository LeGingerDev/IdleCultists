using LGD.PickupSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;


/// <summary>
/// Drop zone based on tilemap bounds with optional grid snapping.
/// Returns grid-aligned positions for entity placement.
/// </summary>
public class TilemapDropZone : DropZoneBase
{
    [SerializeField, FoldoutGroup("Tilemap Settings")]
    private Tilemap _tilemap;

    [SerializeField, FoldoutGroup("Tilemap Settings")]
    private bool _snapToGrid = true;

    [SerializeField, FoldoutGroup("Tilemap Settings")]
    private Vector3 _cellOffset = Vector3.zero;

    public override Vector3 GetDropPosition(Vector3 worldPos)
    {
        if (_tilemap == null)
        {
            Debug.LogWarning($"Tilemap not assigned on zone {GetZoneId()}");
            return worldPos;
        }

        if (_snapToGrid)
        {
            // Convert world position to cell position
            Vector3Int cellPosition = _tilemap.WorldToCell(worldPos);

            // Check if cell is within tilemap bounds
            if (!IsWithinBounds(cellPosition))
            {
                cellPosition = ClampToBounds(cellPosition);
            }

            // Convert back to world position (cell center)
            Vector3 snappedPosition = _tilemap.GetCellCenterWorld(cellPosition);
            return snappedPosition + _cellOffset;
        }

        return worldPos + _cellOffset;
    }

    public override bool CanAcceptEntity(EntityPickup entity)
    {
        if (!base.CanAcceptEntity(entity))
            return false;

        // Additional tilemap-specific validation could go here
        // For example: check if the target cell is occupied

        return true;
    }

    private bool IsWithinBounds(Vector3Int cellPosition)
    {
        if (_tilemap == null)
            return false;

        BoundsInt bounds = _tilemap.cellBounds;
        return bounds.Contains(cellPosition);
    }

    private Vector3Int ClampToBounds(Vector3Int cellPosition)
    {
        if (_tilemap == null)
            return cellPosition;

        BoundsInt bounds = _tilemap.cellBounds;

        int clampedX = Mathf.Clamp(cellPosition.x, bounds.xMin, bounds.xMax - 1);
        int clampedY = Mathf.Clamp(cellPosition.y, bounds.yMin, bounds.yMax - 1);
        int clampedZ = Mathf.Clamp(cellPosition.z, bounds.zMin, bounds.zMax - 1);

        return new Vector3Int(clampedX, clampedY, clampedZ);
    }

    #region Editor Helpers

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_tilemap == null)
            return;

        // Draw tilemap bounds
        Gizmos.color = Color.cyan;
        BoundsInt bounds = _tilemap.cellBounds;

        Vector3 min = _tilemap.GetCellCenterWorld(new Vector3Int(bounds.xMin, bounds.yMin, bounds.zMin));
        Vector3 max = _tilemap.GetCellCenterWorld(new Vector3Int(bounds.xMax, bounds.yMax, bounds.zMax));

        Vector3 size = max - min;
        Vector3 center = (min + max) / 2f;

        Gizmos.DrawWireCube(center, size);
    }
#endif

    #endregion
}
