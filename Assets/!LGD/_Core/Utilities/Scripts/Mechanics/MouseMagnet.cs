using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System.Collections;

public class MouseMagnet : MonoBehaviour
{
    [FoldoutGroup("Detection Settings")]
    [SerializeField] private float _detectionRadius = 5f;

    [FoldoutGroup("Detection Settings")]
    [SerializeField] private float _collectRadius = 0.5f;

    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float _acceleration = 20f;

    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float _maxSpeed = 15f;

    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float _velocityDampening = 0.95f;

    [FoldoutGroup("Movement Settings")]
    [SerializeField] private bool _isMagnetActive = false;

    [FoldoutGroup("Events")]
    [SerializeField] private UnityEvent _onCollect;

    private Rigidbody2D _rb2D;
    private Coroutine _magnetCoroutine;
    private bool _isBeingMagnetized;
    private Camera _mainCamera;
    private Vector2 _currentVelocity;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _rb2D = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        StartMagnetizing();
    }

    private void OnDisable()
    {
        StopMagnetizing();
    }

    private void StartMagnetizing()
    {
        if (_magnetCoroutine == null)
        {
            _magnetCoroutine = StartCoroutine(MagnetizeRoutine());
        }
    }

    private void StopMagnetizing()
    {
        if (_magnetCoroutine != null)
        {
            StopCoroutine(_magnetCoroutine);
            _magnetCoroutine = null;
        }
    }

    private IEnumerator MagnetizeRoutine()
    {
        while (true)
        {
            // Only check for initial detection if magnet is active and not already magnetizing
            if (_isMagnetActive && !_isBeingMagnetized)
            {
                Vector3 mouseWorldPosition = GetMouseWorldPosition();
                float distanceToMouse = Vector2.Distance(transform.position, mouseWorldPosition);

                // Enter magnetization range
                if (distanceToMouse <= _detectionRadius)
                {
                    _isBeingMagnetized = true;
                    _currentVelocity = Vector2.zero; // Reset velocity when starting magnetization
                }
            }

            // Once magnetizing, continue until collected
            if (_isBeingMagnetized)
            {
                if (_rb2D != null)
                    _rb2D.bodyType = RigidbodyType2D.Kinematic; // Disable physics interactions

                Vector3 mouseWorldPosition = GetMouseWorldPosition();
                float distanceToMouse = Vector2.Distance(transform.position, mouseWorldPosition);

                // Calculate direction to mouse
                Vector2 directionToMouse = (mouseWorldPosition - transform.position).normalized;

                // Apply acceleration
                _currentVelocity += directionToMouse * _acceleration * Time.deltaTime;

                // Apply dampening to prevent orbiting - reduces perpendicular velocity
                _currentVelocity *= _velocityDampening;

                // Clamp velocity to max speed
                if (_currentVelocity.magnitude > _maxSpeed)
                {
                    _currentVelocity = _currentVelocity.normalized * _maxSpeed;
                }

                // Move with current velocity
                transform.position += (Vector3)_currentVelocity * Time.deltaTime;

                // Check if close enough to collect
                if (distanceToMouse <= _collectRadius)
                {
                    HandleCollection();
                    yield break; // Stop after collection
                }
            }

            yield return null;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -_mainCamera.transform.position.z; // Adjust for 2D camera distance
        return _mainCamera.ScreenToWorldPoint(mousePosition);
    }

    private void HandleCollection()
    {
        _onCollect?.Invoke();
        StopMagnetizing();
    }

    // Public methods to control magnetization
    public void SetMagnetActive(bool isActive)
    {
        _isMagnetActive = isActive;
    }

    // Public getters for external access
    public bool GetIsBeingMagnetized() => _isBeingMagnetized;
    public bool GetIsMagnetActive() => _isMagnetActive;
    public float GetDetectionRadius() => _detectionRadius;
    public float GetCollectRadius() => _collectRadius;

    // Visual debugging in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _collectRadius);
    }
}