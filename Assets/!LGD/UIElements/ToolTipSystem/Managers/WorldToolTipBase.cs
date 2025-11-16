using LGD.Core;
using LGD.Core.Events;
using LGD.InteractionSystem;
using ToolTipSystem.Utilities;
using UnityEngine;
using Sirenix.OdinInspector;

namespace ToolTipSystem.Components
{
    /// <summary>
    /// Base class for world objects that will use the tooltip system.
    /// Works with WorldInteractionManager hover events.
    /// </summary>
    /// <typeparam name="T">The type of tooltip data this component provides</typeparam>
    public abstract class WorldToolTipBase<T> : BaseBehaviour
    {
        [SerializeField, FoldoutGroup("Tooltip Settings")]
        private float _showDelay = 0.3f;

        [SerializeField, FoldoutGroup("Tooltip Settings")]
        private Transform _targetTransform;

        [SerializeField, FoldoutGroup("Tooltip Settings")]
        private Vector2 _offset = new Vector2(50f, 50f);

        private bool _isHovering;
        private Coroutine _showDelayCoroutine;

        public abstract T Data { get; }

        [Topic(InteractionEventIds.ON_INTERACTION_HOVER_ENTERED)]
        public void OnHoverEntered(object sender, InteractionData data)
        {
            if (data == null || !data.HasTarget)
            {
                HandleHoverExit();
                return;
            }

            if (data.Target != this.transform)
            {
                HandleHoverExit();
                return;
            }

            HandleHoverEnter();
        }

        [Topic(InteractionEventIds.ON_INTERACTION_HOVER_EXITED)]
        public void OnHoverExited(object sender, InteractionData data)
        {
            HandleHoverExit();
        }

        private void Update()
        {
            if (_isHovering)
            {
                ToolTipPositionData positionData = CreatePositionData();
                Publish(ToolTipEventIds.ON_TOOLTIP_MOVE, positionData);
            }
        }

        private void HandleHoverEnter()
        {
            if (_showDelayCoroutine != null)
                StopCoroutine(_showDelayCoroutine);

            _showDelayCoroutine = StartCoroutine(ShowTooltipDelayed());
            OnHoverEnter();
        }

        private void HandleHoverExit()
        {
            if (_showDelayCoroutine != null)
            {
                StopCoroutine(_showDelayCoroutine);
                _showDelayCoroutine = null;
            }

            if (_isHovering)
            {
                Publish(ToolTipEventIds.ON_TOOLTIP_HIDE);
                _isHovering = false;
            }

            OnHoverExit();
        }

        private System.Collections.IEnumerator ShowTooltipDelayed()
        {
            yield return new WaitForSeconds(_showDelay);
            ShowTooltip();
        }

        private void ShowTooltip()
        {
            T tooltipData = Data;

            if (tooltipData == null)
                return;

            if (!ShouldShowTooltip())
                return;

            Publish(ToolTipEventIds.ON_TOOLTIP_SHOW, tooltipData);

            // Immediately position the tooltip after showing
            ToolTipPositionData positionData = CreatePositionData();
            Publish(ToolTipEventIds.ON_TOOLTIP_MOVE, positionData);

            _isHovering = true;
        }

        private ToolTipPositionData CreatePositionData()
        {
            Transform targetTransform = GetTargetTransform();

            if (targetTransform != null)
            {
                return ToolTipPositionData.FromWorldTarget(targetTransform, _offset);
            }

            return ToolTipPositionData.FromMousePosition(_offset);
        }

        private Transform GetTargetTransform()
        {
            if (_targetTransform != null)
                return _targetTransform;

            return transform;
        }

        /// <summary>
        /// Override this to add custom logic for when hover starts
        /// </summary>
        protected virtual void OnHoverEnter()
        {
        }

        /// <summary>
        /// Override this to add custom logic for when hover ends
        /// </summary>
        protected virtual void OnHoverExit()
        {
        }

        /// <summary>
        /// Override this to conditionally show/hide tooltips
        /// </summary>
        protected virtual bool ShouldShowTooltip()
        {
            return true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HandleHoverExit();
        }

        private void OnDestroy()
        {
            HandleHoverExit();
        }
    }
}