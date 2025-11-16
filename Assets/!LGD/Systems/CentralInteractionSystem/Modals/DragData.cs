using UnityEngine;

namespace LGD.InteractionSystem
{
    public class DragData
    {
        public Vector3 StartScreenPos { get; private set; }
        public Vector3 CurrentScreenPos { get; private set; }
        public Vector3 Delta => CurrentScreenPos - StartScreenPos;
        public InteractionHitData? CurrentHit { get; private set; }

        public DragData(Vector3 start, Vector3 current, InteractionHitData? hit = null)
        {
            StartScreenPos = start;
            CurrentScreenPos = current;
            CurrentHit = hit;
        }

        public bool HasHit => CurrentHit.HasValue;
        public bool Is3DHit => CurrentHit?.Is3D ?? false;
        public bool Is2DHit => CurrentHit.HasValue && !CurrentHit.Value.Is3D;
    }
}