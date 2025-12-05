namespace ScoredProductions.StreamLinked.API {

	/// <summary>
	/// All addresses used by Twitch API
	/// </summary>
	public static class TwitchAPILinks {
		public const string TwitchAPIWebsite = "https://api.twitch.tv/helix/";

		public const string StartCommercial = TwitchAPIWebsite + "channels/commercial";
		public const string GetAdSchedule = TwitchAPIWebsite + "channels/ads";
		public const string SnoozeNextAd = TwitchAPIWebsite + "channels/ads/schedule/snooze";
		public const string GetExtensionAnalytics = TwitchAPIWebsite + "analytics/extensions";
		public const string GetGameAnalytics = TwitchAPIWebsite + "analytics/games";
		public const string GetBitsLeaderboard = TwitchAPIWebsite + "bits/leaderboard";
		public const string GetCheermotes = TwitchAPIWebsite + "bits/cheermotes";
		public const string GetExtensionTransactions = TwitchAPIWebsite + "extensions/transactions";
		public const string ChannelInformation = TwitchAPIWebsite + "channels";
		public const string GetChannelEditors = TwitchAPIWebsite + "channels/editors";
		public const string GetFollowedChannels = TwitchAPIWebsite + "channels/followed";
		public const string GetChannelFollowers = TwitchAPIWebsite + "channels/followers";
		public const string CustomRewards = TwitchAPIWebsite + "channel_points/custom_rewards";
		public const string CustomRewardRedemption = TwitchAPIWebsite + "channel_points/custom_rewards/redemptions";
		public const string GetCharityCampaign = TwitchAPIWebsite + "charity/campaigns";
		public const string GetCharityCampaignDonations = TwitchAPIWebsite + "charity/donations";
		public const string GetChatters = TwitchAPIWebsite + "chat/chatters";
		public const string GetUserEmotes = TwitchAPIWebsite + "chat/emotes/user";
		public const string GetChannelEmotes = TwitchAPIWebsite + "chat/emotes";
		public const string GetGlobalEmotes = TwitchAPIWebsite + "chat/emotes/global";
		public const string GetEmoteSets = TwitchAPIWebsite + "chat/emotes/set";
		public const string GetChannelChatBadges = TwitchAPIWebsite + "chat/badges";
		public const string GetGlobalChatBadges = TwitchAPIWebsite + "chat/badges/global";
		public const string ChatSettings = TwitchAPIWebsite + "chat/settings";
		public const string SendChatAnnouncement = TwitchAPIWebsite + "chat/announcements";
		public const string SendAShoutout = TwitchAPIWebsite + "chat/shoutouts";
		public const string UserChatColor = TwitchAPIWebsite + "chat/color";
		public const string Clips = TwitchAPIWebsite + "clips";
		public const string ClipsDownloads = Clips + "/downloads";
		public const string GetContentClassificationLabels = TwitchAPIWebsite + "content_classification_labels";
		public const string DropsEntitlements = TwitchAPIWebsite + "entitlements/drops";
		public const string ExtensionConfigurationSegment = TwitchAPIWebsite + "extensions/configurations";
		public const string SetExtensionRequiredConfiguration = TwitchAPIWebsite + "extensions/required_configuration";
		public const string SendExtensionPubSubMessage = TwitchAPIWebsite + "extensions/pubsub";
		public const string GetExtensionLiveChannels = TwitchAPIWebsite + "extensions/live";
		public const string ExtensionSecrets = TwitchAPIWebsite + "extensions/jwt/secrets";
		public const string SendExtensionChatMessage = TwitchAPIWebsite + "extensions/chat";
		public const string GetExtensions = TwitchAPIWebsite + "extensions";
		public const string GetReleasedExtensions = TwitchAPIWebsite + "extensions/released";
		public const string ExtensionBitsProducts = TwitchAPIWebsite + "bits/extensions";
		public const string EventSubSubscription = TwitchAPIWebsite + "eventsub/subscriptions";
		public const string GetTopGames = TwitchAPIWebsite + "games/top";
		public const string GetGames = TwitchAPIWebsite + "games";
		public const string GetCreatorGoals = TwitchAPIWebsite + "goals";
		public const string ChannelGuestStarSettings = TwitchAPIWebsite + "guest_star/channel_settings";
		public const string GuestStarSession = TwitchAPIWebsite + "guest_star/session";
		public const string GuestStarInvites = TwitchAPIWebsite + "guest_star/invites";
		public const string GuestStarSlot = TwitchAPIWebsite + "guest_star/slot";
		public const string UpdateGuestStarSlotSettings = TwitchAPIWebsite + "guest_star/slot_settings";
		public const string GetHypeTrainEvents = TwitchAPIWebsite + "hypetrain/events";
		public const string GetHypeTrainStatus = TwitchAPIWebsite + "hypetrain/status";
		public const string CheckAutoModStatus = TwitchAPIWebsite + "moderation/enforcements/status";
		public const string ManageHeldAutoMessages = TwitchAPIWebsite + "moderation/automod/message";
		public const string AutoModSettings = TwitchAPIWebsite + "moderation/automod/settings";
		public const string GetBannedUsers = TwitchAPIWebsite + "moderation/banned";
		public const string BanUser = TwitchAPIWebsite + "moderation/bans";
		public const string BlockedTerms = TwitchAPIWebsite + "moderation/blocked_terms";
		public const string DeleteChatMessages = TwitchAPIWebsite + "moderation/chat";
		public const string Moderators = TwitchAPIWebsite + "moderation/moderators";
		public const string WarnUser = TwitchAPIWebsite + "moderation/warnings";
		public const string VIPs = TwitchAPIWebsite + "channels/vips";
		public const string ShieldModeStatus = TwitchAPIWebsite + "moderation/shield_mode";
		public const string Polls = TwitchAPIWebsite + "polls";
		public const string Predictions = TwitchAPIWebsite + "predictions";
		public const string Raids = TwitchAPIWebsite + "raids";
		public const string GetChannelStreamSchedule = TwitchAPIWebsite + "schedule";
		public const string GetChanneliCalendar = TwitchAPIWebsite + "schedule/icalendar";
		public const string UpdateChannelStreamSchedule = TwitchAPIWebsite + "schedule/settings";
		public const string ChannelStreamScheduleSegment = TwitchAPIWebsite + "schedule/segment";
		public const string SearchCategories = TwitchAPIWebsite + "search/categories";
		public const string SearchChannels = TwitchAPIWebsite + "search/channels";
		public const string GetStreamKey = TwitchAPIWebsite + "streams/key";
		public const string GetStreams = TwitchAPIWebsite + "streams";
		public const string GetFollowedStreams = TwitchAPIWebsite + "streams/followed";
		public const string CreateStreamMarker = TwitchAPIWebsite + "streams/markers";
		public const string GetStreamMarkers = TwitchAPIWebsite + "streams/markers";
		public const string GetBroadcasterSubscriptions = TwitchAPIWebsite + "subscriptions";
		public const string CheckUserSubscription = TwitchAPIWebsite + "subscriptions/user";
		public const string GetChannelTeams = TwitchAPIWebsite + "teams/channel";
		public const string GetTeams = TwitchAPIWebsite + "teams";
		public const string Users = TwitchAPIWebsite + "users";
		public const string UsersAuthorization = TwitchAPIWebsite + "authorization/users";
		public const string UnbanRequests = TwitchAPIWebsite + "moderation/unban_requests";
		public const string UserBlockList = TwitchAPIWebsite + "users/blocks";
		public const string GetUserExtensions = TwitchAPIWebsite + "users/extensions/list";
		public const string UserActiveExtensions = TwitchAPIWebsite + "users/extensions";
		public const string Videos = TwitchAPIWebsite + "videos";
		public const string SendWhisper = TwitchAPIWebsite + "whispers";
		public const string GetModeratedChannels = TwitchAPIWebsite + "moderation/channels";
		public const string Conduits = TwitchAPIWebsite + "eventsub/conduits";
		public const string ConduitShards = TwitchAPIWebsite + "eventsub/conduits/shards";
		public const string SendChatMessage = TwitchAPIWebsite + "chat/messages";
		public const string Session = TwitchAPIWebsite + "shared_chat/session";

		// Non Reference page links
		public const string GetOAuth2Link = "https://id.twitch.tv/oauth2/";
		public const string GetAuthToken = GetOAuth2Link + "token";
		public const string GetAuthData = GetOAuth2Link + "authorize";
		public const string GetTokenValidation = GetOAuth2Link + "validate";
		public const string GetDeviceToken = GetOAuth2Link + "device";
	}
}