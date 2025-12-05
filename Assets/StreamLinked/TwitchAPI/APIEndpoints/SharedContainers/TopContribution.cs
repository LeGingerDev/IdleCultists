using System;

using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.SharedContainers {

	[Serializable]
	public struct TopContribution : IShared {

		[field: SerializeField] public int total { get; set; }
		[field: SerializeField] public string type { get; set; }
		[field: SerializeField] public string user { get; set; }
		[field: SerializeField] public string user_id { get; set; } // sigh...
		[field: SerializeField] public string user_login { get; set; }
		[field: SerializeField] public string user_name { get; set; }

		public TopContribution(JsonValue body) {
			this.total = body[TwitchWords.TOTAL].AsInteger;
			this.type = body[TwitchWords.TYPE].AsString;
			this.user = body[TwitchWords.USER].AsString;
			this.user_id = body[TwitchWords.USER_ID].AsString;
			this.user_login = body[TwitchWords.USER_LOGIN].AsString;
			this.user_name = body[TwitchWords.USER_NAME].AsString;
		}
	}
}
