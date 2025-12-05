using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.IRC.Tags;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/chat/irc/#usernotice-command">Twitch IRC Command</see>: Sent when events occur in the channel, such as someone subscribing to the channel.
	/// </summary>
	[IRCCommandType(TwitchIRCCommand.USERSTATE)]
	public readonly struct USERSTATE : ISenderMessage, ITagMessage<USERSTATE.TAGS> {

		//public const string PROTOTYPE = "<tags> :tmi.twitch.tv USERSTATE #<channel>";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.USERSTATE;

		private readonly string sender;
		public string Sender => this.sender;


		public readonly string Channel;

		private readonly TAGS tags;
		public readonly TAGS Tags => this.tags;

		public USERSTATE(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();
			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int seperator;

			if (messageData[0] == '@') {
				seperator = messageData.IndexOf(' ');
				ReadOnlySpan<char> tagSegment = messageData[..seperator++];
				messageData = messageData[seperator..];
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

		public readonly BadgeDetails[] GetBadgeData(out string room_id) { room_id = string.Empty; return this.tags.badges ?? Array.Empty<BadgeDetails>(); }
		public EmoteDetails[] GetEmoteData() => Array.Empty<EmoteDetails>();
		public readonly bool CheckHasBadgesOrEmotes() => this.GetBadgeData(out _).Length > 0;
		public readonly string GetColor() => this.tags.color;
		public ITagContainer GetTagContainer() => this.tags;



		[IRCCommandType(TwitchIRCCommand.USERSTATE)]
		public readonly struct TAGS : ITagContainer {

			public readonly string badge_info;
			public readonly BadgeDetails[] badges;
			public readonly string color;
			public readonly string displayName;
			public readonly string[] emoteSets;
			public readonly string id;
			public readonly bool mod;
			public readonly bool subscriber;
			public readonly bool turbo;
			public readonly string user_type;


			public TAGS(JsonValue tags) {
				this.badge_info = tags[TwitchIRCTags.USERSTATE.BADGE_INFO].AsString;

				if (ITagContainer.ExtractBadges(tags[TwitchIRCTags.USERSTATE.BADGES].AsString, out BadgeDetails[] badgeArray)) {
					this.badges = badgeArray;
				}
				else {
					this.badges = null;
				}

				this.color = tags[TwitchIRCTags.USERSTATE.COLOR].AsString;
				this.displayName = tags[TwitchIRCTags.USERSTATE.DISPLAY_NAME].AsString;

				JsonValue emoteSets = tags[TwitchIRCTags.USERSTATE.EMOTE_SETS];
				if (emoteSets != JsonValue.Null) {
					this.emoteSets = emoteSets.AsString.Split(',');
				}
				else {
					this.emoteSets = null;
				}

				this.id = tags[TwitchIRCTags.USERSTATE.ID].AsString;
				this.mod = tags[TwitchIRCTags.USERSTATE.MOD].AsBoolean;
				this.subscriber = tags[TwitchIRCTags.USERSTATE.SUBSCRIBER].AsBoolean;
				this.turbo = tags[TwitchIRCTags.USERSTATE.TURBO].AsBoolean;
				this.user_type = tags[TwitchIRCTags.USERSTATE.USER_TYPE].AsString;
			}
		}
	}
}
