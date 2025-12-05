using System;

using ScoredProductions.StreamLinked.EventSub.Events.Objects;
using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.SharedContainers {

	[Serializable]
	public struct Current : IShared {

		[field: SerializeField] public string id { get; set; }
		[field: SerializeField] public string broadcaster_user_id { get; set; }
		[field: SerializeField] public string broadcaster_user_login { get; set; }
		[field: SerializeField] public string broadcaster_user_name { get; set; }
		[field: SerializeField] public int level { get; set; }
		[field: SerializeField] public int total { get; set; }
		[field: SerializeField] public int progress { get; set; }
		[field: SerializeField] public int goal { get; set; }
		[field: SerializeField] public TopContribution[] top_contributions { get; set; }
		[field: SerializeField] public SharedTrainParticipants[] shared_train_participants { get; set; }
		[field: SerializeField] public string expires_at { get; set; }
		[field: SerializeField] public string started_at { get; set; }
		[field: SerializeField] public string type { get; set; }
		[field: SerializeField] public bool is_shared_train { get; set; }

		public Current(JsonValue body) {
			this.id = body[TwitchWords.ID].AsString;
			this.broadcaster_user_id = body[TwitchWords.BROADCASTER_USER_ID].AsString;
			this.broadcaster_user_login = body[TwitchWords.BROADCASTER_USER_LOGIN].AsString;
			this.broadcaster_user_name = body[TwitchWords.BROADCASTER_USER_NAME].AsString;
			this.level = body[TwitchWords.LEVEL].AsInteger;
			this.total = body[TwitchWords.TOTAL].AsInteger;
			this.progress = body[TwitchWords.PROGRESS].AsInteger;
			this.goal = body[TwitchWords.GOAL].AsInteger;
			this.top_contributions = body[TwitchWords.TOP_CONTRIBUTIONS].AsJsonArray?.ToModelArray<TopContribution>();
			this.shared_train_participants = body[TwitchWords.SHARED_TRAIN_PARTICIPANTS].AsJsonArray?.ToModelArray<SharedTrainParticipants>();
			this.expires_at = body[TwitchWords.EXPIRES_AT].AsString;
			this.started_at = body[TwitchWords.STARTED_AT].AsString;
			this.type = body[TwitchWords.TYPE].AsString;
			this.is_shared_train = body[TwitchWords.IS_SHARED_TRAIN].AsBoolean;
		}
	}
}
