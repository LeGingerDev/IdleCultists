using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;
using LGD.Core.Singleton;

public class CameraController2D : MonoSingleton<CameraController2D>
{
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float _moveSpeed = 10f;
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private LayerMask _dragLayerMask;

    [FoldoutGroup("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 1f;
    [FoldoutGroup("Zoom Settings")]
    [SerializeField] private float _startOrthographicSize = 5f;
    [FoldoutGroup("Zoom Settings")]
    [SerializeField] private float _minOrthographicSize = 2f;
    [FoldoutGroup("Zoom Settings")]
    [SerializeField] private float _maxOrthographicSize = 10f;
    [FoldoutGroup("Zoom Settings")]
    [SerializeField] private float _zoomSmoothTime = 0.2f;

    [FoldoutGroup("Boundary Settings")]
    [SerializeField] private bool _useBoundaries = true;
    [FoldoutGroup("Boundary Settings")]
    [SerializeField] private float _minX = -50f;
    [FoldoutGroup("Boundary Settings")]
    [SerializeField] private float _maxX = 50f;
    [FoldoutGroup("Boundary Settings")]
    [SerializeField] private float _minY = -50f;
    [FoldoutGroup("Boundary Settings")]
    [SerializeField] private float _maxY = 50f;

    [FoldoutGroup("Pan Settings")]
    [SerializeField] private float _panDuration = 1f;
    [FoldoutGroup("Pan Settings")]
    [SerializeField] private Ease _panEaseType = Ease.OutCubic;

    [FoldoutGroup("Lock-On Settings")]
    [SerializeField] private float _lockOnSmoothTime = 0.3f;
    [FoldoutGroup("Lock-On Settings")]
    [SerializeField] private float _lockOnSpeed = 10f;

    [FoldoutGroup("Zoom Settings")]
    [SerializeField] private float _zoomAnimationDuration = 0.5f;
    [FoldoutGroup("Zoom Settings")]
    [SerializeField] private Ease _zoomEaseType = Ease.OutCubic;

    [FoldoutGroup("Shake Settings")]
    [SerializeField] private CameraShakeData _defaultShakeData;

    private Camera _camera;
    private float _currentOrthographicSize;
    private float _targetOrthographicSize;
    private float _zoomVelocity;

    private Transform _currentTarget;
    private Vector3 _lockOnVelocity;
    private bool _isRightMouseDragging;
    private Vector3 _lastMouseWorldPosition;
    private Tween _panTween;
    private Tween _zoomTween;
    private Tween _shakeTween;

    private bool _isLocked;

    protected override void Awake()
    {
        base.Awake();
        InitializeReferences();
        InitializeZoom();
    }

    private void Update()
    {
        if (_isLocked)
            return;
        HandleMovementInput();
        HandleMouseDragInput();
        HandleZoomInput();
        HandleLockOn();
    }

    private void InitializeReferences()
    {
        _camera = GetComponent<Camera>();

        if (_camera == null)
        {
            DebugManager.Error("[Core] CameraController2D: No Camera component found!");
            return;
        }

        if (!_camera.orthographic)
        {
            DebugManager.Warning("[Core] CameraController2D: Camera is not orthographic. Setting to orthographic mode.");
            _camera.orthographic = true;
        }
    }

    private void InitializeZoom()
    {
        if (_camera != null)
        {
            _currentOrthographicSize = _startOrthographicSize;
            _targetOrthographicSize = _startOrthographicSize;
            _camera.orthographicSize = _startOrthographicSize;
        }
    }

    private void HandleMovementInput()
    {
        if (_isRightMouseDragging) return;
        if (IsPointerOverUI()) return;

        Vector3 inputDirection = GetMovementDirection();

        if (inputDirection != Vector3.zero)
        {
            ClearLockOn();
            MoveCamera(inputDirection);
        }
    }

    /// <summary>
    /// Check if the mouse pointer is currently over UI
    /// </summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private Vector3 GetMovementDirection()
    {
        float horizontal = 0f;
        float vertical = 0f;

        // WASD Input
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;

        // Arrow Keys Input
        if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;

        return new Vector3(horizontal, vertical, 0f).normalized;
    }

    private void MoveCamera(Vector3 direction)
    {
        Vector3 movement = direction * _moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;
        transform.position = ClampPosition(newPosition);
    }

    private void HandleMouseDragInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            StartMouseDrag();
        }
        else if (Input.GetMouseButton(1) && _isRightMouseDragging)
        {
            UpdateMouseDrag();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            StopMouseDrag();
        }
    }

