using System;

using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.SharedContainers {

	[Serializable]
	public struct Shared_All_Time_High : IShared {

		[field: SerializeField] public int level { get; set; }
		[field: SerializeField] public int total { get; set; }
		[field: SerializeField] public string achieved_at { get; set; }

		public Shared_All_Time_High(JsonValue body) {
			this.level = body[TwitchWords.LEVEL].AsInteger;
			this.total = body[TwitchWords.TOTAL].AsInteger;
			this.achieved_at = body[TwitchWords.ACHIEVED_AT].AsString;
		}
	}
}
