namespace ScoredProductions.StreamLinked.API.Auth {
	public interface IAuth : ITwitchAPIDataObject { 
		public static bool EndpointIsAuthRequest(string endpoint) {
			return endpoint.StartsWith(TwitchAPILinks.GetOAuth2Link);
		}
	}
}
