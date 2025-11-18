using LGD.Core.Events;
using LGD.Core.Singleton;
using System;
using System.Collections.Generic;
using ToolTipSystem.Interfaces;
using ToolTipSystem.Utilities;
using UnityEngine;
using Sirenix.OdinInspector;

namespace ToolTipSystem.Managers
{
    /// <summary>
    /// The overall controller that will listen to the events for the tooltip system
    /// </summary>
    public class ToolTipController : MonoSingleton<ToolTipController>
    {
        private Dictionary<Type, IToolTip> _toolTipMapping = new Dictionary<Type, IToolTip>();
        private IToolTip _currentToolTip;

        [SerializeField, FoldoutGroup("Position Settings")]
        private Vector2 _screenPadding = new Vector2(10f, 10f);

        [SerializeField, FoldoutGroup("Position Settings")]
        private Camera _mainCamera;

        protected override void Awake()
        {
            base.Awake();
            CameraCheck();
        }

        public void CameraCheck()
        {
            if (_mainCamera != null)
                return;
            _mainCamera = Camera.main;
        }

        public void RegisterToolTip(IToolTip toolTip)
        {
            _toolTipMapping.Add(toolTip.DataType, toolTip);
        }

        public void UnregisterToolTip(IToolTip toolTip)
        {
            _toolTipMapping.Remove(toolTip.DataType);
            
        }

        [Topic(ToolTipEventIds.ON_TOOLTIP_SHOW)]
        public void OnToolTipShow(object sender, object dataType)
        {
            IToolTip toolTip = GetToolTip(dataType.GetType());
            _currentToolTip = toolTip;
            _currentToolTip.Show(dataType);
        }

        [Topic(ToolTipEventIds.ON_TOOLTIP_HIDE)]
        public void OnToolTipHide(object sender)
        {
            if (_currentToolTip == null)
                return;

            _currentToolTip.Hide();
            _currentToolTip = null;
        }

        [Topic(ToolTipEventIds.ON_TOOLTIP_MOVE)]
        public void OnToolTipMove(object sender, ToolTipPositionData positionData)
        {
            if (_currentToolTip == null)
                return;

            Vector2 finalPosition = CalculateFinalPosition(positionData);
            _currentToolTip.Move(finalPosition);
        }

        private Vector2 CalculateFinalPosition(ToolTipPositionData positionData)
        {
            Vector2 basePosition = GetBasePosition(positionData);
            Vector2 adjustedPosition = ClampToScreen(basePosition);
            return adjustedPosition;
        }

        private Vector2 GetBasePosition(ToolTipPositionData positionData)
        {
            if (positionData.UseUITarget)
            {
                return GetUITargetPosition(positionData.UITarget, positionData.Offset);
            }

            if (positionData.UseWorldTarget)
            {
                return GetWorldTargetPosition(positionData.WorldTarget, positionData.Offset);
            }

            return positionData.ScreenPosition + positionData.Offset;
        }

        private Vector2 GetUITargetPosition(RectTransform target, Vector2 offset)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            float targetRight = corners[2].x;
            float targetCenterY = (corners[0].y + corners[2].y) * 0.5f;

            // Account for target's scale when applying offset
            // This makes tooltip offset scale with zoomed content (e.g., skill tree zoom)
            float scaleFactor = target.lossyScale.x;
            Vector2 scaledOffset = offset * scaleFactor;

            return new Vector2(targetRight, targetCenterY) + scaledOffset;
        }

        private Vector2 GetWorldTargetPosition(Transform target, Vector2 offset)
        {
            CameraCheck();
            Vector3 worldPosition = target.position;
            Vector2 screenPosition = _mainCamera.WorldToScreenPoint(worldPosition);

            return screenPosition + offset;
        }

        private Vector2 ClampToScreen(Vector2 position)
        {
            if (_currentToolTip == null)
                return position;

            RectTransform tooltipRect = GetTooltipRectTransform();
            if (tooltipRect == null)
                return position;

            Rect tooltipBounds = GetTooltipScreenBounds(tooltipRect, position);
            Vector2 clampedPosition = position;

            clampedPosition = ClampHorizontal(clampedPosition, tooltipBounds);
            clampedPosition = ClampVertical(clampedPosition, tooltipBounds);

            return clampedPosition;
        }

        private Vector2 ClampHorizontal(Vector2 position, Rect tooltipBounds)
        {
            float screenWidth = Screen.width;

            if (tooltipBounds.xMax > screenWidth - _screenPadding.x)
            {
                float overflow = tooltipBounds.xMax - (screenWidth - _screenPadding.x);
                position.x -= overflow;
            }

            if (tooltipBounds.xMin < _screenPadding.x)
            {
                float overflow = _screenPadding.x - tooltipBounds.xMin;
                position.x += overflow;
            }

            return position;
        }

        private Vector2 ClampVertical(Vector2 position, Rect tooltipBounds)
        {
            float screenHeight = Screen.height;

            if (tooltipBounds.yMax > screenHeight - _screenPadding.y)
            {
                float overflow = tooltipBounds.yMax - (screenHeight - _screenPadding.y);
                position.y -= overflow;
            }

            if (tooltipBounds.yMin < _screenPadding.y)
            {
                float overflow = _screenPadding.y - tooltipBounds.yMin;
                position.y += overflow;
            }

            return position;
        }

        private Rect GetTooltipScreenBounds(RectTransform tooltipRect, Vector2 position)
        {
            Vector2 size = tooltipRect.rect.size;
            Vector2 pivot = tooltipRect.pivot;

            float minX = position.x - (size.x * pivot.x);
            float minY = position.y - (size.y * pivot.y);

            return new Rect(minX, minY, size.x, size.y);
        }

        private RectTransform GetTooltipRectTransform()
        {
            if (_currentToolTip is MonoBehaviour tooltipMono)
            {
                return tooltipMono.transform as RectTransform;
            }

            return null;
        }

        public IToolTip GetToolTip(Type type)
        {
            return _toolTipMapping[type];
        }
    }
}