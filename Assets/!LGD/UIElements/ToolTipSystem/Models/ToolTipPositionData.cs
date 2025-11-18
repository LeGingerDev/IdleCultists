using UnityEngine;

namespace ToolTipSystem.Utilities
{
    /// <summary>
    /// Contains positioning data for tooltips, including optional target transforms
    /// and per-instance offsets for flexible positioning
    /// </summary>
    public class ToolTipPositionData
    {
        public Vector2 ScreenPosition { get; private set; }
        public RectTransform UITarget { get; private set; }
        public Transform WorldTarget { get; private set; }
        public Vector2 Offset { get; private set; }
        public float Scale { get; private set; }
        public bool UseUITarget { get; private set; }
        public bool UseWorldTarget { get; private set; }

        private ToolTipPositionData(Vector2 screenPosition, Vector2 offset, float scale = 1f, RectTransform uiTarget = null, Transform worldTarget = null)
        {
            ScreenPosition = screenPosition;
            Offset = offset;
            Scale = scale;
            UITarget = uiTarget;
            WorldTarget = worldTarget;
            UseUITarget = uiTarget != null;
            UseWorldTarget = worldTarget != null && uiTarget == null;

        }

        public static ToolTipPositionData FromMousePosition(Vector2 offset)
        {
            return new ToolTipPositionData(Input.mousePosition, offset);
        }

        public static ToolTipPositionData FromUITarget(RectTransform target, Vector2 offset)
        {
            float scale = target != null ? target.lossyScale.x : 1f;
            return new ToolTipPositionData(Input.mousePosition, offset, scale, uiTarget: target);
        }

        public static ToolTipPositionData FromWorldTarget(Transform target, Vector2 offset)
        {
            float scale = target != null ? target.lossyScale.x : 1f;
            return new ToolTipPositionData(Input.mousePosition, offset, scale, worldTarget: target);
        }
    }
}