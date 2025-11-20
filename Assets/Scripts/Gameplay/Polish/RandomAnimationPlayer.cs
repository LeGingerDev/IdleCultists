using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LGD.Gameplay.Polish
{
    /// <summary>
    /// Plays a default animation, then periodically plays random weighted animations for visual polish.
    /// Uses Animator.Play() for 2D workflows - transitions back to idle are handled by Animator transitions.
    /// Perfect for idle variations, occasional special animations, or adding life to static objects.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RandomAnimationPlayer : SerializedMonoBehaviour
    {
        #region Animation Configuration

        [Serializable]
        public class WeightedAnimation
        {
            [Tooltip("The name of the animation state in the Animator")]
            public string animationName;

            [Tooltip("Weight for this animation (higher = more likely to be selected)")]
            [MinValue(0)]
            public int weight = 1;

            [Tooltip("Optional: specific duration for this animation (0 = use default interval)")]
            [MinValue(0)]
            public float overrideDuration = 0f;

            public WeightedAnimation(string animName, int weight = 1)
            {
                this.animationName = animName;
                this.weight = weight;
            }
        }

        #endregion

        #region Inspector Fields

        [SerializeField, FoldoutGroup("Animator Reference")]
        [Tooltip("Reference to the Animator component (auto-assigned if null)")]
        private Animator _animator;

        [SerializeField, FoldoutGroup("Default Animation")]
        [InfoBox("The animation state that plays when the component starts (leave empty to skip)")]
        [Tooltip("State name for the default/idle animation")]
        private string _defaultAnimationName = "Idle";

        [SerializeField, FoldoutGroup("Default Animation")]
        [Tooltip("Should the default animation be played on Start?")]
        private bool _playDefaultOnStart = true;

        [SerializeField, FoldoutGroup("Random Animations")]
        [InfoBox("List of animations that can be randomly played. Higher weight = more likely to play.\nTransitions back to idle are handled by Animator transitions.")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "animationName")]
        private List<WeightedAnimation> _randomAnimations = new List<WeightedAnimation>()
        {
            new WeightedAnimation("Special1", 5),
            new WeightedAnimation("Special2", 3),
            new WeightedAnimation("Rare", 1)
        };

        [SerializeField, FoldoutGroup("Timing Settings")]
        [Tooltip("Minimum time in seconds between random animations")]
        [MinValue(0.1f)]
        private float _minInterval = 5f;

        [SerializeField, FoldoutGroup("Timing Settings")]
        [Tooltip("Maximum time in seconds between random animations")]
        [MinValue(0.1f)]
        private float _maxInterval = 15f;

        [SerializeField, FoldoutGroup("Timing Settings")]
        [Tooltip("If true, starts playing random animations automatically on enable")]
        private bool _autoStart = true;

        [SerializeField, FoldoutGroup("Timing Settings")]
        [Tooltip("If true, delays the first random animation. If false, plays one immediately.")]
        private bool _delayFirstAnimation = true;

        #endregion

        #region Runtime State

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private bool _isPlaying;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private float _nextAnimationTime;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private string _lastAnimationPlayed;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        private int _totalAnimationsPlayed;

        [SerializeField, ReadOnly, FoldoutGroup("Debug")]
        [ShowInInspector]
        private int TotalWeight => _randomAnimations?.Sum(a => a.weight) ?? 0;

        private Coroutine _randomAnimationCoroutine;
        private int _totalWeightCache = 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Auto-assign animator if not set
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            // Cache total weight
            RecalculateTotalWeight();
        }

        private void Start()
        {
            // Play default animation on start
            if (_playDefaultOnStart && !string.IsNullOrEmpty(_defaultAnimationName))
            {
                PlayDefaultAnimation();
            }
        }

        private void OnEnable()
        {
            if (_autoStart)
            {
                StartRandomAnimations();
            }
        }

        private void OnDisable()
        {
            StopRandomAnimations();
        }

        private void OnValidate()
        {
            // Ensure max is always >= min
            if (_maxInterval < _minInterval)
            {
                _maxInterval = _minInterval;
            }

            // Recalculate weight when list changes
            RecalculateTotalWeight();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start playing random animations at intervals.
        /// </summary>
        [Button("Start Random Animations"), FoldoutGroup("Debug")]
        public void StartRandomAnimations()
        {
            if (_isPlaying)
            {
                Debug.LogWarning($"[RandomAnimationPlayer] Already playing on {gameObject.name}");
                return;
            }

            if (_animator == null)
            {
                Debug.LogError($"[RandomAnimationPlayer] No Animator assigned on {gameObject.name}");
                return;
            }

            if (_randomAnimations == null || _randomAnimations.Count == 0)
            {
                Debug.LogError($"[RandomAnimationPlayer] No random animations configured on {gameObject.name}");
                return;
            }

            if (_randomAnimationCoroutine != null)
                StopCoroutine(_randomAnimationCoroutine);

            _randomAnimationCoroutine = StartCoroutine(RandomAnimationCoroutine());
            _isPlaying = true;

            Debug.Log($"[RandomAnimationPlayer] Started random animations on {gameObject.name}");
        }

        /// <summary>
        /// Stop playing random animations.
        /// </summary>
        [Button("Stop Random Animations"), FoldoutGroup("Debug")]
        public void StopRandomAnimations()
        {
            if (_randomAnimationCoroutine != null)
            {
                StopCoroutine(_randomAnimationCoroutine);
                _randomAnimationCoroutine = null;
            }

            _isPlaying = false;

            Debug.Log($"[RandomAnimationPlayer] Stopped random animations on {gameObject.name}");
        }

        /// <summary>
        /// Play the default animation immediately.
        /// </summary>
        [Button("Play Default Animation"), FoldoutGroup("Debug")]
        public void PlayDefaultAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError($"[RandomAnimationPlayer] No Animator assigned on {gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(_defaultAnimationName))
            {
                Debug.LogWarning($"[RandomAnimationPlayer] No default animation state set on {gameObject.name}");
                return;
            }

            _animator.Play(_defaultAnimationName);
            _lastAnimationPlayed = _defaultAnimationName;

            Debug.Log($"[RandomAnimationPlayer] Playing default animation '{_defaultAnimationName}' on {gameObject.name}");
        }

        /// <summary>
        /// Immediately trigger a random weighted animation.
        /// </summary>
        [Button("Play Random Animation Now"), FoldoutGroup("Debug")]
        public void PlayRandomAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError($"[RandomAnimationPlayer] No Animator assigned on {gameObject.name}");
                return;
            }

            WeightedAnimation selected = SelectRandomAnimation();
            if (selected != null)
            {
                PlayAnimation(selected);
            }
        }

        /// <summary>
        /// Play a specific animation by state name.
        /// </summary>
        /// <param name="animationName">The animation state name to play</param>
        public void PlaySpecificAnimation(string animationName)
        {
            if (_animator == null)
            {
                Debug.LogError($"[RandomAnimationPlayer] No Animator assigned on {gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(animationName))
            {
                Debug.LogWarning($"[RandomAnimationPlayer] Attempted to play animation with empty state name on {gameObject.name}");
                return;
            }

            _animator.Play(animationName);
            _lastAnimationPlayed = animationName;
            _totalAnimationsPlayed++;

            Debug.Log($"[RandomAnimationPlayer] Playing specific animation '{animationName}' on {gameObject.name}");
        }

        /// <summary>
        /// Add a new weighted animation at runtime.
        /// </summary>
        /// <param name="animationName">Animation state name</param>
        /// <param name="weight">Weight for selection</param>
        public void AddAnimation(string animationName, int weight = 1)
        {
            if (string.IsNullOrEmpty(animationName))
                return;

            if (_randomAnimations == null)
                _randomAnimations = new List<WeightedAnimation>();

            _randomAnimations.Add(new WeightedAnimation(animationName, weight));
            RecalculateTotalWeight();

            Debug.Log($"[RandomAnimationPlayer] Added animation '{animationName}' (weight: {weight}) to {gameObject.name}");
        }

        /// <summary>
        /// Remove an animation by state name.
        /// </summary>
        /// <param name="animationName">Animation state name to remove</param>
        public void RemoveAnimation(string animationName)
        {
            if (_randomAnimations == null)
                return;

            int removed = _randomAnimations.RemoveAll(a => a.animationName == animationName);
            if (removed > 0)
            {
                RecalculateTotalWeight();
                Debug.Log($"[RandomAnimationPlayer] Removed {removed} animation(s) with state '{animationName}' from {gameObject.name}");
            }
        }

        /// <summary>
        /// Clear all random animations.
        /// </summary>
        public void ClearAnimations()
        {
            if (_randomAnimations == null)
                _randomAnimations = new List<WeightedAnimation>();
            else
                _randomAnimations.Clear();

            RecalculateTotalWeight();

            Debug.Log($"[RandomAnimationPlayer] Cleared all animations on {gameObject.name}");
        }

        #endregion

        #region Coroutines

        private IEnumerator RandomAnimationCoroutine()
        {
            // Optional delay before first animation
            if (_delayFirstAnimation)
            {
                float initialDelay = UnityEngine.Random.Range(_minInterval * 0.5f, _minInterval);
                _nextAnimationTime = Time.time + initialDelay;
                yield return new WaitForSeconds(initialDelay);
            }

            // Main loop
            while (true)
            {
                // Select and play random animation
                WeightedAnimation selected = SelectRandomAnimation();
                if (selected != null)
                {
                    PlayAnimation(selected);
                }

                // Wait random interval before next animation
                float interval = UnityEngine.Random.Range(_minInterval, _maxInterval);
                _nextAnimationTime = Time.time + interval;
                yield return new WaitForSeconds(interval);
            }
        }

        #endregion

        #region Animation Logic

        private WeightedAnimation SelectRandomAnimation()
        {
            if (_randomAnimations == null || _randomAnimations.Count == 0)
                return null;

            if (_totalWeightCache <= 0)
            {
                Debug.LogWarning($"[RandomAnimationPlayer] Total weight is 0 on {gameObject.name}, selecting first animation");
                return _randomAnimations[0];
            }

            // Weighted random selection
            int randomValue = UnityEngine.Random.Range(0, _totalWeightCache);
            int cumulativeWeight = 0;

            foreach (var animation in _randomAnimations)
            {
                cumulativeWeight += animation.weight;
                if (randomValue < cumulativeWeight)
                {
                    return animation;
                }
            }

            // Fallback (shouldn't happen, but just in case)
            return _randomAnimations[_randomAnimations.Count - 1];
        }

        private void PlayAnimation(WeightedAnimation animation)
        {
            if (_animator == null || animation == null)
                return;

            _animator.Play(animation.animationName);
            _lastAnimationPlayed = animation.animationName;
            _totalAnimationsPlayed++;

            Debug.Log($"[RandomAnimationPlayer] Playing animation '{animation.animationName}' (weight: {animation.weight}) on {gameObject.name}");
        }

        private void RecalculateTotalWeight()
        {
            if (_randomAnimations == null || _randomAnimations.Count == 0)
            {
                _totalWeightCache = 0;
                return;
            }

            _totalWeightCache = _randomAnimations.Sum(a => a.weight);
        }

        #endregion

        #region Debug Helpers

        [Button("Test All Animations (Sequential)"), FoldoutGroup("Debug")]
        private void DebugTestAllAnimations()
        {
            if (_randomAnimations == null || _randomAnimations.Count == 0)
            {
                Debug.LogWarning($"[RandomAnimationPlayer] No animations to test on {gameObject.name}");
                return;
            }

            StartCoroutine(DebugTestAllAnimationsCoroutine());
        }

        private IEnumerator DebugTestAllAnimationsCoroutine()
        {
            bool wasPlaying = _isPlaying;
            if (wasPlaying)
                StopRandomAnimations();

            Debug.Log($"[RandomAnimationPlayer] Testing all {_randomAnimations.Count} animations sequentially...");

            // Test default animation first
            if (!string.IsNullOrEmpty(_defaultAnimationName))
            {
                Debug.Log($"[RandomAnimationPlayer] Testing default: {_defaultAnimationName}");
                PlayDefaultAnimation();
                yield return new WaitForSeconds(2f);
            }

            // Test each random animation
            foreach (var animation in _randomAnimations)
            {
                Debug.Log($"[RandomAnimationPlayer] Testing: {animation.animationName} (weight: {animation.weight})");
                PlayAnimation(animation);
                yield return new WaitForSeconds(2f);
            }

            Debug.Log($"[RandomAnimationPlayer] Finished testing all animations");

            if (wasPlaying)
                StartRandomAnimations();
        }

        [Button("Log Stats"), FoldoutGroup("Debug")]
        private void DebugLogStats()
        {
            Debug.Log($"=== RANDOM ANIMATION PLAYER STATS ({gameObject.name}) ===");
            Debug.Log($"Is Playing: {_isPlaying}");
            Debug.Log($"Default Animation: {_defaultAnimationName}");
            Debug.Log($"Total Random Animations: {(_randomAnimations != null ? _randomAnimations.Count : 0)}");
            Debug.Log($"Total Weight: {_totalWeightCache}");
            Debug.Log($"Total Animations Played: {_totalAnimationsPlayed}");
            Debug.Log($"Last Animation: '{_lastAnimationPlayed}'");
            Debug.Log($"Next Animation Time: {(_isPlaying ? _nextAnimationTime.ToString("F2") : "N/A")}");
            Debug.Log($"Interval Range: {_minInterval}s - {_maxInterval}s");

            if (_randomAnimations != null && _randomAnimations.Count > 0)
            {
                Debug.Log("--- Animation Probabilities ---");
                foreach (var animation in _randomAnimations)
                {
                    float probability = (_totalWeightCache > 0) ? (animation.weight / (float)_totalWeightCache * 100f) : 0f;
                    Debug.Log($"  {animation.animationName}: {probability:F1}% (weight: {animation.weight})");
                }
            }
        }

        [Button("Log Animator Parameters"), FoldoutGroup("Debug")]
        private void DebugLogAnimatorParameters()
        {
            if (_animator == null)
            {
                Debug.LogWarning($"[RandomAnimationPlayer] No Animator on {gameObject.name}");
                return;
            }

            Debug.Log($"=== ANIMATOR PARAMETERS ({gameObject.name}) ===");
            foreach (var param in _animator.parameters)
            {
                Debug.Log($"  {param.name} ({param.type})");
            }
        }

        #endregion
    }
}
