using UnityEngine;

namespace LGD.InteractionSystem
{
    public class InteractionData
    {
        public Transform Target { get; private set; }
        public InteractionHitData? Hit { get; private set; }
        public Vector3 ScreenPosition { get; private set; }
        public InteractionType Type { get; private set; }

        public InteractionData(InteractionType type, InteractionHitData? hit, Vector3 screenPos)
        {
            Type = type;
            Hit = hit;
            Target = hit?.Transform;
            ScreenPosition = screenPos;
        }

        public static InteractionData Empty(InteractionType type, Vector3 screenPos)
        {
            return new InteractionData(type, null, screenPos);
        }

        public bool HasTarget => Target != null;
        public bool Is3DHit => Hit?.Is3D ?? false;
        public bool Is2DHit => Hit.HasValue && !Hit.Value.Is3D;
    }
}