    private void StartMouseDrag()
    {
        if (_camera == null) return;
        if (IsPointerOverUI()) return;

        Vector3 worldPosition = GetMouseWorldPosition();
        if (worldPosition != Vector3.zero)
        {
            ClearLockOn();
            _isRightMouseDragging = true;
            _lastMouseWorldPosition = worldPosition;
        }
    }

    private void UpdateMouseDrag()
    {
        if (_camera == null) return;

        Vector3 currentMouseWorldPosition = GetMouseWorldPosition();

        if (currentMouseWorldPosition == Vector3.zero) return;

        Vector3 mouseDelta = _lastMouseWorldPosition - currentMouseWorldPosition;

        Vector3 newPosition = transform.position + mouseDelta;
        transform.position = ClampPosition(newPosition);

        // Update anchor point after clamping
        _lastMouseWorldPosition = GetMouseWorldPosition();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero, Mathf.Infinity, _dragLayerMask);

        if (hit.collider != null)
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    private void StopMouseDrag()
    {
        _isRightMouseDragging = false;
    }

    private void HandleZoomInput()
    {
        if (IsPointerOverUI()) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            UpdateZoomTarget(scrollInput);
        }

        ApplyZoom();
    }

    private void UpdateZoomTarget(float scrollInput)
    {
        _targetOrthographicSize -= scrollInput * _zoomSpeed;
        _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
    }

    private void ApplyZoom()
    {
        if (_camera == null) return;

        _currentOrthographicSize = Mathf.SmoothDamp(
            _currentOrthographicSize,
            _targetOrthographicSize,
            ref _zoomVelocity,
            _zoomSmoothTime
        );

        _camera.orthographicSize = _currentOrthographicSize;
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        if (!_useBoundaries) return position;

        position.x = Mathf.Clamp(position.x, _minX, _maxX);
        position.y = Mathf.Clamp(position.y, _minY, _maxY);
        return position;
    }

    [Button]
    public void PanToTarget(Transform target, float duration = 0)
    {
        if (target == null)
        {
            DebugManager.Warning("[Core] CameraController2D: Cannot pan to null target.");
            return;
        }

        PanToPosition(target.position, duration);
    }

    public void PanToPosition(Vector3 targetPosition, float duration = 0)
    {
        float panDuration = duration;
        if (duration < 0)
            panDuration = 0;
        if (duration == 0)
            panDuration = _panDuration;
        _panTween?.Kill();

        Vector3 clampedPosition = ClampPosition(new Vector3(targetPosition.x, targetPosition.y, transform.position.z));

        _panTween = transform.DOMove(clampedPosition, panDuration)
            .SetEase(_panEaseType);
    }

    #region Lock-On Logic

    private void HandleLockOn()
    {
        if (_currentTarget == null) return;

        Vector3 targetPosition = new Vector3(_currentTarget.position.x, _currentTarget.position.y, transform.position.z);
        targetPosition = ClampPosition(targetPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _lockOnVelocity,
            _lockOnSmoothTime,
            _lockOnSpeed
        );
    }

    public void LockOnToTarget(Transform target)
    {
        _currentTarget = target;
        _lockOnVelocity = Vector3.zero;
    }

    public void ClearLockOn()
    {
        _currentTarget = null;
        _lockOnVelocity = Vector3.zero;
    }

    public Transform GetCurrentLockOnTarget()
    {
        return _currentTarget;
    }

    #endregion

    #region Public Getters and Setters

    public float GetCurrentOrthographicSize()
    {
        return _currentOrthographicSize;
    }

    public float GetMoveSpeed()
    {
        return _moveSpeed;
    }

    public void SetMoveSpeed(float speed)
    {
        _moveSpeed = Mathf.Max(0f, speed);
    }

    public void SetZoomLimits(float minSize, float maxSize)
    {
        _minOrthographicSize = Mathf.Max(0.1f, minSize);
        _maxOrthographicSize = Mathf.Max(_minOrthographicSize, maxSize);
        _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
    }

    public void SetBoundaries(float minX, float maxX, float minY, float maxY)
    {
        _minX = minX;
        _maxX = maxX;
        _minY = minY;
        _maxY = maxY;

        // Immediately clamp current position if it's outside new boundaries
        transform.position = ClampPosition(transform.position);
    }

    public void SetUseBoundaries(bool use)
    {
        _useBoundaries = use;
    }

    public bool GetUseBoundaries()
    {
        return _useBoundaries;
    }

    public void ToggleIsLocked(bool isLocked) => _isLocked = isLocked;

    [Button]
    public void ZoomToSize(float targetSize, bool animate = true, bool canDoAnySize = false, float customDuration = 0f)
    {
        if(canDoAnySize == false)
            targetSize = Mathf.Clamp(targetSize, _minOrthographicSize, _maxOrthographicSize);

        if (animate)
        {
            ZoomToSizeAnimated(targetSize, customDuration,canDoAnySize);
        }
        else
        {
            ZoomToSizeInstant(targetSize, canDoAnySize);
        }
    }

    public void ZoomToSizeAnimated(float targetSize, float customDuration = 0, bool canDoAnySize = false)
    {
        float duration = customDuration > 0 ? customDuration : _zoomAnimationDuration;

        _zoomTween?.Kill();
        float clampedSize = targetSize;
        if(canDoAnySize == false)
            clampedSize = Mathf.Clamp(targetSize, _minOrthographicSize, _maxOrthographicSize);

        _zoomTween = DOTween.To(
            () => _currentOrthographicSize,
            x => {
                _currentOrthographicSize = x;
                _targetOrthographicSize = x;
                if (_camera != null)
                    _camera.orthographicSize = x;
            },
            clampedSize,
            duration
        ).SetEase(_zoomEaseType);
    }

    public void ZoomToSizeInstant(float targetSize, bool canGoAnySize = false)
    {
        _zoomTween?.Kill();
        float clampedSize = targetSize;
        if(canGoAnySize == false)
            clampedSize = Mathf.Clamp(targetSize, _minOrthographicSize, _maxOrthographicSize);

        _currentOrthographicSize = clampedSize;
        _targetOrthographicSize = clampedSize;

        if (_camera != null)
            _camera.orthographicSize = clampedSize;
    }
    #endregion

    #region Camera Shake

    [Button("Test Shake")]
    public void ShakeCamera()
    {
        if (_defaultShakeData != null)
        {
            ShakeCamera(_defaultShakeData);
        }
        else
        {
            DebugManager.Warning("[Core] CameraController2D: No default shake data assigned!");
        }
    }

    public void ShakeCamera(CameraShakeData shakeData)
    {
        if (shakeData == null)
        {
            DebugManager.Warning("[Core] CameraController2D: Cannot shake with null shake data!");
            return;
        }

        _shakeTween?.Kill();

        _shakeTween = transform.DOShakePosition(
            shakeData.GetDuration(),
            shakeData.GetStrength(),
            shakeData.GetVibrato(),
            shakeData.GetRandomness(),
            false,
            shakeData.GetFadeOut()
        ).SetUpdate(true);
    }

    public void ShakeCamera(float duration, float strength, int vibrato = 10, float randomness = 90f, bool fadeOut = true)
    {
        CameraShakeData shakeData = new CameraShakeData(duration, strength, vibrato, randomness, fadeOut);
        ShakeCamera(shakeData);
    }

    public void StopShake()
    {
        _shakeTween?.Kill();
    }

    #endregion
}