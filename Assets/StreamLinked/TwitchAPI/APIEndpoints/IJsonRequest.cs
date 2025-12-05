namespace ScoredProductions.StreamLinked.API {
	/// <summary>
	/// Ensure a static string called BuildDataJson is included, NetStandard 2.1 doesnt include inherited statics
	/// </summary>
	public interface IJsonRequest {

		public const string MethodName = "BuildDataJson";

		public static readonly (string, string) CONTENT_TYPE = (TwitchWords.CONTENT_TYPE, TwitchWords.APPLICATION_JSON);

	}
}
