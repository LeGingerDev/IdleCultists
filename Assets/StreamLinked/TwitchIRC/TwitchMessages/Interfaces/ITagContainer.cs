using System;

using ScoredProductions.StreamLinked.IRC.Tags;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.ManagersAndBuilders;

namespace ScoredProductions.StreamLinked.IRC.Message.Interface {
	public interface ITagContainer {

		public static JsonValue ExtractTags(string source, string defaultValue = "") => ExtractTags(source.AsSpan(), defaultValue);
		public static JsonValue ExtractTags(ReadOnlySpan<char> source, string defaultValue = "") {
			JsonObject TagValue = new JsonObject();
			int len = source.Length;
			if (len <= 1) {
				return TagValue;
			}

			bool endFound;
			int startPoint = source[0] == '@' ? 1 : 0;
			do {
				source = source[startPoint..];
				int end = source.IndexOf(';');
				endFound = end != -1;

				ReadOnlySpan<char> slice = endFound ? source[..end] : source;

				int seperator = slice.IndexOf('=');

				string tagName = new string(slice[..seperator]);
				string tagValue = new string(slice[(seperator + 1)..]);

				TagValue.Add(tagName, string.IsNullOrEmpty(tagValue) ? defaultValue : tagValue);

				startPoint = end + 1;
			} while (endFound);

			return TagValue;
		}

		/// <summary>
		/// Returns a pre-sorted array of emote positions
		/// </summary>
		/// <param name="emoteString"></param>
		/// <param name="chatMessage"></param>
		/// <param name="Emotes"></param>
		/// <returns>Emotes > 0</returns>
		public static bool ExtractEmotes(string emoteString, string chatMessage, out EmoteDetails[] Emotes) {
			Emotes = null;
			if (string.IsNullOrWhiteSpace(emoteString)) {
				return false;
			}
			int len = emoteString.Length;

			int totalEmotes = 0;
			for (int x = 0; x < emoteString.Length; x++) {
				totalEmotes += emoteString[x] == '-' ? 1 : 0;
			}

			if (totalEmotes < 1) {
				return false;
			}

			Emotes = new EmoteDetails[totalEmotes];
			int count = 0;
			bool clientFound = TwitchEmoteManager.GetInstance(out TwitchEmoteManager client);

			int previous = 0;
			for (int x = 0; x < len; x++) {
				bool endOfLine = x == (len - 1);
				if (endOfLine || (emoteString[x] == '/' && !endOfLine && (x == 0 || emoteString[x - 1] != '/'))) {
					bool onlyWhiteSpace = true;
					int indexMax = x + Convert.ToInt32(endOfLine);
					for (int y = previous; y < indexMax; y++) {
						if (!char.IsWhiteSpace(emoteString[y])) {
							onlyWhiteSpace = false;
							break;
						}
					}
					if (onlyWhiteSpace) {
						previous = x + 1;
						continue;
					}

					string name = null;
					bool idRequested = false;
					Span<char> part = stackalloc char[(x + 1) - previous];

					int index = 0;
					for (int y = previous; y < indexMax; y++) {
						part[index++] = emoteString[y];
					}

					int seperatorIndex = part.IndexOf(':');
					string id = new string(part[..seperatorIndex]);
					Span<char> positions = part[(seperatorIndex + 1)..];

					int emotePrevious = 0;
					int posLen = positions.Length;
					for (int y = 0; y < posLen; y++) {
						bool endOfPositionLine = y == (posLen - 1);
						if (endOfPositionLine || (positions[y] == ',' && (y == 0 || positions[y - 1] != ','))) {
							Span<char> pair = positions[emotePrevious..(y + Convert.ToInt32(endOfPositionLine))];
							int valuesSeperatorIndex = pair.IndexOf('-');

							int start = int.Parse(pair[..valuesSeperatorIndex]);
							int end = int.Parse(pair[(valuesSeperatorIndex + 1)..]) + 1; // C# indexes for strings stops 1 character before provided values so +1

							if (string.IsNullOrWhiteSpace(name)) {
								name = chatMessage[start..end];
							}

							Emotes[count++] = new EmoteDetails(id, start, end, name);
							if (!idRequested && clientFound) {
								idRequested = true;
								client.QueueEmoteDownload(id, name);
							}

							emotePrevious = y + 1;
						}
					}

					previous = x + 1;
				}
			}

			Array.Sort(Emotes);
			return true;
		}

		/// <summary>
		/// Process badges into seperate containers
		/// </summary>
		/// <param name="badgeString"></param>
		/// <param name="Badges"></param>
		/// <returns>badges > 0</returns>
		public static bool ExtractBadges(string badgeString, out BadgeDetails[] Badges) {
			Badges = null;

			if (string.IsNullOrWhiteSpace(badgeString)) {
				return false;
			}

			int len = badgeString.Length;

			int totalBadges = 0;
			for (int x = 0; x < len; x++) {
				totalBadges += badgeString[x] == '/' ? 1 : 0;
			}

			if (totalBadges < 1) {
				return false;
			}

			Badges = new BadgeDetails[totalBadges];
			int count = 0;

			int previous = 0;
			for (int x = 0; x < len; x++) {
				bool endOfLine = x == (len - 1);
				if (endOfLine || (badgeString[x] == ',' && (x == 0 || badgeString[x - 1] != ','))) {
					bool onlyWhiteSpace = true;
					int indexMax = x + Convert.ToInt32(endOfLine);
					for (int y = previous; y < indexMax; y++) {
						if (!char.IsWhiteSpace(badgeString[y])) {
							onlyWhiteSpace = false;
							break;
						}
					}
					if (onlyWhiteSpace) {
						previous = x + 1;
						continue;
					}

					Span<char> part = stackalloc char[(x + 1) - previous];

					int index = 0;
					for (int y = previous; y < indexMax; y++) {
						part[index++] = badgeString[y];
					}

					int slash = part.IndexOf('/');
					string badge;
					if (slash < 0) {
						badge = new string(part);
						Badges[count++] = new BadgeDetails(badge, 0);
					}
					else {
						badge = new string(part[..slash]);
						// If prediction, version 1 (blue) or 2 (red), else subscriber number
						if (badge.Contains(TwitchWords.PREDICTION_BADGE)) {
							Badges[count++] = new BadgeDetails(badge, part[slash + 1] == TwitchWords.PREDICTION_BLUE[0] ? 1 : 2);
						}
						else {
							Badges[count++] = new BadgeDetails(badge, int.Parse(part[(slash + 1)..]));
						}
					}

					previous = x + 1;
				}
			}
			return true;
		}
	}
}