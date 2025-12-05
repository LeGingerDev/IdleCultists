using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.IRC.Tags;
using ScoredProductions.StreamLinked.ManagersAndBuilders;
using ScoredProductions.StreamLinked.Utility;

namespace ScoredProductions.StreamLinked.IRC.Message {

	public static class TwitchMessageTagHelper {

		/// <summary>
		/// Provides a Tuple including the Username section and Message section in TextMeshPro format. Built as Enumerator to allow yield waiting for managers to finish processing.
		/// </summary>
		/// <param name="Callback"></param>
		/// <returns></returns>
		public static IEnumerator BuildMessageForTextmeshWithWait<T>(this T value, Action<(string User, string Message)> Callback) where T : ITagMessage, IUserMessage, IParsedMessage {
			bool emotesExist = TwitchEmoteManager.GetInstance(out TwitchEmoteManager emotesInstance);
			bool badgesExist = TwitchBadgeManager.GetInstance(out TwitchBadgeManager badgeInstance);

			(string user, string message) returningMessage = ("", "");
			StringBuilder builder = new StringBuilder();

			BadgeDetails[] badges = value.GetBadgeData(out string room_id);
			string colour = value.GetColor();
			EmoteDetails[] receivedEmotes = value.GetEmoteData();

			while ((badgesExist && badgeInstance.Busy) | (emotesExist && emotesInstance.Busy)) {
				yield return TwitchStatic.EndOfFrameWait;
			}

			if (badgesExist && badges != null && badges.Length > 0) {
				builder.Append(badgeInstance.GetBadgeTMPText(badges, room_id));
			}

			if (string.IsNullOrEmpty(colour)) {
				builder.Append(value.User);
			}
			else {
				builder.Append(value.User.RichTextColour(colour));
			}

			builder.Append(':');
			returningMessage.user = builder.ToString();

			// Message

			builder.Clear();
			int previousEnd = 0;

			if (emotesExist && receivedEmotes != null && receivedEmotes.Length > 0) {
				List<(string emoteName, string textmeshpro)> emoteText = emotesInstance.GetEmotesTMPText(receivedEmotes);
				string text;
				foreach (EmoteDetails emotes in receivedEmotes) {
					text = "";
					for (int x = 0; x < emoteText.Count; x++) {
						(string emoteName, string textmeshpro) = emoteText[x];
						if (!string.IsNullOrEmpty(emoteName) && !string.IsNullOrEmpty(textmeshpro) && emoteName.Equals(emotes.EmoteId)) {
							text = textmeshpro;
							break;
						}
					}

					if (!string.IsNullOrEmpty(text)) {
						if (emotes.Start > 0) {
							builder.Append(value.Message[previousEnd..emotes.Start] + text);
						}
						else {
							builder.Append(text);
						}

						previousEnd = emotes.End;
					}
				}
				if (previousEnd < value.Message.Length) {
					builder.Append(value.Message[previousEnd..]);
				}
			}

			if (builder.Length > 0) {
				returningMessage.message = builder.ToString();
			}
			else {
				returningMessage.message = value.Message;
			}

			yield return returningMessage;
			Callback.Invoke(returningMessage);
		}

		/// <summary>
		/// Provides a Tuple including the Username section and Message section in TextMeshPro format. Will not wait for managers to finish.
		/// </summary>
		/// <returns></returns>
		public static (string User, string Message) BuildMessageForTextmesh<T>(this T value) where T : ITagMessage, IUserMessage, IParsedMessage {
			(string user, string message) returningMessage = ("", "");
			StringBuilder builder = new StringBuilder();

			BadgeDetails[] badges = value.GetBadgeData(out string room_id);
			string colour = value.GetColor();
			EmoteDetails[] receivedEmotes = value.GetEmoteData();

			if (badges != null && badges.Length > 0 && TwitchBadgeManager.GetInstance(out TwitchBadgeManager badgeInstance)) {
				builder.Append(badgeInstance.GetBadgeTMPText(badges, room_id));
			}

			if (string.IsNullOrEmpty(colour)) {
				builder.Append(value.User);
			}
			else {
				builder.Append(value.User.RichTextColour(colour));
			}

			builder.Append(':');
			returningMessage.user = builder.ToString();

			// Message

			builder.Clear();
			int previousEnd = 0;

			if (receivedEmotes != null && receivedEmotes.Length > 0 && TwitchEmoteManager.GetInstance(out TwitchEmoteManager emotesInstance)) {
				List<(string emoteName, string textmeshpro)> emoteText = emotesInstance.GetEmotesTMPText(receivedEmotes);
				string text;
				foreach (EmoteDetails emotes in receivedEmotes) {
					text = "";
					for (int x = 0; x < emoteText.Count; x++) {
						(string emoteName, string textmeshpro) = emoteText[x];
						if (!string.IsNullOrEmpty(emoteName) && !string.IsNullOrEmpty(textmeshpro) && emoteName.Equals(emotes.EmoteId)) {
							text = textmeshpro;
							break;
						}
					}

					if (!string.IsNullOrEmpty(text)) {
						if (emotes.Start > 0) {
							builder.Append(value.Message[previousEnd..emotes.Start] + text);
						}
						else {
							builder.Append(text);
						}

						previousEnd = emotes.End;
					}
				}
				if (previousEnd < value.Message.Length) {
					builder.Append(value.Message[previousEnd..]);
				}
			}

			if (builder.Length > 0) {
				returningMessage.message = builder.ToString();
			}
			else {
				returningMessage.message = value.Message;
			}

			return returningMessage;
		}

