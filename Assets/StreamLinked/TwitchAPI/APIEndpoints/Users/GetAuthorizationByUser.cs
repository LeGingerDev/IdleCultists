using System;

using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.Users {

	/// <summary>
	/// Gets the authorization scopes that the specified user has granted the application.
	/// <see href="https://dev.twitch.tv/docs/api/reference/#get-authorization-by-user">Twitch API Class</see>
	/// </summary>
	[Serializable]
	public struct GetAuthorizationByUser : IUsers {

		[field: SerializeField] public string user_id { get; set; }
		[field: SerializeField] public string user_name { get; set; }
		[field: SerializeField] public string user_login { get; set; }
		[field: SerializeField] public string[] scopes { get; set; }

		public void Initialise(JsonValue body) {
			this.user_id = body[TwitchWords.USER_ID].AsString;
			this.user_name = body[TwitchWords.USER_NAME].AsString;
			this.user_login = body[TwitchWords.USER_LOGIN].AsString;
			this.scopes = body[TwitchWords.SCOPES].AsJsonArray.CastToStringArray;
		}

		public readonly TwitchAPIRequestMethod HTTPMethod => TwitchAPIRequestMethod.GET;
		public readonly string Endpoint => TwitchAPILinks.UsersAuthorization;
		public readonly TwitchAPIClassEnum TypeEnum => TwitchAPIClassEnum.GetAuthorizationByUser;
		public readonly TwitchScopesEnum[] Scopes => Array.Empty<TwitchScopesEnum>();

		public static string USER_ID => TwitchWords.USER_ID;
	}
}