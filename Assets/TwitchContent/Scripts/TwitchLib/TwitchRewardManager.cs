using UnityEngine;
using ScoredProductions.StreamLinked.EventSub;
using ScoredProductions.StreamLinked.API;
using ScoredProductions.StreamLinked.API.Users;
using System.Collections;
using Sirenix.OdinInspector;

namespace YourNamespace.Twitch
{
    /// <summary>
    /// Manages Twitch EventSub connection and initializes all reward listeners
    /// </summary>
    public class TwitchRewardManager : MonoBehaviour
    {
        [SerializeField, FoldoutGroup("References")]
        private TwitchEventSubClient _eventSubClient;

        [SerializeField, FoldoutGroup("Settings")]
        private bool _initializeOnStart = true;

        [SerializeField, FoldoutGroup("Debug"), ReadOnly]
        private bool _isInitialized = false;

        [SerializeField, FoldoutGroup("Debug"), ReadOnly]
        private string _userId;

        public bool IsInitialized => _isInitialized;
        public string UserId => _userId;

        private void Start()
        {
            if (_initializeOnStart)
            {
                StartCoroutine(InitializeConnectionCoroutine());
            }
        }

        private IEnumerator InitializeConnectionCoroutine()
        {
            if (_eventSubClient == null)
            {
                Debug.LogError("[TwitchRewardManager] EventSubClient reference is not set!", this);
                yield break;
            }

            Debug.Log("[TwitchRewardManager] Initializing Twitch EventSub connection...");

            // Get the broadcaster's user ID
            IEnumerator getUsersRequest = TwitchAPIClient.MakeTwitchAPIRequest<GetUsers>(_eventSubClient.EventSubToken);
            yield return getUsersRequest;
            TwitchAPIDataContainer<GetUsers> userData = (TwitchAPIDataContainer<GetUsers>)getUsersRequest.Current;

            if (userData.HasErrored)
            {
                Debug.LogError($"[TwitchRewardManager] Failed to get user data: {userData.ErrorText}", this);
                yield break;
            }

            _userId = userData.data[0].id;
            Debug.Log($"[TwitchRewardManager] User ID: {_userId}");

            // Establish EventSub connection
            Debug.Log("[TwitchRewardManager] Establishing EventSub connection...");
            yield return _eventSubClient.BeginConnectionSession();

            _isInitialized = true;
            Debug.Log("[TwitchRewardManager] Connection established! Notifying reward listeners...");

            // TODO: Replace this with your Topic System
            // TopicSystem.Publish("TwitchRewardManagerInitialized", this);

            // Notify all listeners to initialize
            NotifyAllListeners();
        }

        private void NotifyAllListeners()
        {
            TwitchRewardListener[] listeners = FindObjectsOfType<TwitchRewardListener>();

            Debug.Log($"[TwitchRewardManager] Found {listeners.Length} reward listener(s)");

            foreach (TwitchRewardListener listener in listeners)
            {
                listener.Initialize(this);
            }
        }

        public TwitchEventSubClient GetEventSubClient()
        {
            return _eventSubClient;
        }

        #region Debug Tools

        [Button("Manual Initialize", ButtonSizes.Medium), FoldoutGroup("Debug")]
        private void ManualInitialize()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("This can only be used in Play Mode!");
                return;
            }

            StartCoroutine(InitializeConnectionCoroutine());
        }

        #endregion
    }
}