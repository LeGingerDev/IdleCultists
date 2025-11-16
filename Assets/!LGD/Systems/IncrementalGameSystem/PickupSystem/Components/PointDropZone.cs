using LGD.PickupSystem;
using Sirenix.OdinInspector;
using UnityEngine;
/// <summary>
/// Single-point drop zone for specific locations like altars, beds, or stations.
/// Always returns the exact drop point position.
/// </summary>
public class PointDropZone : DropZoneBase
{
    [SerializeField, FoldoutGroup("Point Settings")]
    private Transform _dropPoint;

    [SerializeField, FoldoutGroup("Point Settings")]
    private bool _useThisTransform = true;

    private void Start()
    {
        if (_useThisTransform && _dropPoint == null)
        {
            _dropPoint = this.transform;
        }
    }

    public override Vector3 GetDropPosition(Vector3 worldPos)
    {
        if (_dropPoint == null)
        {
            Debug.LogWarning($"Drop point not assigned on zone {GetZoneId()}");
            return worldPos;
        }

        return _dropPoint.position;
    }

    #region Editor Helpers

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform point = _useThisTransform ? this.transform : _dropPoint;

        if (point != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(point.position, 0.3f);
            Gizmos.DrawLine(point.position, point.position + Vector3.up * 0.5f);
        }
    }
#endif

    #endregion
}
