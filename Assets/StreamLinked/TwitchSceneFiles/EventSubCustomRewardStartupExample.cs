using System.Collections;
using System.Threading;

using ScoredProductions.StreamLinked.API;
using ScoredProductions.StreamLinked.API.Channel_Points;
using ScoredProductions.StreamLinked.API.Users;
using ScoredProductions.StreamLinked.EventSub;
using ScoredProductions.StreamLinked.EventSub.Events;
using ScoredProductions.StreamLinked.EventSub.Events.Objects;
using ScoredProductions.StreamLinked.LightJson.Serialization;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;
using UnityEngine.UI;

namespace ScoredProductions.StreamLinked.TwitchSceneFiles {

	/// <summary>
	/// Simple implementation object of starting the Twitch EventSub and subscribing to the OAuth channels Poll related subscriptions.
	/// </summary>
	public class EventSubCustomRewardStartupExample : MonoBehaviour {

		public const string DefaultTitle = "Custom Reward";
		public const string DefaultColour = "#9147FF";

		public Text OutputText;
		public string RewardTitle = DefaultTitle;
		public int RewardCost = 1;
		public string RewardColour = DefaultColour;
		public bool UserInputRequired = false;
		[Tooltip("Text provided if UserInputRequired is true")]
		public string RewardPrompt = "This Example was provided by StreamLinked";

		public string RewardId { get; private set; }

		private string UserID;
		public GetUsers UsernameData { get; private set; }

		private TwitchEventSubClient referenceEventSub;

		private CancellationTokenSource cts = new CancellationTokenSource();

		private void Awake() {
			if (!TwitchAPIClient.CreateOrGetInstance(out _) | !TwitchEventSubClient.CreateOrGetInstance(out this.referenceEventSub)) {
				DebugManager.LogMessage("TwitchEventSubInterface requires TwitchAPIClient and TwitchEventSubClient to be in the scene to work.".RichTextColour("red"));
				this.gameObject.SetActive(false);
			}
		}

