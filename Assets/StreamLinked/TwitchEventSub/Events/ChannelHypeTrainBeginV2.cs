using System;

using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.EventSub.Events.Objects;
using ScoredProductions.StreamLinked.EventSub.Interfaces;
using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.EventSub.Events {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelhype_trainbegin-v2">Twitch Eventsub Reference</see>
	/// </summary>
	[Serializable]
	public struct ChannelHypeTrainBeginV2 : IHypeTrain {

		[field: SerializeField] public string id { get; set; }
		[field: SerializeField] public string broadcaster_user_id { get; set; }
		[field: SerializeField] public string broadcaster_user_login { get; set; }
		[field: SerializeField] public string broadcaster_user_name { get; set; }
		[field: SerializeField] public int total { get; set; }
		[field: SerializeField] public int progress { get; set; }
		[field: SerializeField] public int goal { get; set; }
		[field: SerializeField] public TopContributions top_contributions { get; set; }
		[field: SerializeField] public int level { get; set; }
		[field: SerializeField] public int all_time_high_level { get; set; }
		[field: SerializeField] public int all_time_high_total { get; set; }
		[field: SerializeField] public SharedTrainParticipants[] shared_train_participants { get; set; }
		[field: SerializeField] public string started_at { get; set; }
		[field: SerializeField] public string expires_at { get; set; }
		[field: SerializeField] public string type { get; set; }
		[field: SerializeField] public bool is_shared_train { get; set; }

		public readonly TwitchEventSubSubscriptionsEnum Enum => TwitchEventSubSubscriptionsEnum.Channel_Hype_Train_Begin_V2;
		public readonly TwitchScopesEnum[] Scopes => new TwitchScopesEnum[] {
			TwitchScopesEnum.channel_read_hype_train,
		};

		public ChannelHypeTrainBeginV2(JsonValue body) {
			this.id = body[TwitchWords.ID].AsString;
			this.broadcaster_user_id = body[TwitchWords.BROADCASTER_USER_ID].AsString;
			this.broadcaster_user_login = body[TwitchWords.BROADCASTER_USER_LOGIN].AsString;
			this.broadcaster_user_name = body[TwitchWords.BROADCASTER_USER_NAME].AsString;
			this.total = body[TwitchWords.TOTAL].AsInteger;
			this.progress = body[TwitchWords.PROGRESS].AsInteger;
			this.goal = body[TwitchWords.GOAL].AsInteger;
			this.top_contributions = new TopContributions(body[TwitchWords.TOP_CONTRIBUTIONS]);
			this.level = body[TwitchWords.LEVEL].AsInteger;
			this.all_time_high_level = body[TwitchWords.ALL_TIME_HIGH_LEVEL].AsInteger;
			this.all_time_high_total = body[TwitchWords.ALL_TIME_HIGH_TOTAL].AsInteger;
			this.shared_train_participants = body[TwitchWords.SHARED_TRAIN_PARTICIPANTS].AsJsonArray.ToModelArray<SharedTrainParticipants>();
			this.started_at = body[TwitchWords.STARTED_AT].AsString;
			this.expires_at = body[TwitchWords.EXPIRES_AT].AsString;
			this.type = body[TwitchWords.TYPE].AsString;
			this.is_shared_train = body[TwitchWords.IS_SHARED_TRAIN].AsBoolean;
		}
	}
}