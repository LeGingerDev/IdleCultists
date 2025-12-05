using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.IRC.Tags;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/chat/irc/#roomstate-command">Twitch IRC Command</see>: Sent when the bot joins a channel or when the channel’s chat settings change.
	/// </summary>
	[IRCCommandType(TwitchIRCCommand.ROOMSTATE)]
	public readonly struct ROOMSTATE : ISenderMessage, ITagMessage<ROOMSTATE.TAGS> {

		//public const string PROTOTYPE = ":tmi.twitch.tv ROOMSTATE #<channel>";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.ROOMSTATE;

		private readonly string sender;
		public string Sender => this.sender;


		public readonly string Channel;

		private readonly TAGS tags;
		public readonly TAGS Tags => this.tags;

		public ROOMSTATE(string data) {
			this.rawMessage = data;
			int seperator;
			ReadOnlySpan<char> messageData = data.AsSpan();
			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			if (messageData[0] == '@') {
				int tagSperator = messageData.IndexOf(' ');
				ReadOnlySpan<char> tagSegment = messageData[..tagSperator++];
				messageData = messageData[tagSperator..];
				this.tags = new TAGS(ITagContainer.ExtractTags(tagSegment));
			}
			else {
				this.tags = new TAGS();
			}

			seperator = messageData.IndexOf(' ');
			this.sender = new string(messageData[..seperator++]);
			messageData = messageData[seperator..];

			seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];
			this.Channel = new string(messageData[1..]);
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);

		public readonly BadgeDetails[] GetBadgeData(out string room_id) { room_id = string.Empty; return Array.Empty<BadgeDetails>(); }
		public readonly EmoteDetails[] GetEmoteData() => Array.Empty<EmoteDetails>();
		public readonly bool CheckHasBadgesOrEmotes() => false;
		public readonly string GetColor() => string.Empty;
		public ITagContainer GetTagContainer() => this.tags;



		[IRCCommandType(TwitchIRCCommand.ROOMSTATE)]
		public readonly struct TAGS : ITagContainer {

			public readonly bool emote_only;
			public readonly int followers_only;
			public readonly bool r9k;
			public readonly string room_id;
			public readonly int slow;
			public readonly bool subs_only;

			public TAGS(JsonValue tags) {
				this.emote_only = tags[TwitchIRCTags.ROOMSTATE.EMOTE_ONLY].AsBoolean;
				this.followers_only = tags[TwitchIRCTags.ROOMSTATE.FOLLOWERS_ONLY].AsInteger;
				this.r9k = tags[TwitchIRCTags.ROOMSTATE.R9K].AsBoolean;
				this.room_id = tags[TwitchIRCTags.ROOMSTATE.ROOM_ID].AsString;
				this.slow = tags[TwitchIRCTags.ROOMSTATE.SLOW].AsInteger;
				this.subs_only = tags[TwitchIRCTags.ROOMSTATE.SUBS_ONLY].AsBoolean;
			}
		}
	}
}
