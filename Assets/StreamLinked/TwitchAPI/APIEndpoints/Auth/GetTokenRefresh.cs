using System;
using System.Text;

using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.LightJson;

namespace ScoredProductions.StreamLinked.API.Auth {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/authentication/refresh-tokens">Twitch API Class</see>
	/// </summary>
	[Serializable]
	public struct GetTokenRefresh : IAuth {

		public string access_token { get; set; }
		public string refresh_token { get; set; }
		public string[] scope { get; set; }
		public string token_type { get; set; }

		public void Initialise(JsonValue body) {
			this.access_token = body[TwitchWords.ACCESS_TOKEN].AsString;
			this.refresh_token = body[TwitchWords.REFRESH_TOKEN].AsString;
			this.scope = body[TwitchWords.SCOPE].AsJsonArray?.CastToStringArray;
			this.token_type = body[TwitchWords.TOKEN_TYPE].AsString;
		}

		public readonly TwitchAPIRequestMethod HTTPMethod => TwitchAPIRequestMethod.POST;
		public readonly string Endpoint => TwitchAPILinks.GetAuthToken;
		public readonly TwitchAPIClassEnum TypeEnum => TwitchAPIClassEnum.GetTokenRefresh;
		public readonly TwitchScopesEnum[] Scopes => Array.Empty<TwitchScopesEnum>();

		public static (string, string) GRANT_TYPE => (TwitchWords.GRANT_TYPE, TwitchWords.REFRESH_TOKEN);
		public static string CLIENT_ID => TwitchWords.CLIENT_ID;
		public static string CLIENT_SECRET => TwitchWords.CLIENT_SECRET;
		public static string REFRESH_TOKEN => TwitchWords.REFRESH_TOKEN;

		public static string GenerateBodyString(string clientId, string refresh_token, string clientSecret = null) {
			if (string.IsNullOrWhiteSpace(clientId)
				|| string.IsNullOrWhiteSpace(refresh_token)) {
				throw new ArgumentException("All values must be provided and populated to generate the body");
			}
			StringBuilder builder = new StringBuilder(256);
			builder.Append(CLIENT_ID);
			builder.Append('=');
			builder.Append(clientId);
			builder.Append('&');
			if (!string.IsNullOrWhiteSpace(clientSecret)) {
				builder.Append(CLIENT_SECRET);
				builder.Append('=');
				builder.Append(clientSecret);
				builder.Append('&');
			}
			(string grantType, string grantCode) = GRANT_TYPE;
			builder.Append(grantType);
			builder.Append('=');
			builder.Append(grantCode);
			builder.Append('&');
			builder.Append(REFRESH_TOKEN);
			builder.Append('=');
			builder.Append(refresh_token);
			return builder.ToString();
		}
	}
}