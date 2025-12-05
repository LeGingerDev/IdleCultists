using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.IRC.Tags;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/chat/irc/#clearmsg-command">Twitch IRC Command</see>: Sent when the bot or moderator removes all messages from the chat room or removes all messages for the specified user.
	/// </summary>
	[IRCCommandType(TwitchIRCCommand.CLEARCHAT)]
	public readonly struct CLEARCHAT : ISenderMessage, IParsedMessage, ITagMessage<CLEARCHAT.TAGS> {

		//public const string PROTOTYPE = "<tags> :tmi.twitch.tv CLEARCHAT #<channel> :<message>"; // Added <tags> to Twitchs prototype example

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;

		public readonly TwitchIRCCommand Command => TwitchIRCCommand.CLEARCHAT;

		private readonly string sender;
		public readonly string Sender => this.sender;

		private readonly string message;
		public readonly string Message => this.message;

		public readonly string Channel;

		private readonly TAGS tags;
		public readonly TAGS Tags => this.tags;

		public CLEARCHAT(string data) {
			this.rawMessage = data;
			int seperator;

			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			if (messageData[0] == '@') {
				seperator = messageData.IndexOf(' ');
				ReadOnlySpan<char> tagSegment = messageData[..seperator++];
				messageData = messageData[seperator..];
				this.tags = new TAGS(ITagContainer.ExtractTags(tagSegment));
			} else {
				this.tags = new TAGS();
			}

			seperator = messageData.IndexOf(' ');
			this.sender = new string(messageData[1..seperator++]);
			messageData = messageData[seperator..];

			seperator = messageData.IndexOf(' '); // Skip Command as we already know
			messageData = messageData[++seperator..];

			seperator = messageData.IndexOf(' ');
			this.Channel = new string(messageData[1..seperator++]);

			this.message = new string(messageData[++seperator..]); // +1 for colon
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);

		public readonly BadgeDetails[] GetBadgeData(out string room_id) { room_id = string.Empty; return Array.Empty<BadgeDetails>(); }
		public readonly EmoteDetails[] GetEmoteData() => Array.Empty<EmoteDetails>();
		public readonly bool CheckHasBadgesOrEmotes() => false;
		public readonly string GetColor() => string.Empty;
		public ITagContainer GetTagContainer() => this.tags;



		[IRCCommandType(TwitchIRCCommand.CLEARCHAT)]
		public readonly struct TAGS : ITagContainer {

			public readonly long ban_duration;
			public readonly string room_id;
			public readonly string target_user_id;
			public readonly long tmi_sent_ts;
			public readonly DateTime TimestampDate => TwitchStatic.TwitchUTCStart.AddMilliseconds(this.tmi_sent_ts);

			public TAGS(JsonValue tags) {
				this.ban_duration = tags[TwitchIRCTags.CLEARCHAT.BAN_DURATION].AsLong;
				this.room_id = tags[TwitchIRCTags.CLEARCHAT.ROOM_ID].AsString;
				this.target_user_id = tags[TwitchIRCTags.CLEARCHAT.TARGET_USER_ID].AsString;
				this.tmi_sent_ts = tags[TwitchIRCTags.CLEARCHAT.TMI_SENT_TS].AsLong;
			}
		}
	}
}
