using UnityEngine;
using ScoredProductions.StreamLinked.EventSub;
using ScoredProductions.StreamLinked.EventSub.Events;
using ScoredProductions.StreamLinked.API;
using ScoredProductions.StreamLinked.API.Channel_Points;
using ScoredProductions.StreamLinked.API.Users;
using System.Collections;
using Sirenix.OdinInspector;
using ScoredProductions.StreamLinked.EventSub.Events.Objects;

namespace YourNamespace.Twitch
{
    /// <summary>
    /// Base class for listening to Twitch Channel Point reward redemptions.
    /// Extend this to create specific reward handlers.
    /// Waits for TwitchRewardManager to initialize before subscribing.
    /// </summary>
    public abstract class TwitchRewardListener : MonoBehaviour
    {
        // NOTE: The event listener below should be converted to use your Topic System

        [SerializeField, FoldoutGroup("Settings")]
        protected string _targetRewardId;

        [SerializeField, FoldoutGroup("Settings")]
        protected bool _requireExactRewardId = true;

        [SerializeField, FoldoutGroup("Debug"), ReadOnly]
        private bool _isSubscribed = false;

        protected TwitchRewardManager _manager;
        protected TwitchEventSubClient _eventSubClient;

        protected virtual void OnEnable()
        {
            // If already initialized, re-register listener
            if (_eventSubClient != null)
            {
                _eventSubClient.OnChannelPointsCustomRewardRedemptionAdd.RemoveListener(OnRedemptionReceived);
                _eventSubClient.OnChannelPointsCustomRewardRedemptionAdd.AddListener(OnRedemptionReceived);
            }
        }

        protected virtual void OnDisable()
        {
            if (_eventSubClient != null)
            {
                _eventSubClient.OnChannelPointsCustomRewardRedemptionAdd.RemoveListener(OnRedemptionReceived);
            }
        }

        /// <summary>
        /// Called by TwitchRewardManager when connection is established
        /// </summary>
        /// <summary>
        /// Called by TwitchRewardManager when connection is established
        /// </summary>
        public void Initialize(TwitchRewardManager manager)
        {
            _manager = manager;
            _eventSubClient = manager.GetEventSubClient();

            // Register the event listener now that we have the client reference
            if (_eventSubClient != null)
            {
                _eventSubClient.OnChannelPointsCustomRewardRedemptionAdd.AddListener(OnRedemptionReceived);
            }

            StartCoroutine(SubscribeToRewardCoroutine());
        }

        private IEnumerator SubscribeToRewardCoroutine()
        {
            if (string.IsNullOrEmpty(_targetRewardId))
            {
                Debug.LogError($"[{gameObject.name}] Target Reward ID is not set!", this);
                yield break;
            }

            Debug.Log($"[{gameObject.name}] Subscribing to reward: {_targetRewardId}");

            _eventSubClient.SubscribeToEvent<ChannelPointsCustomRewardRedemptionAdd>(
                new Condition()
                {
                    broadcaster_user_id = _manager.UserId,
                    reward_id = _targetRewardId,
                },
                APIScopeWarning.None
            ).ConfigureAwait(false);

            _isSubscribed = true;
            Debug.Log($"[{gameObject.name}] Successfully subscribed to reward redemptions!");
        }

        private void OnRedemptionReceived(ChannelPointsCustomRewardRedemptionAdd redemption)
        {
            if (!IsValidRedemption(redemption))
            {
                return;
            }

            Debug.Log($"[{gameObject.name}] Redemption received from {redemption.user_name}!");

            StartCoroutine(ProcessRedemptionCoroutine(redemption));
        }

        protected virtual bool IsValidRedemption(ChannelPointsCustomRewardRedemptionAdd redemption)
        {
            if (!_requireExactRewardId)
            {
                return true;
            }

            return redemption.reward.id == _targetRewardId;
        }

        /// <summary>
        /// Override this to implement your reward handling logic
        /// </summary>
        protected abstract IEnumerator ProcessRedemptionCoroutine(ChannelPointsCustomRewardRedemptionAdd redemption);

        #region Debug Tools

        [Button("Log All Channel Rewards", ButtonSizes.Medium), FoldoutGroup("Debug")]
        private void LogAllRewards()
        {
            if (_eventSubClient == null && _manager != null)
            {
                _eventSubClient = _manager.GetEventSubClient();
            }

            if (_eventSubClient == null)
            {
                Debug.LogError("EventSubClient reference is not available!");
                return;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("This can only be used in Play Mode!");
                return;
            }

            StartCoroutine(LogAllRewardsCoroutine());
        }

        private IEnumerator LogAllRewardsCoroutine()
        {
            Debug.Log("Fetching all channel rewards...");

            // Get the broadcaster's user ID
            IEnumerator getUsersRequest = TwitchAPIClient.MakeTwitchAPIRequest<GetUsers>(_eventSubClient.EventSubToken);
            yield return getUsersRequest;
            TwitchAPIDataContainer<GetUsers> userData = (TwitchAPIDataContainer<GetUsers>)getUsersRequest.Current;

            if (userData.HasErrored)
            {
                Debug.LogError($"Failed to get user data: {userData.ErrorText}");
                yield break;
            }

            string userId = userData.data[0].id;
            string userName = userData.data[0].display_name;

            // Get all custom rewards for this channel
            IEnumerator getRewardsRequest = TwitchAPIClient.MakeTwitchAPIRequest<GetCustomReward>(
                _eventSubClient.EventSubToken,
                QueryParameters: new (string, string)[] { (GetCustomReward.BROADCASTER_ID, userId) },
                ScopeSettings: APIScopeWarning.None
            );
            yield return getRewardsRequest;
            TwitchAPIDataContainer<GetCustomReward> rewardsData = (TwitchAPIDataContainer<GetCustomReward>)getRewardsRequest.Current;

            if (rewardsData.HasErrored)
            {
                Debug.LogError($"Failed to get rewards: {rewardsData.ErrorText}");
                yield break;
            }

            Debug.Log($"=== Channel Rewards for {userName} ===");
            Debug.Log($"Found {rewardsData.data.Length} reward(s):\n");

            for (int i = 0; i < rewardsData.data.Length; i++)
            {
                GetCustomReward reward = rewardsData.data[i];
                Debug.Log($"[{i + 1}] {reward.title}\n" +
                         $"    ID: {reward.id}\n" +
                         $"    Cost: {reward.cost}\n" +
                         $"    Enabled: {reward.is_enabled}\n" +
                         $"    Paused: {reward.is_paused}\n");
            }

            Debug.Log("=== End of Rewards List ===");
        }

        #endregion
    }
}