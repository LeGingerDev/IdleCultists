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
	/// <see href="https://dev.twitch.tv/docs/chat/irc/#usernotice-command">Twitch IRC Command</see>: Sent when events occur in the channel, such as someone subscribing to the channel.
	/// </summary>
	[IRCCommandType(TwitchIRCCommand.USERNOTICE)]
	public readonly struct USERNOTICE : ISenderMessage, IParsedMessage, ITagMessage<USERNOTICE.TAGS> {

		//public const string PROTOTYPE = "<tags> :tmi.twitch.tv USERNOTICE #<channel> :<message>";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.USERNOTICE;

		private readonly string sender;
		public string Sender => this.sender;

		private readonly string message;
		/// <summary>
		/// Optional
		/// </summary>
		public readonly string Message => this.message;

		public readonly string Channel;

		private readonly TAGS tags;
		public readonly TAGS Tags => this.tags;

		public USERNOTICE(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();
			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int seperator;
			ReadOnlySpan<char> tagSegment = null;

			if (messageData[0] == '@') {
				seperator = messageData.IndexOf(' ');
				tagSegment = messageData[..seperator++];
				messageData = messageData[seperator..];
			}

			seperator = messageData.IndexOf(' ');
			this.sender = new string(messageData[..seperator++]);
			messageData = messageData[seperator..];

			seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];

			seperator = messageData.IndexOf(' ');
			if (seperator == -1) {
				this.Channel = new string(messageData);
				this.message = string.Empty;
			}
			else {
				this.Channel = new string(messageData[..seperator++]);
				messageData = messageData[seperator..];
				this.message = new string(messageData[1..]);
			}

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

		

		[IRCCommandType(TwitchIRCCommand.USERNOTICE)]
		public readonly struct TAGS : ITagContainer {

			public readonly string badge_info;
			public readonly BadgeDetails[] badges;
			public readonly string color;
			public readonly string display_name;
			public readonly EmoteDetails[] emotes;
			public readonly string id;
			public readonly string login;
			public readonly bool mod;
			public readonly string msg_id;
			public readonly MsgIDEnum MessageID => Enum.TryParse(this.msg_id, out MsgIDEnum value) ? value : MsgIDEnum.OTHER;
			public readonly string room_id;
			public readonly BadgeDetails[] source_badges;
			public readonly string source_badge_info;
			public readonly string source_id;
			public readonly string source_room_id;
			public readonly bool subscriber;
			public readonly string system_msg;
			public readonly long tmi_sent_ts;
			public readonly DateTime TimestampDate => TwitchStatic.TwitchUTCStart.AddMilliseconds(this.tmi_sent_ts);
			public readonly bool turbo;
			public readonly string user_id;
			public readonly string user_type;

			public readonly int msg_param_cumulative_months;
			public readonly string msg_param_displayName;
			public readonly string msg_param_login;
			public readonly int msg_param_months;
			public readonly int msg_param_promo_gift_total;
			public readonly string msg_param_promo_name;
			public readonly string msg_param_recipient_display_name;
			public readonly string msg_param_recipient_id;
			public readonly string msg_param_recipient_user_name;
			public readonly string msg_param_sender_login;
			public readonly string msg_param_sender_name;
			public readonly bool msg_param_should_share_streak;
			public readonly int msg_param_streak_months;
			public readonly string msg_param_sub_plan;
			public readonly string msg_param_sub_plan_name;
			public readonly int msg_param_viewerCount;
			public readonly string msg_param_threshold;
			public readonly int msg_param_gift_months;

			public enum MsgIDEnum : byte {
				sub = 0,
				resub = 1,
				subgift = 2,
				submysterygift = 3,
				giftpaidupgrade = 4,
				rewardgift = 5,
				anongiftpaidupgrade = 6,
				raid = 7,
				unraid = 8,
				ritual = 9,
				bitsbadgetier = 10,
				sharedchatnotice = 11,
				announcment = 12,
				OTHER = 13 // Added later, value 13 to not disrupt backwards compatibility
			}

			public TAGS(JsonValue tags, string chatMessage) {
				bool managerExists = TwitchBadgeManager.GetInstance(out TwitchBadgeManager manager);
				TokenInstance ti = (managerExists && TwitchIRCClient.GetInstance(out TwitchIRCClient client)) ? client.IRCToken : null;

				this.badge_info = tags[TwitchIRCTags.USERNOTICE.BADGE_INFO].AsString;

				if (ITagContainer.ExtractBadges(tags[TwitchIRCTags.USERNOTICE.BADGES].AsString, out BadgeDetails[] badgeArray)) {
					this.badges = badgeArray;
				}
				else {
					this.badges = null;
				}

				this.color = tags[TwitchIRCTags.USERNOTICE.COLOR].AsString;
				this.display_name = tags[TwitchIRCTags.USERNOTICE.DISPLAY_NAME].AsString;

				if (ITagContainer.ExtractEmotes(tags[TwitchIRCTags.USERNOTICE.EMOTES].AsString, chatMessage, out EmoteDetails[] emoteArray)) {
					this.emotes = emoteArray;
				}
				else {
					this.emotes = null;
				}

				this.id = tags[TwitchIRCTags.USERNOTICE.ID].AsString;
				this.login = tags[TwitchIRCTags.USERNOTICE.LOGIN].AsString;
				this.mod = tags[TwitchIRCTags.USERNOTICE.MOD].AsBoolean;
				this.msg_id = tags[TwitchIRCTags.USERNOTICE.MSG_ID].AsString;

				this.room_id = tags[TwitchIRCTags.USERNOTICE.ROOM_ID].AsString;
				if (!string.IsNullOrWhiteSpace(this.room_id) && managerExists) {
					manager.GetChannelBadges(this.room_id, false, ti);
				}

				if (ITagContainer.ExtractBadges(tags[TwitchIRCTags.USERNOTICE.SOURCE_BADGES].AsString, out BadgeDetails[] sourceBadgeArray)) {
					this.source_badges = sourceBadgeArray;
				}
				else {
					this.source_badges = null;
				}

				this.source_badge_info = tags[TwitchIRCTags.USERNOTICE.SOURCE_BADGE_INFO].AsString;
				this.source_id = tags[TwitchIRCTags.USERNOTICE.SOURCE_ID].AsString;

				this.source_room_id = tags[TwitchIRCTags.USERNOTICE.SOURCE_ROOM_ID].AsString;
				if (!string.IsNullOrWhiteSpace(this.source_id) && !string.IsNullOrWhiteSpace(this.source_room_id) && managerExists) {
					manager.GetChannelBadges(this.source_room_id, false, ti);
				}

				this.subscriber = tags[TwitchIRCTags.USERNOTICE.SUBSCRIBER].AsBoolean;
				this.system_msg = tags[TwitchIRCTags.USERNOTICE.SYSTEM_MSG].AsString;
				this.tmi_sent_ts = tags[TwitchIRCTags.USERNOTICE.TMI_SENT_TS].AsLong;
				this.turbo = tags[TwitchIRCTags.USERNOTICE.TURBO].AsBoolean;
				this.user_id = tags[TwitchIRCTags.USERNOTICE.USER_ID].AsString;
				this.user_type = tags[TwitchIRCTags.USERNOTICE.USER_TYPE].AsString;

				this.msg_param_cumulative_months = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_CUMULATIVE_MONTHS].AsInteger;
				this.msg_param_displayName = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_DISPLAYNAME].AsString;
				this.msg_param_login = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_LOGIN].AsString;
				this.msg_param_months = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_MONTHS].AsInteger;
				this.msg_param_promo_gift_total = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_PROMO_GIFT_TOTAL].AsInteger;
				this.msg_param_promo_name = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_PROMO_NAME].AsString;
				this.msg_param_recipient_display_name = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_RECIPIENT_DISPLAY_NAME].AsString;
				this.msg_param_recipient_id = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_RECIPIENT_ID].AsString;
				this.msg_param_recipient_user_name = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_RECIPIENT_USER_NAME].AsString;
				this.msg_param_sender_login = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_SENDER_LOGIN].AsString;
				this.msg_param_sender_name = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_SENDER_NAME].AsString;
				this.msg_param_should_share_streak = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_SHOULD_SHARE_STREAK].AsBoolean;
				this.msg_param_streak_months = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_STREAK_MONTHS].AsInteger;
				this.msg_param_sub_plan = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_SUB_PLAN].AsString;
				this.msg_param_sub_plan_name = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_SUB_PLAN_NAME].AsString;
				this.msg_param_viewerCount = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_VIEWERCOUNT].AsInteger;
				this.msg_param_threshold = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_THRESHOLD].AsString;
				this.msg_param_gift_months = tags[TwitchIRCTags.USERNOTICE.MSG_PARAM_GIFT_MONTHS].AsInteger;
			}
		}
	}
}
