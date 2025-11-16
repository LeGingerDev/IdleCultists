using UnityEngine;


namespace LGD.InteractionSystem
{
    /// <summary>
    /// Wrapper for both 3D and 2D raycast hit data.
    /// Provides unified access to common properties.
    /// </summary>
    public struct InteractionHitData
    {
        public Transform Transform { get; private set; }
        public Vector3 Point { get; private set; }
        public Vector3 Normal { get; private set; }
        public float Distance { get; private set; }
        public Collider Collider3D { get; private set; }
        public Collider2D Collider2D { get; private set; }
        public bool Is3D { get; private set; }

        public InteractionHitData(RaycastHit hit3D)
        {
            Transform = hit3D.transform;
            Point = hit3D.point;
            Normal = hit3D.normal;
            Distance = hit3D.distance;
            Collider3D = hit3D.collider;
            Collider2D = null;
            Is3D = true;
        }

        public InteractionHitData(RaycastHit2D hit2D)
        {
            Transform = hit2D.transform;
            Point = hit2D.point;
            Normal = hit2D.normal;
            Distance = hit2D.distance;
            Collider3D = null;
            Collider2D = hit2D.collider;
            Is3D = false;
        }
    }
}