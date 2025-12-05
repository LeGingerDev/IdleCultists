using System;
using System.Text;

using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.Auth {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#authorization-code-grant-flow">Twitch API Class</see>
	/// </summary>
	[Serializable]
	public struct GetAuthorizationCodeToken : IAuth {

		[field: SerializeField] public string access_token { get; set; }
		[field: SerializeField] public long expires_in { get; set; }
		[field: SerializeField] public string refresh_token { get; set; }
		[field: SerializeField] public string[] scope { get; set; }
		[field: SerializeField] public string token_type { get; set; }

		public void Initialise(JsonValue body) {
			this.access_token = body[TwitchWords.ACCESS_TOKEN].AsString;
			this.expires_in = body[TwitchWords.EXPIRES_IN].AsLong;
			this.refresh_token = body[TwitchWords.REFRESH_TOKEN].AsString;
			this.scope = body[TwitchWords.SCOPE].AsJsonArray?.CastToStringArray;
			this.token_type = body[TwitchWords.TOKEN_TYPE].AsString;
		}

		public readonly TwitchAPIRequestMethod HTTPMethod => TwitchAPIRequestMethod.POST;
		public readonly string Endpoint => TwitchAPILinks.GetAuthToken;
		public readonly TwitchAPIClassEnum TypeEnum => TwitchAPIClassEnum.GetAuthorizationCodeToken;
		public readonly TwitchScopesEnum[] Scopes => Array.Empty<TwitchScopesEnum>();

		public static string CLIENT_ID => TwitchWords.CLIENT_ID;
		public static string CLIENT_SECRET => TwitchWords.CLIENT_SECRET;
		public static string CODE => TwitchWords.CODE;
		public static (string, string) GRANT_TYPE => (TwitchWords.GRANT_TYPE, TwitchWords.AUTHORIZATION_CODE);
		public static string REDIRECT_URI => TwitchWords.REDIRECT_URI;

		public static string GenerateBodyString(string clientId, string clientSecret, string code, string redirectURI) {
			if (string.IsNullOrWhiteSpace(clientId)
				|| string.IsNullOrWhiteSpace(clientSecret)
				|| string.IsNullOrWhiteSpace(code)
				|| string.IsNullOrWhiteSpace(redirectURI)) {
				throw new ArgumentException("All values must be provided and populated to generate the body");
			}
			StringBuilder builder = new StringBuilder(256);
			builder.Append(CLIENT_ID);
			builder.Append('=');
			builder.Append(clientId);
			builder.Append('&');
			builder.Append(CLIENT_SECRET);
			builder.Append('=');
			builder.Append(clientSecret);
			builder.Append('&');
			builder.Append(CODE);
			builder.Append('=');
			builder.Append(code);
			builder.Append('&');
			(string grantType, string grantCode) = GRANT_TYPE;
			builder.Append(grantType);
			builder.Append('=');
			builder.Append(grantCode);
			builder.Append('&');
			builder.Append(REDIRECT_URI);
			builder.Append('=');
			builder.Append(redirectURI);
			return builder.ToString();
		}
	}
}