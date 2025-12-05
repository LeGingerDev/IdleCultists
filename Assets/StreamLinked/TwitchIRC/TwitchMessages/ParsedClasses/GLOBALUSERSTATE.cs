using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.IRC.Tags;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/chat/irc/#globaluserstate-command">Twitch IRC Command</see>: Sent after the bot authenticates with the server.
	/// </summary>
	[IRCCommandType(TwitchIRCCommand.GLOBALUSERSTATE)]
	public readonly struct GLOBALUSERSTATE : ISenderMessage, ITagMessage<GLOBALUSERSTATE.TAGS> {

		//public const string PROTOTYPE = "<tags> :tmi.twitch.tv GLOBALUSERSTATE"; // Added <tags> to Twitchs prototype example

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;

		public readonly TwitchIRCCommand Command => TwitchIRCCommand.GLOBALUSERSTATE;

		private readonly string sender;
		public readonly string Sender => this.sender;


		private readonly TAGS tags;
		public readonly TAGS Tags => this.tags;

		public GLOBALUSERSTATE(string data) {
			this.rawMessage = data;
			int sperator;

			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData[0] == '@') {
				sperator = messageData.IndexOf(' ');
				ReadOnlySpan<char> tagSegment = messageData[..sperator++];
				messageData = messageData[sperator..];
				this.tags = new TAGS(ITagContainer.ExtractTags(tagSegment));
			} else {
				this.tags = new TAGS();
			}

			sperator = messageData.IndexOf(' ');
			this.sender = new string(messageData[1..sperator]);
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);

		public readonly BadgeDetails[] GetBadgeData(out string room_id) { room_id = string.Empty; return this.tags.badges ?? Array.Empty<BadgeDetails>(); }
		public readonly EmoteDetails[] GetEmoteData() => Array.Empty<EmoteDetails>();
		public readonly bool CheckHasBadgesOrEmotes() => this.GetBadgeData(out _).Length > 0;
		public readonly string GetColor() => this.tags.color;
		public ITagContainer GetTagContainer() => this.tags;



		[IRCCommandType(TwitchIRCCommand.GLOBALUSERSTATE)]
		public readonly struct TAGS : ITagContainer {

			public readonly int badge_info;
			public readonly BadgeDetails[] badges;
			public readonly string color;
			public readonly string display_name;
			public readonly string[] emoteSets;
			public readonly bool turbo;
			public readonly string user_id;
			public readonly string user_type;

			public TAGS(JsonValue tags) {
				this.badge_info = tags[TwitchIRCTags.GLOBALUSERSTATE.BADGE_INFO].AsInteger;

				if (ITagContainer.ExtractBadges(tags[TwitchIRCTags.GLOBALUSERSTATE.BADGES].AsString, out BadgeDetails[] badgeArray)) {
					this.badges = badgeArray;
				}
				else {
					this.badges = null;
				}

				this.color = tags[TwitchIRCTags.GLOBALUSERSTATE.COLOR].AsString;
				this.display_name = tags[TwitchIRCTags.GLOBALUSERSTATE.DISPLAY_NAME].AsString;

				JsonValue emoteSets = tags[TwitchIRCTags.GLOBALUSERSTATE.EMOTE_SETS];
				if (emoteSets != JsonValue.Null) {
					this.emoteSets = emoteSets.AsString.Split(',');
				}
				else {
					this.emoteSets = null;
				}

				this.turbo = tags[TwitchIRCTags.GLOBALUSERSTATE.TURBO].AsBoolean;
				this.user_id = tags[TwitchIRCTags.GLOBALUSERSTATE.USER_ID].AsString;
				this.user_type = tags[TwitchIRCTags.GLOBALUSERSTATE.USER_TYPE].AsString;
			}
		}
	}
}
