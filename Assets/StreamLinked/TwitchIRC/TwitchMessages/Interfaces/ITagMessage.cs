using System;

using ScoredProductions.StreamLinked.IRC.Tags;

namespace ScoredProductions.StreamLinked.IRC.Message.Interface {

	public interface ITagMessage : ITwitchIRCMessage {

		public BadgeDetails[] GetBadgeData(out string room_id);
		public EmoteDetails[] GetEmoteData();
		public bool CheckHasBadgesOrEmotes();
		public string GetColor();
		/// <summary>
		/// Interface access to message tags, if not using interface, access tags via field <c>Tags</c> else boxing and casting will be required
		/// </summary>
		/// <returns></returns>
		public ITagContainer GetTagContainer();

		public string[] GetBadgeNames(out string room_id) {
			BadgeDetails[] badgeData = this.GetBadgeData(out room_id);
			int len;
			if (badgeData != null && (len = badgeData.Length) > 0) {
				string[] badgeNames = new string[len];
				for (int x = 0; x < len; x++) {
					badgeNames[x] = badgeData[x].Name;
				}

				return badgeNames;
			}
			else {
				return Array.Empty<string>();
			}
		}

		public string[] GetEmoteNames() {
			EmoteDetails[] emoteData = this.GetEmoteData();
			int len;
			if (emoteData != null && (len = emoteData.Length) > 0) {
				string[] emoteIds = new string[len];
				for (int x = 0; x < len; x++) {
					emoteIds[x] = emoteData[x].EmoteId;
				}
				return emoteIds;
			}
			else {
				return Array.Empty<string>();
			}
		}

	}

	public interface ITagMessage<T> : ITagMessage where T : ITagContainer, new() {
		public T Tags { get; }
	}
}