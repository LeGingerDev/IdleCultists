using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace LGD.Gameplay.Polish
{
    /// <summary>
    /// Simple isopod crawler that alternates between idle and crawling left/right.
    /// No jumping, no complexity - just cute crawling action.
    /// </summary>
    public class IsopodCrawlerController : SerializedMonoBehaviour
    {
        #region State Enum

        private enum CrawlerState
        {
            Idle,
            Crawling
        }

        #endregion

        #region Inspector Fields

        [SerializeField, FoldoutGroup("References")]
        [Tooltip("Animator component (auto-assigned if null)")]
        private Animator _animator;

        [SerializeField, FoldoutGroup("References")]
        [Tooltip("SpriteRenderer to flip based on movement direction")]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, FoldoutGroup("Animation Settings")]
        [Tooltip("Name of the idle animation state")]
        private string _idleAnimationName = "Idle";

        [SerializeField, FoldoutGroup("Animation Settings")]
        [Tooltip("Name of the crawl animation state")]
        private string _crawlAnimationName = "Crawl";

        [SerializeField, FoldoutGroup("Movement Settings")]
        [Tooltip("Speed when crawling")]
        [MinValue(0f)]
        private float _crawlSpeed = 1f;

        [SerializeField, FoldoutGroup("State Timing")]
        [Tooltip("Minimum seconds to stay idle")]
        [MinValue(0.1f)]
        private float _minIdleTime = 2f;

        [SerializeField, FoldoutGroup("State Timing")]
        [Tooltip("Maximum seconds to stay idle")]
        [MinValue(0.1f)]
        private float _maxIdleTime = 5f;

        [SerializeField, FoldoutGroup("State Timing")]
        [Tooltip("Minimum seconds to crawl")]
        [MinValue(0.1f)]
        private float _minCrawlTime = 1f;

        [SerializeField, FoldoutGroup("State Timing")]
        [Tooltip("Maximum seconds to crawl")]
        [MinValue(0.1f)]
        private float _maxCrawlTime = 3f;

        [SerializeField, FoldoutGroup("Settings")]
        [Tooltip("Auto-start behavior on enable")]
        private bool _autoStart = true;

        [SerializeField, FoldoutGroup("Settings")]
        [Tooltip("Play idle animation on start")]
        private bool _playIdleOnStart = true;

        #endregion

        #region Runtime State

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private CrawlerState _currentState = CrawlerState.Idle;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private bool _isRunning;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private float _stateChangeTime;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private float _currentDirection; // -1 for left, 1 for right

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private int _totalStateChanges;

        private Coroutine _behaviorCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Auto-assign components if not set
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Validate
            if (_animator == null)
                Debug.LogError($"[IsopodCrawlerController] No Animator found on {gameObject.name}!");

            if (_spriteRenderer == null)
                Debug.LogWarning($"[IsopodCrawlerController] No SpriteRenderer assigned on {gameObject.name}. Sprite flipping will not work.");
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
                StartBehavior();
            }
        }

        private void OnDisable()
        {
            StopBehavior();
        }

        private void Update()
        {
            // Move if crawling
            if (_currentState == CrawlerState.Crawling)
            {
                transform.position += new Vector3(_currentDirection * _crawlSpeed * Time.deltaTime, 0f, 0f);
            }
        }

        private void OnValidate()
        {
            // Ensure max is always >= min
            if (_maxIdleTime < _minIdleTime)
                _maxIdleTime = _minIdleTime;

            if (_maxCrawlTime < _minCrawlTime)
                _maxCrawlTime = _minCrawlTime;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start the automatic behavior cycle.
        /// </summary>
        [Button("Start Behavior"), FoldoutGroup("Debug")]
        public void StartBehavior()
        {
            if (_isRunning)
            {
                Debug.LogWarning($"[IsopodCrawlerController] Behavior already running on {gameObject.name}");
                return;
            }

            if (_behaviorCoroutine != null)
                StopCoroutine(_behaviorCoroutine);

            _behaviorCoroutine = StartCoroutine(BehaviorCoroutine());
            _isRunning = true;

            Debug.Log($"[IsopodCrawlerController] Started behavior on {gameObject.name}");
        }

        /// <summary>
        /// Stop the automatic behavior cycle.
        /// </summary>
        [Button("Stop Behavior"), FoldoutGroup("Debug")]
        public void StopBehavior()
        {
            if (_behaviorCoroutine != null)
            {
                StopCoroutine(_behaviorCoroutine);
                _behaviorCoroutine = null;
            }

            _isRunning = false;
            SetState(CrawlerState.Idle);

            Debug.Log($"[IsopodCrawlerController] Stopped behavior on {gameObject.name}");
        }

        /// <summary>
        /// Force the isopod to idle.
        /// </summary>
        [Button("Force Idle"), FoldoutGroup("Debug")]
        public void ForceIdle()
        {
            SetState(CrawlerState.Idle);
        }

        /// <summary>
        /// Force the isopod to crawl in a direction.
        /// </summary>
        /// <param name="direction">-1 for left, 1 for right</param>
        [Button("Force Crawl"), FoldoutGroup("Debug")]
        public void ForceCrawl(float direction = 1f)
        {
            _currentDirection = direction > 0 ? 1f : -1f;
            SetState(CrawlerState.Crawling);
        }

        #endregion

        #region Coroutines

        private IEnumerator BehaviorCoroutine()
        {
            // Start idle
            SetState(CrawlerState.Idle);

            while (true)
            {
                if (_currentState == CrawlerState.Idle)
                {
                    // Wait idle duration
                    float idleDuration = Random.Range(_minIdleTime, _maxIdleTime);
                    _stateChangeTime = Time.time + idleDuration;
                    yield return new WaitForSeconds(idleDuration);

                    // Switch to crawling with random direction
                    _currentDirection = Random.value > 0.5f ? 1f : -1f;
                    SetState(CrawlerState.Crawling);
                }
                else // Crawling
                {
                    // Wait crawl duration
                    float crawlDuration = Random.Range(_minCrawlTime, _maxCrawlTime);
                    _stateChangeTime = Time.time + crawlDuration;
                    yield return new WaitForSeconds(crawlDuration);

                    // Switch back to idle
                    SetState(CrawlerState.Idle);
                }
            }
        }

        #endregion

        #region State Management

        private void SetState(CrawlerState newState)
        {
            if (_currentState == newState)
                return;

            _currentState = newState;
            _totalStateChanges++;

            switch (newState)
            {
                case CrawlerState.Idle:
                    PlayIdleAnimation();
                    break;

                case CrawlerState.Crawling:
                    PlayCrawlAnimation();
                    UpdateSpriteFlip();
                    break;
            }

            Debug.Log($"[IsopodCrawlerController] State changed to {newState} on {gameObject.name}");
        }

        private void PlayIdleAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError($"[IsopodCrawlerController] No Animator on {gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(_idleAnimationName))
            {
                Debug.LogWarning($"[IsopodCrawlerController] Idle animation name is empty on {gameObject.name}");
                return;
            }

            _animator.Play(_idleAnimationName);
        }

        private void PlayCrawlAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError($"[IsopodCrawlerController] No Animator on {gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(_crawlAnimationName))
            {
                Debug.LogWarning($"[IsopodCrawlerController] Crawl animation name is empty on {gameObject.name}");
                return;
            }

            _animator.Play(_crawlAnimationName);
        }

        private void UpdateSpriteFlip()
        {
            if (_spriteRenderer == null)
                return;

            _spriteRenderer.flipX = _currentDirection < 0f;
        }

        #endregion

        #region Debug Helpers

        [Button("Crawl Left"), FoldoutGroup("Debug")]
        private void DebugCrawlLeft()
        {
            ForceCrawl(-1f);
        }

        [Button("Crawl Right"), FoldoutGroup("Debug")]
        private void DebugCrawlRight()
        {
            ForceCrawl(1f);
        }

        [Button("Log Stats"), FoldoutGroup("Debug")]
        private void DebugLogStats()
        {
            Debug.Log($"=== ISOPOD CRAWLER STATS ({gameObject.name}) ===");
            Debug.Log($"Is Running: {_isRunning}");
            Debug.Log($"Current State: {_currentState}");
            Debug.Log($"Current Direction: {(_currentDirection > 0 ? "Right" : "Left")} ({_currentDirection})");
            Debug.Log($"Total State Changes: {_totalStateChanges}");
            Debug.Log($"Next State Change: {(_isRunning ? _stateChangeTime.ToString("F2") : "N/A")}");
            Debug.Log($"Crawl Speed: {_crawlSpeed}");
            Debug.Log($"Idle Time Range: {_minIdleTime}s - {_maxIdleTime}s");
            Debug.Log($"Crawl Time Range: {_minCrawlTime}s - {_maxCrawlTime}s");
        }

        #endregion
    }
}
