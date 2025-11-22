using Sirenix.OdinInspector;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace LGD.Gameplay.Polish
{
    /// <summary>
    /// Controls the rubber duck's idle and jump behavior.
    /// Plays idle animation and periodically triggers jump animations.
    /// Jump direction is determined via animation events.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RubberDuckController : SerializedMonoBehaviour
    {
        #region Inspector Fields

        [SerializeField, FoldoutGroup("References")]
        [Tooltip("Animator component (auto-assigned if null)")]
        private Animator _animator;

        [SerializeField, FoldoutGroup("References")]
        [Tooltip("Rigidbody2D component (auto-assigned if null)")]
        private Rigidbody2D _rb;

        [SerializeField, FoldoutGroup("References")]
        [Tooltip("SpriteRenderer to flip based on movement direction")]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, FoldoutGroup("Animation Settings")]
        [Tooltip("Name of the idle animation state")]
        private string _idleAnimationName = "Idle";

        [SerializeField, FoldoutGroup("Animation Settings")]
        [Tooltip("Name of the jump animation state")]
        private string _jumpAnimationName = "Jump";

        [SerializeField, FoldoutGroup("Jump Timing")]
        [Tooltip("Minimum seconds between jump animations")]
        [MinValue(0.1f)]
        private float _minJumpInterval = 3f;

        [SerializeField, FoldoutGroup("Jump Timing")]
        [Tooltip("Maximum seconds between jump animations")]
        [MinValue(0.1f)]
        private float _maxJumpInterval = 8f;

        [SerializeField, FoldoutGroup("Jump Physics")]
        [Tooltip("Force applied when jumping")]
        [MinValue(0f)]
        private float _jumpForce = 5f;

        [SerializeField, FoldoutGroup("Jump Physics")]
        [Tooltip("Whether to apply jump force relative to current velocity or replace it")]
        private bool _additive = false;

        [SerializeField, FoldoutGroup("Settings")]
        [Tooltip("Auto-start jump cycle on enable")]
        private bool _autoStart = true;

        [SerializeField, FoldoutGroup("Settings")]
        [Tooltip("Play idle animation on start")]
        private bool _playIdleOnStart = true;

        #endregion

        #region Runtime State

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private bool _isRunning;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private float _nextJumpTime;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private int _totalJumps;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private Vector2 _lastJumpDirection;

        private Coroutine _jumpCycleCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Auto-assign components if not set
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_rb == null)
                _rb = GetComponent<Rigidbody2D>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Validate
            if (_animator == null)
                Debug.LogError($"[RubberDuckController] No Animator found on {gameObject.name}!");

            if (_rb == null)
                Debug.LogError($"[RubberDuckController] No Rigidbody2D found on {gameObject.name}!");

            if (_spriteRenderer == null)
                Debug.LogWarning($"[RubberDuckController] No SpriteRenderer assigned on {gameObject.name}. Sprite flipping will not work.");
        }

        private void Start()
        {
            if (_playIdleOnStart)
            {
                PlayIdleAnimation();
            }
        }

        private void OnEnable()
        {
            if (_autoStart)
            {
                StartJumpCycle();
            }
        }

        private void OnDisable()
        {
            StopJumpCycle();
        }

        private void OnValidate()
        {
            // Ensure max is always >= min
            if (_maxJumpInterval < _minJumpInterval)
            {
                _maxJumpInterval = _minJumpInterval;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Play the idle animation.
        /// </summary>
        [Button("Play Idle Animation"), FoldoutGroup("Debug")]
        public void PlayIdleAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError($"[RubberDuckController] No Animator on {gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(_idleAnimationName))
            {
                Debug.LogWarning($"[RubberDuckController] Idle animation name is empty on {gameObject.name}");
                return;
            }

            _animator.Play(_idleAnimationName);
            Debug.Log($"[RubberDuckController] Playing idle animation on {gameObject.name}");
        }

        /// <summary>
        /// Start the automatic jump cycle.
        /// </summary>
        [Button("Start Jump Cycle"), FoldoutGroup("Debug")]
        public void StartJumpCycle()
        {
            if (_isRunning)
            {
                Debug.LogWarning($"[RubberDuckController] Jump cycle already running on {gameObject.name}");
                return;
            }

            if (_jumpCycleCoroutine != null)
                StopCoroutine(_jumpCycleCoroutine);

            _jumpCycleCoroutine = StartCoroutine(JumpCycleCoroutine());
            _isRunning = true;

            Debug.Log($"[RubberDuckController] Started jump cycle on {gameObject.name}");
        }

        /// <summary>
        /// Stop the automatic jump cycle.
        /// </summary>
        [Button("Stop Jump Cycle"), FoldoutGroup("Debug")]
        public void StopJumpCycle()
        {
            if (_jumpCycleCoroutine != null)
            {
                StopCoroutine(_jumpCycleCoroutine);
                _jumpCycleCoroutine = null;
            }

            _isRunning = false;

            Debug.Log($"[RubberDuckController] Stopped jump cycle on {gameObject.name}");
        }

        /// <summary>
        /// Jump in a specific direction. Called from animation events.
        /// </summary>
        /// <param name="direction">Direction to jump (-1 for left, 1 for right, 0 for straight up)</param>
        public void Jump(float direction)
        {
            if (_rb == null)
            {
                Debug.LogError($"[RubberDuckController] No Rigidbody2D on {gameObject.name}");
                return;
            }

            // Normalize direction to -1, 0, or 1
            float normalizedDirection = direction > 0 ? 1f : (direction < 0 ? -1f : 0f);

            // Create jump vector (horizontal component based on direction, vertical is always up)
            Vector2 jumpVector = new Vector2(normalizedDirection, 1f).normalized * _jumpForce;

            // Apply force
            if (_additive)
            {
                _rb.AddForce(jumpVector, ForceMode2D.Impulse);
            }
            else
            {
                _rb.linearVelocity = jumpVector;
            }

            // Flip sprite if needed
            if (_spriteRenderer != null && normalizedDirection != 0f)
            {
                _spriteRenderer.flipX = normalizedDirection < 0f;
            }

            // Store debug info
            _lastJumpDirection = jumpVector;
            _totalJumps++;

            Debug.Log($"[RubberDuckController] Jumped in direction {normalizedDirection} on {gameObject.name}");
        }

        /// <summary>
        /// Jump in a random direction (-1, 0, or 1).
        /// </summary>
        [Button("Jump Random Direction"), FoldoutGroup("Debug")]
        public void JumpRandomDirection()
        {
            float randomDir = Random.Range(-1, 2); // -1, 0, or 1
            Jump(randomDir);
        }

        #endregion

        #region Coroutines

        private IEnumerator JumpCycleCoroutine()
        {
            while (true)
            {
                // Wait random interval
                float interval = Random.Range(_minJumpInterval, _maxJumpInterval);
                _nextJumpTime = Time.time + interval;
                yield return new WaitForSeconds(interval);

                // Trigger jump animation (actual jump happens via animation event)
                PlayJumpAnimation();
            }
        }

        #endregion

        #region Private Methods

        private void PlayJumpAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError($"[RubberDuckController] No Animator on {gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(_jumpAnimationName))
            {
                Debug.LogWarning($"[RubberDuckController] Jump animation name is empty on {gameObject.name}");
                return;
            }

            _animator.Play(_jumpAnimationName);
            Debug.Log($"[RubberDuckController] Playing jump animation on {gameObject.name}");
        }

        #endregion

        #region Debug Helpers

        [Button("Test Jump Left"), FoldoutGroup("Debug")]
        private void DebugJumpLeft()
        {
            Jump(-1f);
        }

        [Button("Test Jump Right"), FoldoutGroup("Debug")]
        private void DebugJumpRight()
        {
            Jump(1f);
        }

        [Button("Test Jump Straight"), FoldoutGroup("Debug")]
        private void DebugJumpStraight()
        {
            Jump(0f);
        }

        [Button("Log Stats"), FoldoutGroup("Debug")]
        private void DebugLogStats()
        {
            Debug.Log($"=== RUBBER DUCK STATS ({gameObject.name}) ===");
            Debug.Log($"Is Running: {_isRunning}");
            Debug.Log($"Total Jumps: {_totalJumps}");
            Debug.Log($"Last Jump Direction: {_lastJumpDirection}");
            Debug.Log($"Next Jump Time: {(_isRunning ? _nextJumpTime.ToString("F2") : "N/A")}");
            Debug.Log($"Jump Interval Range: {_minJumpInterval}s - {_maxJumpInterval}s");
            Debug.Log($"Jump Force: {_jumpForce}");
            Debug.Log($"Additive Force: {_additive}");
        }

        #endregion
    }
}