		private void Start() {
			if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchCustomReward, out string value)) {
				this.RewardId = value;
			}
		}

		private void OnEnable() {
			this.cts ??= new CancellationTokenSource();

			this.referenceEventSub.OnChannelPointsCustomRewardRedemptionAdd.AddListener(this.OnRewardRedemption);
		}

		private void OnDisable() {
			this.cts?.Cancel();
			this.cts?.Dispose();

			this.referenceEventSub.OnChannelPointsCustomRewardRedemptionAdd.RemoveListener(this.OnRewardRedemption);

			//if (!string.IsNullOrWhiteSpace(this.RewardId) && !string.IsNullOrWhiteSpace(this.UserID)) {
			//	this.RemoveGeneratedReward().ConfigureAwait(false);
			//}
		}

		public void BuildUserSubscriptionsForUser() {
			this.StartCoroutine(this.BuildUserSubscriptionsForUserCoroutine());
		}

		private IEnumerator BuildUserSubscriptionsForUserCoroutine() {
			if (string.IsNullOrWhiteSpace(this.RewardTitle)) {
				this.RewardTitle = DefaultTitle;
			}
			else {
				this.RewardTitle = this.RewardTitle.Trim();
			}

			if (this.RewardCost < 1) {
				this.RewardCost = 1;
			}

			if (string.IsNullOrWhiteSpace(this.RewardColour) || !ColorUtility.TryParseHtmlString(this.RewardColour, out _)) {
				this.RewardColour = DefaultColour;
			}

			if (this.cts.IsCancellationRequested) {
				yield break;
			}


			IEnumerator getUsersRequest = TwitchAPIClient.MakeTwitchAPIRequest<GetUsers>(this.referenceEventSub.EventSubToken);
			yield return getUsersRequest;
			TwitchAPIDataContainer<GetUsers> returnedUser = (TwitchAPIDataContainer<GetUsers>)getUsersRequest.Current;

			if (!returnedUser.HasErrored) {
				this.UsernameData = returnedUser.data[0];
				this.UserID = this.UsernameData.id;
			}
			else {
				DebugManager.LogMessage($"GetUsers API call failed to get User associated with OAuth. Error: {returnedUser.ErrorText}");
				yield break;
			}

			if (this.cts.IsCancellationRequested) {
				yield break;
			}

			yield return this.referenceEventSub.BeginConnectionSession();

			IEnumerator getRewardRequest = TwitchAPIClient.MakeTwitchAPIRequest<GetCustomReward>(this.referenceEventSub.EventSubToken,
				QueryParameters: new (string, string)[] { (GetCustomReward.BROADCASTER_ID, this.UserID) }, ScopeSettings: APIScopeWarning.None);
			yield return getRewardRequest;
			TwitchAPIDataContainer<GetCustomReward> data = (TwitchAPIDataContainer<GetCustomReward>)getRewardRequest.Current;

			if (!data.HasErrored) {
				bool rewardExists = false;
				for (int x = 0; x < data.data.Length; x++) {
					GetCustomReward reward = data.data[x];
					if (reward.id == this.RewardId) {
						rewardExists = true;
						break;
					}
				}

				if (rewardExists) {
					IEnumerator updateRequest = TwitchAPIClient.MakeTwitchAPIRequest<UpdateCustomReward>
								(this.referenceEventSub.EventSubToken,
								QueryParameters: new (string, string)[] { (UpdateCustomReward.BROADCASTER_ID, this.UserID), (UpdateCustomReward.ID, this.RewardId) },
								Body: UpdateCustomReward.BuildDataJson(
									this.RewardTitle,
									this.UserInputRequired ? this.RewardPrompt : null,
									this.RewardCost,
									background_color: this.RewardColour,
									is_user_input_required: this.UserInputRequired),
								ScopeSettings: APIScopeWarning.None);
					yield return updateRequest;
					TwitchAPIDataContainer<CreateCustomRewards> UpdatedReward = (TwitchAPIDataContainer<CreateCustomRewards>)updateRequest.Current;

					if (!UpdatedReward.HasErrored) {
						this.RewardId = UpdatedReward.data[0].id;
					}
				}
				else {
					this.RewardId = null;
					IEnumerator createRequest = TwitchAPIClient.MakeTwitchAPIRequest<CreateCustomRewards>
								(this.referenceEventSub.EventSubToken,
								QueryParameters: new (string, string)[] { (CreateCustomRewards.BROADCASTER_ID, this.UserID) },
								Body: CreateCustomRewards.BuildDataJson(
									this.RewardTitle,
									this.RewardCost,
									this.UserInputRequired ? this.RewardPrompt : null,
									background_color: this.RewardColour,
									is_user_input_required: this.UserInputRequired),
								ScopeSettings: APIScopeWarning.None);
					yield return createRequest;
					TwitchAPIDataContainer<CreateCustomRewards> NewReward = (TwitchAPIDataContainer<CreateCustomRewards>)createRequest.Current;

					if (!NewReward.HasErrored) {
						this.RewardId = NewReward.data[0].id;
					}
				}

				this.referenceEventSub.SubscribeToEvent<ChannelPointsCustomRewardRedemptionAdd>(new Condition() {
					broadcaster_user_id = this.UserID,
					reward_id = this.RewardId,
				}, APIScopeWarning.None).ConfigureAwait(false);
			}


			yield return null;
		}

		private void OnRewardRedemption(ChannelPointsCustomRewardRedemptionAdd add) {
			if (!string.IsNullOrEmpty(add.id)) {
				DebugManager.LogMessage("OnChannelPointsCustomRewardRedemptionAdd : " + add.id);

				if (this.OutputText == null) {
					return;
				}

				this.OutputText.text = JsonWriter.Serialize(add, true);
			}
		}
	}
}