		/// <summary>
		/// Provides a list of sections of the message in TextMeshPro format. Divided between images and text with inclusion of the ':' seperatly. Built as Enumerator to allow yield waiting for managers to finish processing.
		/// </summary>
		/// <returns></returns>
		public static IEnumerator BuildMessageAsComponentsWithWait<T>(this T value, Action<List<string>> Callback) where T : ITagMessage, IUserMessage, IParsedMessage {
			bool emotesExist = TwitchEmoteManager.GetInstance(out TwitchEmoteManager emotesInstance);
			bool badgesExist = TwitchBadgeManager.GetInstance(out TwitchBadgeManager badgeInstance);
			List<string> messageComponents = new List<string>();

			BadgeDetails[] badges = value.GetBadgeData(out string room_id);
			string colour = value.GetColor();
			EmoteDetails[] receivedEmotes = value.GetEmoteData();

			while ((badgesExist && badgeInstance.Busy) | (emotesExist && emotesInstance.Busy)) {
				yield return TwitchStatic.EndOfFrameWait;
			}

			if (badgesExist && badges != null && badges.Length > 0) {
				for (int x = 0; x < badges.Length; x++) {
					messageComponents.Add(badgeInstance.GetBadgeTMPText(badges[x], room_id));
				}
			}

			if (string.IsNullOrEmpty(colour)) {
				messageComponents.Add(value.User);
			}
			else {
				messageComponents.Add(value.User.RichTextColour(colour));
			}

			messageComponents.Add(":"); // Seperator between Username and Message

			int previousEnd = 0;

			if (emotesExist && receivedEmotes != null && receivedEmotes.Length > 0) {
				List<(string emoteName, string textmeshpro)> emoteText = emotesInstance.GetEmotesTMPText(receivedEmotes);
				string text;
				foreach (EmoteDetails emotes in receivedEmotes) {
					text = "";
					for (int x = 0; x < emoteText.Count; x++) {
						(string emoteName, string textmeshpro) = emoteText[x];
						if (!string.IsNullOrEmpty(emoteName) && !string.IsNullOrEmpty(textmeshpro) && emoteName.Equals(emotes.EmoteId)) {
							text = textmeshpro;
							break;
						}
					}

					if (!string.IsNullOrEmpty(text)) {
						if (emotes.Start > 0) {
							messageComponents.Add(value.Message[previousEnd..emotes.Start] + text);
						}
						else {
							messageComponents.Add(text);
						}

						previousEnd = emotes.End;
					}
				}
				if (previousEnd < value.Message.Length) {
					messageComponents.Add(value.Message[previousEnd..]);
				}
			}
			else {
				messageComponents.Add(value.Message);
			}

			yield return messageComponents;
			Callback?.Invoke(messageComponents);
		}

		/// <summary>
		/// Provides a list of sections of the message in TextMeshPro format. Divided between images and text with inclusion of the ':' seperatly. Will not wait for managers to finish.
		/// </summary>
		/// <returns></returns>
		public static List<string> BuildMessageAsComponents<T>(this T value) where T : ITagMessage, IUserMessage, IParsedMessage {
			List<string> messageComponents = new List<string>();

			BadgeDetails[] badges = value.GetBadgeData(out string room_id);
			string colour = value.GetColor();
			EmoteDetails[] receivedEmotes = value.GetEmoteData();

			if (badges != null && badges.Length > 0 && TwitchBadgeManager.GetInstance(out TwitchBadgeManager badgeInstance)) {
				for (int x = 0; x < badges.Length; x++) {
					messageComponents.Add(badgeInstance.GetBadgeTMPText(badges[x], room_id));
				}
			}

			if (string.IsNullOrEmpty(colour)) {
				messageComponents.Add(value.User);
			}
			else {
				messageComponents.Add(value.User.RichTextColour(colour));
			}

			messageComponents.Add(":"); // Seperator between Username and Message

			int previousEnd = 0;

			if (receivedEmotes != null && receivedEmotes.Length > 0 && TwitchEmoteManager.GetInstance(out TwitchEmoteManager emotesInstance)) {
				List<(string emoteName, string textmeshpro)> emoteText = emotesInstance.GetEmotesTMPText(receivedEmotes);
				string text;
				foreach (EmoteDetails emotes in receivedEmotes) {
					text = "";
					for (int x = 0; x < emoteText.Count; x++) {
						(string emoteName, string textmeshpro) = emoteText[x];
						if (!string.IsNullOrEmpty(emoteName) && !string.IsNullOrEmpty(textmeshpro) && emoteName.Equals(emotes.EmoteId)) {
							text = textmeshpro;
							break;
						}
					}

					if (!string.IsNullOrEmpty(text)) {
						if (emotes.Start > 0) {
							messageComponents.Add(value.Message[previousEnd..emotes.Start] + text);
						}
						else {
							messageComponents.Add(text);
						}

						previousEnd = emotes.End;
					}
				}
				if (previousEnd < value.Message.Length) {
					messageComponents.Add(value.Message[previousEnd..]);
				}
			}
			else {
				messageComponents.Add(value.Message);
			}

			return messageComponents;
		}

	}

}
