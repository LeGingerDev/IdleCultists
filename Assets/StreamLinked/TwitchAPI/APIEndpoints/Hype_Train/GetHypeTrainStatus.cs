using System;

using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.API.SharedContainers;
using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.Hype_Train {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/api/reference/#get-hype-train-status">Twitch API Class</see>
	/// </summary>
	[Serializable]
	public struct GetHypeTrainStatus : IHype_Train {

		[field: SerializeField] public Current current { get; set; }
		[field: SerializeField] public All_Time_High all_time_high { get; set; }
		[field: SerializeField] public Shared_All_Time_High shared_all_time_high { get; set; }

		public void Initialise(JsonValue body) {
			this.current = new Current(body[TwitchWords.CURRENT]);
			this.all_time_high = new All_Time_High(body[TwitchWords.ALL_TIME_HIGH]);
			this.shared_all_time_high = new Shared_All_Time_High(body[TwitchWords.SHARED_ALL_TIME_HIGH]);
		}

		public readonly TwitchAPIRequestMethod HTTPMethod => TwitchAPIRequestMethod.GET;
		public readonly string Endpoint => TwitchAPILinks.GetHypeTrainStatus;
		public readonly TwitchAPIClassEnum TypeEnum => TwitchAPIClassEnum.GetHypeTrainStatus;
		public readonly TwitchScopesEnum[] Scopes => new TwitchScopesEnum[] {
			TwitchScopesEnum.channel_read_hype_train,
		};

		public static string BROADCASTER_ID => TwitchWords.BROADCASTER_ID;
	}
}