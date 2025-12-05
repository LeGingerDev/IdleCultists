using System;

using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.LightJson;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.Clips {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/api/reference#get-clips-download">Twitch API Class</see>
	/// </summary>
	[Serializable]
	public struct GetClipsDownload : IClips {

		[field: SerializeField] public string clip_id { get; set; }
		[field: SerializeField] public string landscape_download_url { get; set; }
		[field: SerializeField] public string portrait_download_url { get; set; }

		public void Initialise(JsonValue body) {
			this.clip_id = body[TwitchWords.CLIP_ID].AsString;
			this.landscape_download_url = body[TwitchWords.LANDSCAPE_DOWNLOAD_URL].AsString;
			this.portrait_download_url = body[TwitchWords.PORTRAIT_DOWNLOAD_URL].AsString;
		}

		public readonly TwitchAPIRequestMethod HTTPMethod => TwitchAPIRequestMethod.GET;
		public readonly string Endpoint => TwitchAPILinks.ClipsDownloads;
		public readonly TwitchAPIClassEnum TypeEnum => TwitchAPIClassEnum.GetClipsDownload;
		public readonly TwitchScopesEnum[] Scopes => new TwitchScopesEnum[] {
			TwitchScopesEnum.editor_manage_clips,
			TwitchScopesEnum.channel_manage_clips,
		};

		public static string EDITOR_ID => TwitchWords.EDITOR_ID;
		public static string BROADCASTER_ID => TwitchWords.BROADCASTER_ID;
		public static string CLIP_ID => TwitchWords.CLIP_ID;

	}
}