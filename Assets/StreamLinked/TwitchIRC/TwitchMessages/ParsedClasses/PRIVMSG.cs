using System;

using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.IRC.Tags;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;
using ScoredProductions.StreamLinked.ManagersAndBuilders;

namespace ScoredProductions.StreamLinked.IRC.Message {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/chat/irc/#privmsg-command">Twitch IRC Command</see>: Sent when a user sends a chat message to a chatroom your bot has joined.
	/// </summary>
	[IRCCommandType(TwitchIRCCommand.PRIVMSG)]
	public readonly struct PRIVMSG : IUserMessage, IParsedMessage, ITagMessage<PRIVMSG.TAGS> {

		//public const string PROTOTYPE = "<tags> :<user>!<user>@<user>.tmi.twitch.tv PRIVMSG #<channel> :<message>"; // Added <tags> to Twitchs prototype example

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;

		public readonly TwitchIRCCommand Command => TwitchIRCCommand.PRIVMSG;

		private readonly string sender;
		public readonly string Sender => this.sender;

		private readonly string user;
		public readonly string User => this.user;

		private readonly string message;
		public readonly string Message => this.message;

		public readonly string Channel;

		private readonly TAGS tags;
		public readonly TAGS Tags => this.tags;

		public PRIVMSG(string data) {
			this.rawMessage = data;
			int seperator;

			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int msgLen = data.Length;
			ReadOnlySpan<char> tagSegment = null;

			if (messageData[0] == '@') {
				seperator = messageData.IndexOf(' ');
				tagSegment = messageData[..seperator++];
				messageData = messageData[seperator..];
			}

			seperator = messageData.IndexOf('!');
			this.user = new string(messageData[1..seperator]);

			seperator = messageData.IndexOf(' ');
			this.sender = new string(messageData[1..seperator++]);
			messageData = messageData[seperator..];

			seperator = messageData.IndexOf(' '); // Skip Command as we already know
			messageData = messageData[++seperator..];

			seperator = messageData.IndexOf(' ');
			this.Channel = new string(messageData[1..seperator++]);

			this.message = new string(messageData[++seperator..]);

			if (tagSegment != null) {
				this.tags = new TAGS(ITagContainer.ExtractTags(tagSegment), this.message);
			}
			else {
				this.tags = new TAGS();
			}
		}

		public readonly BadgeDetails[] GetBadgeData(out string room_id) {
			if (string.IsNullOrWhiteSpace(this.tags.source_id)) {
				room_id = this.tags.room_id;
				return this.tags.badges ?? Array.Empty<BadgeDetails>();
			}
			else { 
				room_id = this.tags.source_room_id;
				return this.tags.source_badges ?? Array.Empty<BadgeDetails>();
			}
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);

		public readonly EmoteDetails[] GetEmoteData() => this.tags.emotes ?? Array.Empty<EmoteDetails>();
		public readonly bool CheckHasBadgesOrEmotes() => this.GetEmoteData().Length > 0 || this.GetBadgeData(out _).Length > 0;
		public readonly string GetColor() => this.tags.color;
		public ITagContainer GetTagContainer() => this.tags;



		[IRCCommandType(TwitchIRCCommand.PRIVMSG)]
		public readonly struct TAGS : ITagContainer {

			public readonly string badge_info;
			public readonly BadgeDetails[] badges;
			public readonly int bits;
			public readonly string color;
			public readonly string display_name;
			public readonly EmoteDetails[] emotes;
			public readonly string id;
			public readonly bool mod;
			public readonly string reply_parent_msg_id;
			public readonly string reply_parent_user_id;
			public readonly string reply_parent_user_login;
			public readonly string reply_parent_display_name;
			public readonly string reply_parent_msg_body;
			public readonly string reply_thread_parent_msg_id;
			public readonly string reply_thread_parent_user_login;
			public readonly string room_id;
			public readonly string source_badge_info;
			public readonly BadgeDetails[] source_badges;
			public readonly string source_id;
			public readonly bool source_only;
			public readonly string source_room_id;
			public readonly bool subscriber;
			public readonly long tmi_sent_ts;
			public readonly DateTime TimestampDate => TwitchStatic.TwitchUTCStart.AddMilliseconds(this.tmi_sent_ts);
			public readonly bool turbo;
			public readonly string user_id;
			public readonly string user_type;
			public readonly bool vip;

			public TAGS(JsonValue tags, string chatMessage) {
				bool managerExists = TwitchBadgeManager.GetInstance(out TwitchBadgeManager manager);
				TokenInstance ti = (managerExists && TwitchIRCClient.GetInstance(out TwitchIRCClient client)) ? client.IRCToken : null;

				this.badge_info = tags[TwitchIRCTags.PRIVMSG.BADGE_INFO].AsString;

				if (ITagContainer.ExtractBadges(tags[TwitchIRCTags.PRIVMSG.BADGES].AsString, out BadgeDetails[] badgeArray)) {
					this.badges = badgeArray;
				}
				else {
					this.badges = null;
				}

				this.bits = tags[TwitchIRCTags.PRIVMSG.BITS].AsInteger;
				this.color = tags[TwitchIRCTags.PRIVMSG.COLOR].AsString;
				this.display_name = tags[TwitchIRCTags.PRIVMSG.DISPLAY_NAME].AsString;

				if (ITagContainer.ExtractEmotes(tags[TwitchIRCTags.PRIVMSG.EMOTES].AsString, chatMessage, out EmoteDetails[] emoteArray)) {
					this.emotes = emoteArray;
				}
				else {
					this.emotes = null;
				}

				this.id = tags[TwitchIRCTags.PRIVMSG.ID].AsString;
				this.mod = tags[TwitchIRCTags.PRIVMSG.MOD].AsBoolean;
				this.reply_parent_msg_id = tags[TwitchIRCTags.PRIVMSG.REPLY_PARENT_MSG_ID].AsString;
				this.reply_parent_user_id = tags[TwitchIRCTags.PRIVMSG.REPLY_PARENT_USER_ID].AsString;
				this.reply_parent_user_login = tags[TwitchIRCTags.PRIVMSG.REPLY_PARENT_USER_LOGIN].AsString;
				this.reply_parent_display_name = tags[TwitchIRCTags.PRIVMSG.REPLY_PARENT_DISPLAY_NAME].AsString;
				this.reply_parent_msg_body = tags[TwitchIRCTags.PRIVMSG.REPLY_PARENT_MSG_BODY].AsString;
				this.reply_thread_parent_msg_id = tags[TwitchIRCTags.PRIVMSG.REPLY_THREAD_PARENT_MSG_ID].AsString;
				this.reply_thread_parent_user_login = tags[TwitchIRCTags.PRIVMSG.REPLY_THREAD_PARENT_USER_LOGIN].AsString;

				this.room_id = tags[TwitchIRCTags.PRIVMSG.ROOM_ID].AsString;
				if (!string.IsNullOrWhiteSpace(this.room_id) && managerExists) {
					manager.GetChannelBadges(this.room_id, false, ti);
				}

				if (ITagContainer.ExtractBadges(tags[TwitchIRCTags.PRIVMSG.SOURCE_BADGES].AsString, out BadgeDetails[] sourceBadgeArray)) {
					this.source_badges = sourceBadgeArray;
				}
				else {
					this.source_badges = null;
				}

				this.source_badge_info = tags[TwitchIRCTags.PRIVMSG.SOURCE_BADGE_INFO].AsString;
				this.source_id = tags[TwitchIRCTags.PRIVMSG.SOURCE_ID].AsString;
				this.source_only = tags[TwitchIRCTags.PRIVMSG.SOURCE_ONLY].AsBoolean;

				this.source_room_id = tags[TwitchIRCTags.PRIVMSG.SOURCE_ROOM_ID].AsString;
				if (!string.IsNullOrWhiteSpace(this.source_id) && !string.IsNullOrWhiteSpace(this.source_room_id) && managerExists) {
					manager.GetChannelBadges(this.source_room_id, false, ti);
				}

				this.subscriber = tags[TwitchIRCTags.PRIVMSG.SUBSCRIBER].AsBoolean;
				this.tmi_sent_ts = tags[TwitchIRCTags.PRIVMSG.TMI_SENT_TS].AsLong;
				this.turbo = tags[TwitchIRCTags.PRIVMSG.SUBSCRIBER].AsBoolean;
				this.user_id = tags[TwitchIRCTags.PRIVMSG.USER_ID].AsString;
				this.user_type = tags[TwitchIRCTags.PRIVMSG.USER_TYPE].AsString;
				this.vip = tags[TwitchIRCTags.PRIVMSG.VIP].AsBoolean;
			}
		}
	}
}
