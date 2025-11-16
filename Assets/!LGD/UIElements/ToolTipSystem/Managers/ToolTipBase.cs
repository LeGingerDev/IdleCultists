using LGD.Core;
using ToolTipSystem.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace ToolTipSystem.Components
{
    /// <summary>
    /// Base class for all UI components that will use the tooltip system
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ToolTipBase<T> : BaseBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField, FoldoutGroup("Tooltip Settings")]
        private RectTransform _targetTransform;

        [SerializeField, FoldoutGroup("Tooltip Settings")]
        private Vector2 _offset = new Vector2(50f, 50f);

        public abstract T Data { get; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnter();
            Publish(ToolTipEventIds.ON_TOOLTIP_SHOW, Data);

            // Immediately position the tooltip after showing
            ToolTipPositionData positionData = CreatePositionData();
            Publish(ToolTipEventIds.ON_TOOLTIP_MOVE, positionData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExit();
            Publish(ToolTipEventIds.ON_TOOLTIP_HIDE);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            ToolTipPositionData positionData = CreatePositionData();
            Publish(ToolTipEventIds.ON_TOOLTIP_MOVE, positionData);
        }

        private ToolTipPositionData CreatePositionData()
        {
            RectTransform targetRect = GetTargetTransform();

            if (targetRect != null)
            {
                return ToolTipPositionData.FromUITarget(targetRect, _offset);
            }

            return ToolTipPositionData.FromMousePosition(_offset);
        }

        private RectTransform GetTargetTransform()
        {
            if (_targetTransform != null)
                return _targetTransform;

            return transform as RectTransform;
        }

        protected virtual void OnPointerEnter()
        {
        }

        protected virtual void OnPointerExit()
        {
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Publish(ToolTipEventIds.ON_TOOLTIP_HIDE);
        }

        private void OnDestroy()
        {
            Publish(ToolTipEventIds.ON_TOOLTIP_HIDE);
        }
    }
}