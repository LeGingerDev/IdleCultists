using Audio.Core;
using Audio.Managers;
using DG.Tweening;
using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LGD.UIelements.Panels
{
    public enum SlideDirection
    {
        Right,
        Left,
        Top,
        Bottom
    }

    public abstract class SlidePanel : BaseBehaviour
    {
        [SerializeField, FoldoutGroup("Panel")]
        private PanelType _panelType;

        [SerializeField, FoldoutGroup("Panel/References")]
        protected CanvasGroup _canvasGroup;
        [SerializeField, FoldoutGroup("Panel/References")]
        protected RectTransform _panelMover;

        [SerializeField, FoldoutGroup("Panel/Animation Settings")]
        protected SlideDirection _slideDirection = SlideDirection.Right;
        [SerializeField, FoldoutGroup("Panel/Animation Settings")]
        protected float _moveDuration = 0.5f;
        [SerializeField, FoldoutGroup("Panel/Animation Settings")]
        protected Ease _easeType = Ease.OutCubic;
        [SerializeField, FoldoutGroup("Panel/Animation Settings")]
        protected float _offscreenMargin = 50f;

        [SerializeField, FoldoutGroup("Panel/Events")]
        private UnityEvent _onPanelOpened;
        [SerializeField, FoldoutGroup("Panel/Events")]
        private UnityEvent _onPanelClosed;

        protected Vector2 _originalPosition;
        protected float _calculatedMoveDistance;
        protected Coroutine _waitToCloseCoroutine;
        protected Tween _moveTween;
        protected Tween _fadeTween;

        private bool _isOpen;
        private Canvas _canvas;

        protected virtual void Awake()
        {
            _originalPosition = _panelMover.anchoredPosition;
            _canvas = GetComponentInParent<Canvas>();
            CalculateMoveDistance();
        }

        protected virtual void Start()
        {
            HideImmediate();
        }

        // --------------------------------------------------------------------
        // Setup
        // --------------------------------------------------------------------

        private void CalculateMoveDistance()
        {
            if (_canvas == null)
            {
                DebugManager.Error("[UI] No Canvas found in parents!");
                return;
            }

            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

            switch (_slideDirection)
            {
                case SlideDirection.Right:
                case SlideDirection.Left:
                    float panelWidth = _panelMover.rect.width;
                    _calculatedMoveDistance = panelWidth + _offscreenMargin;
                    break;

                case SlideDirection.Top:
                case SlideDirection.Bottom:
                    float panelHeight = _panelMover.rect.height;
                    _calculatedMoveDistance = panelHeight + _offscreenMargin;
                    break;
            }
        }

        // --------------------------------------------------------------------
        // Visibility Controls
        // --------------------------------------------------------------------

        protected void ShowPanel(float? durationOverride = null)
        {
            AudioManager.Instance.PlaySFX(AudioConstIds.UI_POPUP);

            StopCloseCoroutine();
            SetPanelVisible(true, durationOverride);
            _isOpen = true;
            OnOpen();

        }

        protected void HidePanel(float? durationOverride = null)
        {
            AudioManager.Instance.PlaySFX(AudioConstIds.UI_POPUP_CLOSE);

            StopCloseCoroutine();
            SetPanelVisible(false, durationOverride);
            _isOpen = false;
            OnClose();
        }

        protected void HideAfterDelay(float delay)
        {
            StopCloseCoroutine();
            _waitToCloseCoroutine = StartCoroutine(WaitToHide(delay));
        }

        private IEnumerator WaitToHide(float delay)
        {
            yield return new WaitForSeconds(delay);
            HidePanel();
        }

        private void SetPanelVisible(bool visible, float? durationOverride = null)
        {
            float duration = durationOverride ?? _moveDuration;

            _moveTween?.Kill();
            _fadeTween?.Kill();

            Vector2 targetPosition = GetTargetPosition(visible);
            float targetAlpha = visible ? 1f : 0f;

            _moveTween = _panelMover.DOAnchorPos(targetPosition, duration).SetEase(_easeType);
            _fadeTween = _canvasGroup.DOFade(targetAlpha, duration)
                .OnStart(() =>
                {
                    if (!visible)
                        DisableInteraction();
                })
                .SetEase(_easeType)
                .OnComplete(() =>
                {
                    if (visible)
                    {
                        EnableInteraction();
                        _onPanelOpened?.Invoke();
                    }
                    else
                    {
                        _onPanelClosed?.Invoke();
                        DisableInteraction();
                    }
                });
        }

        private Vector2 GetTargetPosition(bool visible)
        {
            if (visible)
                return _originalPosition;

            Vector2 offset = Vector2.zero;

            switch (_slideDirection)
            {
                case SlideDirection.Right:
                    offset = new Vector2(_calculatedMoveDistance, 0f);
                    break;
                case SlideDirection.Left:
                    offset = new Vector2(-_calculatedMoveDistance, 0f);
                    break;
                case SlideDirection.Top:
                    offset = new Vector2(0f, _calculatedMoveDistance);
                    break;
                case SlideDirection.Bottom:
                    offset = new Vector2(0f, -_calculatedMoveDistance);
                    break;
            }

            return _originalPosition + offset;
        }

        public void HideImmediate()
        {
            StopCloseCoroutine();
            _moveTween?.Kill();
            _fadeTween?.Kill();

            _panelMover.anchoredPosition = GetTargetPosition(false);
            _canvasGroup.alpha = 0f;
            DisableInteraction();
        }

        protected void StopCloseCoroutine()
        {
            if (_waitToCloseCoroutine != null)
            {
                StopCoroutine(_waitToCloseCoroutine);
                _waitToCloseCoroutine = null;
            }
        }

        protected void EnableInteraction()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        protected void DisableInteraction()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        // --------------------------------------------------------------------
        // AEvent Hooks
        // --------------------------------------------------------------------

        [Topic(PanelEventIds.ON_PANEL_OPEN_REQUESTED)]
        public void OnPanelRequested(object sender, PanelType panelType)
        {
            if (panelType != _panelType)
            {
                if (_isOpen)
                    HidePanel();
                return;
            }

            ShowPanel();
        }

        [Topic(PanelEventIds.ON_PANEL_CLOSE_REQUESTED)]
        public void OnPanelCloseRequested(object sender, PanelType panelType)
        {
            if (panelType != _panelType)
                return;
            HidePanel();
        }

        [Topic(PanelEventIds.ON_PANEL_FORCE_CLOSE_ALL)]
        public void OnPanelForceCloseAll(object sender)
        {
            if (_isOpen)
                HidePanel();
        }

        // --------------------------------------------------------------------
        // Abstract Hooks
        // --------------------------------------------------------------------

        [Button]
        public void ForceOpen()
        {
            ShowPanel();
        }

        [Button]
        public void ForceClose()
        {
            HidePanel();
        }

        protected abstract void OnOpen();
        protected abstract void OnClose();
    }
}