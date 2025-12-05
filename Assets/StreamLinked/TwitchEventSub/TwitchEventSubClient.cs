using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ScoredProductions.StreamLinked.API;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.API.EventSub;
using ScoredProductions.StreamLinked.EventSub.Events;
using ScoredProductions.StreamLinked.EventSub.Events.Objects;
using ScoredProductions.StreamLinked.EventSub.ExtensionAttributes;
using ScoredProductions.StreamLinked.EventSub.Interfaces;
using ScoredProductions.StreamLinked.EventSub.WebSocketMessages;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;
using UnityEngine.Events;

namespace ScoredProductions.StreamLinked.EventSub {

	/// <summary>
	/// Twitch EventSub Singleton. Connects using WebSocket, Webhook not supported.
	/// </summary>
	[DefaultExecutionOrder(-0x2)]
	public class TwitchEventSubClient : SingletonDispatcher<TwitchEventSubClient> {

		public const string WebSocketAddress = "wss://eventsub.wss.twitch.tv/ws";

		[Range(0, 600), Tooltip("Adds keepalive_timeout_seconds to the websocket address, values less than 10 will disable this functionality.")]
		public int KeepaliveTimeoutSeconds = 0;

		/// <summary>
		/// Used to get the websocket address currently used by the client
		/// </summary>
		public string GetSocketAddress => WebSocketAddress
			+ (this.KeepaliveTimeoutSeconds >= 10 ? "?keepalive_timeout_seconds=" + this.KeepaliveTimeoutSeconds : "");

		/// <summary>
		/// 10 KB
		/// </summary>
		public const int BUFFER_SIZE = 10240;

		[SerializeField, HideInInspector, Tooltip("Token to use for the EventSub, if left blank it will default to the one provided to the API Client")]
		private TokenInstance eventSubToken;
		public TokenInstance EventSubToken
		{
			get => this.eventSubToken;
			set
			{
				if (this.eventSubToken != value) {
					this.eventSubToken = value;
					Task.Run(this.CloseWebsocket);
				}
			}
		}

		[HideInInspector]
		public ExtendedUnityEvent<AutoMessageHold> OnAutoMessageHold;
		[HideInInspector]
		public ExtendedUnityEvent<AutoMessageHoldV2> OnAutoMessageHoldV2;
		[HideInInspector]
		public ExtendedUnityEvent<AutoMessageUpdate> OnAutoMessageUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<AutoMessageUpdateV2> OnAutoMessageUpdateV2;
		[HideInInspector]
		public ExtendedUnityEvent<AutomodSettingsUpdate> OnAutomodSettingsUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<AutomodTermsUpdate> OnAutomodTermsUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelBitsUse> OnChannelBitsUse;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelUpdate> OnChannelUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelFollow> OnChannelFollow;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelAdBreakBegin> OnChannelAdBreakBegin;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatClear> OnChannelChatClear;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatClearUserMessages> OnChannelChatClearUserMessages;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatMessage> OnChannelChatMessage;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatMessageDelete> OnChannelChatMessageDelete;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatNotification> OnChannelChatNotification;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatSettingsUpdate> OnChannelChatSettingsUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatUserMessageHold> OnChannelChatUserMessageHold;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelChatUserMessageUpdate> OnChannelChatUserMessageUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSharedChatSessionBegin> OnChannelSharedChatSessionBegin;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSharedChatSessionUpdate> OnChannelSharedChatSessionUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSharedChatSessionEnd> OnChannelSharedChatSessionEnd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSubscribe> OnChannelSubscribe;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSubscriptionEnd> OnChannelSubscriptionEnd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSubscriptionGift> OnChannelSubscriptionGift;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSubscriptionMessage> OnChannelSubscriptionMessage;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelCheer> OnChannelCheer;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelRaid> OnChannelRaid;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelBan> OnChannelBan;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelUnban> OnChannelUnban;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelUnbanRequestCreate> OnChannelUnbanRequestCreate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelUnbanRequestResolve> OnChannelUnbanRequestResolve;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelModerate> OnChannelModerate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelModerateV2> OnChannelModerateV2;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelModeratorAdd> OnChannelModeratorAdd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelModeratorRemove> OnChannelModeratorRemove;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelGuestStarSessionBegin> OnChannelGuestStarSessionBegin;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelGuestStarSessionEnd> OnChannelGuestStarSessionEnd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelGuestStarGuestUpdate> OnChannelGuestStarGuestUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelGuestStarSettingsUpdate> OnChannelGuestStarSettingsUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPointsAutomaticRewardRedemption> OnChannelPointsAutomaticRewardRedemption;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPointsAutomaticRewardRedemptionV2> OnChannelPointsAutomaticRewardRedemptionV2;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPointsCustomRewardAdd> OnChannelPointsCustomRewardAdd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPointsCustomRewardUpdate> OnChannelPointsCustomRewardUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPointsCustomRewardRemove> OnChannelPointsCustomRewardRemove;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPointsCustomRewardRedemptionAdd> OnChannelPointsCustomRewardRedemptionAdd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPointsCustomRewardRedemptionUpdate> OnChannelPointsCustomRewardRedemptionUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPollBegin> OnChannelPollBegin;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPollProgress> OnChannelPollProgress;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPollEnd> OnChannelPollEnd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPredictionBegin> OnChannelPredictionBegin;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPredictionProgress> OnChannelPredictionProgress;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPredictionLock> OnChannelPredictionLock;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelPredictionEnd> OnChannelPredictionEnd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSuspiciousUserMessage> OnChannelSuspiciousUserMessage;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelSuspiciousUserUpdate> OnChannelSuspiciousUserUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelVIPAdd> OnChannelVIPAdd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelVIPRemove> OnChannelVIPRemove;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelWarningAcknowledge> OnChannelWarningAcknowledge;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelWarningSend> OnChannelWarningSend;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelCharityDonation> OnCharityDonation;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelCharityCampaignStart> OnCharityCampaignStart;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelCharityCampaignProgress> OnCharityCampaignProgress;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelCharityCampaignStop> OnCharityCampaignStop;
		[HideInInspector]
		public ExtendedUnityEvent<ConduitShardDisabled> OnConduitShardDisabled;
		[HideInInspector]
		public ExtendedUnityEvent<DropEntitlementGrant> OnDropEntitlementGrant;
		[HideInInspector]
		public ExtendedUnityEvent<ExtensionBitsTransactionCreate> OnExtensionBitsTransactionCreate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelGoalsBegin> OnGoalsBegin;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelGoalsProgress> OnGoalsProgress;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelGoalsEnd> OnGoalsEnd;
		[HideInInspector][Obsolete("Twitch has release V2 of this class and has marked it as Depreciated")]
		public ExtendedUnityEvent<ChannelHypeTrainBegin> OnHypeTrainBegin;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelHypeTrainBeginV2> OnHypeTrainBeginV2;
		[HideInInspector][Obsolete("Twitch has release V2 of this class and has marked it as Depreciated")]
		public ExtendedUnityEvent<ChannelHypeTrainProgress> OnHypeTrainProgress;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelHypeTrainProgressV2> OnHypeTrainProgressV2;
		[HideInInspector][Obsolete("Twitch has release V2 of this class and has marked it as Depreciated")]
		public ExtendedUnityEvent<ChannelHypeTrainEnd> OnHypeTrainEnd;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelHypeTrainEndV2> OnHypeTrainEndV2;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelShoutoutCreate> OnShoutoutCreate;
		[HideInInspector]
		public ExtendedUnityEvent<ChannelShoutoutReceived> OnShoutoutReceived;
		[HideInInspector]
		public ExtendedUnityEvent<StreamOnline> OnStreamOnline;
		[HideInInspector]
		public ExtendedUnityEvent<StreamOffline> OnStreamOffline;
		[HideInInspector]
		public ExtendedUnityEvent<UserAuthorizationGrant> OnUserAuthorizationGrant;
		[HideInInspector]
		public ExtendedUnityEvent<UserAuthorizationRevoke> OnUserAuthorizationRevoke;
		[HideInInspector]
		public ExtendedUnityEvent<UserUpdate> OnUserUpdate;
		[HideInInspector]
		public ExtendedUnityEvent<WhisperReceived> OnWhisperReceived;

		[HideInInspector]
		public ExtendedUnityEvent<Subscription> OnUserRemovedSubscriptionRevoked;
		[HideInInspector]
		public ExtendedUnityEvent<Subscription> OnAuthorizationRemoved;
		[HideInInspector]
		public ExtendedUnityEvent<Subscription> OnVersionRemovedSubscriptionRevoked;

		public static bool EventSubConnectionActive => GetInstance(out TwitchEventSubClient client) && client.ConnectionActive;
		public bool ConnectionActive => this.webSocket?.State == WebSocketState.Open && this.CurrentSessionState.HasValue;

		public static bool EventSubStartingUp => GetInstance(out TwitchEventSubClient client) && client.StartingUp;
		public bool StartingUp => this.StartUpTask != null && !this.StartUpTask.IsCompleted;

		public List<Subscription> GetSubscriptions => new List<Subscription>(this.SessionSubscriptions.Values);

		public WebSocketState SocketState => this.webSocket?.State ?? WebSocketState.None;

		[SerializeField]
		private bool persistBetweenScenes = true;
		public override bool PersistBetweenScenes => this.persistBetweenScenes;

		public bool PostKeepAliveToLog = false;

		public int UsedCost { get; private set; } = 0;
		public int KnownMaxTotalCost { get; private set; } = 0;
		public Session? CurrentSessionState { get; private set; }

		public Transport SessionTransport { get; private set; }

		// Webhooks are only in ASP.NET so not compatible without external library
		private ClientWebSocket webSocket;
		private Task recieverThread;
		private ClientWebSocket webSocketReconnector;
		private Task recieverThreadReconnector;

		private readonly byte[] buffer = new byte[BUFFER_SIZE];

		private readonly Dictionary<string, Subscription> SessionSubscriptions = new Dictionary<string, Subscription>();

		private Task StartUpTask;

		private CancellationTokenSource cts = new CancellationTokenSource();

		private bool reconnecting = false;

		private TwitchAPIClient apiClient;

		private readonly HashSet<string> receivedMessageIds = new HashSet<string>();

		protected override void Awake() {
			if (this.EstablishSingleton(true)) { }
		}

		private void OnDisable() {
			this.CloseWebsocket().ConfigureAwait(false);
		}

		private void OnDestroy() {
			FieldInfo[] classFields = typeof(TwitchEventSubClient).GetFields();
			for (int x = 0; x < classFields.Length; x++) {
				FieldInfo fieldInfo = classFields[x];
				if (typeof(UnityEventBase).IsAssignableFrom(fieldInfo.FieldType)) {
					object field = fieldInfo.GetValue(this);
					if (field != null) {
						UnityEventBase baseEvent = (UnityEventBase)field;
						baseEvent.RemoveAllListeners();
					}
				}
			}
		}

		private void EndCancelTokens() {
			if (this.cts != null) {
				try {
					this.cts.Cancel();
					this.cts.Dispose();
					this.cts = null;
				} catch {
					if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
						DebugManager.LogMessage("EventSub cancel token was ended.".RichTextColour("orange"));
					}
				}
			}
		}

		/// <summary>
		/// Ends current websocket session
		/// </summary>
		/// <returns></returns>
		public async Task CloseWebsocket() {
			if (this.webSocket != null && this.webSocket.State == WebSocketState.Open) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("Closing down EventSub websocket connection.");
				}

				await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Functionality Ending", this.cts.Token);
			}
			this.webSocket?.Dispose();

			this.receivedMessageIds.Clear();
			this.CurrentSessionState = null;

			this.StartUpTask?.Dispose();

			this.EndCancelTokens();
		}

		/// <summary>
		/// Returns a copy of the current sessions subscriptions
		/// </summary>
		public Subscription[] GetCurrentSubscriptions() {
			return this.SessionSubscriptions.Values.ToArray();
		}

		/// <summary>
		/// Get subscription data by ID
		/// </summary>
		public bool GetSubscription(string subscriptionId, out Subscription sub) {
			return this.SessionSubscriptions.TryGetValue(subscriptionId, out sub);
		}

		public IEnumerator BeginConnectionSession(bool restart = false, bool resubscribe = false, params (TwitchEventSubSubscriptionsEnum, Condition)[] immedieteSubs) {
			yield return this.BeginConnectionSessionAsync(restart, resubscribe, immedieteSubs).YieldTask();
		}

		/// <summary>
		/// Starts up the Event sub
		/// </summary>
		public Task BeginConnectionSessionAsync() {
			return this.BeginConnectionSessionAsync(false, false);
		}

		/// <summary>
		/// Starts up the Event sub
		/// </summary>
		/// <param name="restart">If you want to force restart the server</param>
		/// <param name="resubscribe">If you want to resubscribe to existing subscriptions</param>
		public async Task BeginConnectionSessionAsync(bool restart = false, bool resubscribe = false, params (TwitchEventSubSubscriptionsEnum, Condition)[] immedieteSubs) {
			this.cts ??= new CancellationTokenSource();

			if (!TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"TwitchEventSubClient; TwitchAPIClient not found, Startup cancelled");
					return;
				}
			}

			if (!client.CheckOAuthExistsAndInDate(this.EventSubToken)) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"TwitchEventSubClient; Token not ready, waiting.");
				}
				do {
					await Task.Delay(1000);
					if (this.cts.IsCancellationRequested) {
						return;
					}
				} while (!client.CheckOAuthExistsAndInDate(this.EventSubToken));
			}

			if (restart || !EventSubConnectionActive) {
				this.CurrentSessionState = null;

				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"TwitchEventSubClient; {(restart ? "restarting" : "starting")} connection");
				}
				if (this.StartUpTask == null || this.StartUpTask.IsCompleted) {
					this.StartUpTask = this.BuildSocketAndThreadAsync();
				}
			}

			await this.StartUpTask;

			// Hold until connection has been made and ready to receive
			while (!this.CurrentSessionState.HasValue) {
				await Task.Delay(1000);
				if (this.cts.IsCancellationRequested) {
					return;
				}
			}

			if (this.cts.IsCancellationRequested) {
				return;
			}

			if (resubscribe) {
				await this.ResubscribeToSessionEvents();
			}

			if (this.cts.IsCancellationRequested) {
				return;
			}

			// Subs not already subscribed to, to then sub too
			foreach ((TwitchEventSubSubscriptionsEnum, Condition) sub in immedieteSubs) {
				if (this.cts.IsCancellationRequested) {
					return;
				}

				await this.SubscribeToEvent(sub.Item1, sub.Item2);
			}
		}


		/// <summary>
		/// Builds the WebSocket and the thread that will receive the data
		/// </summary>
		private async Task BuildSocketAndThreadAsync() {
			try {
				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage("TwitchEventSubClient; build socket and thread");
				}
				if (this.webSocket != null && this.webSocket.State == WebSocketState.Open) {
					await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket rebuild requested", this.cts.Token);
					this.webSocket.Dispose();
				}

				this.webSocket = await this.BuildNewConnectionAsync(this.GetSocketAddress);

				if (this.recieverThread != null && !this.recieverThread.IsCompleted) {
					this.EndCancelTokens();
					this.cts = new CancellationTokenSource();
				}
				this.recieverThread = this.ManageResponseAsync(this.webSocket);
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
			}
		}

		/// <summary>
		/// Builds a new websocket object
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		private async Task<ClientWebSocket> BuildNewConnectionAsync(string uri) {
			try {
				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage("TwitchEventSubClient; building connection");
				}
				ClientWebSocket ws = new ClientWebSocket();
				ws.Options.SetBuffer(BUFFER_SIZE, 100);
				await ws.ConnectAsync(new Uri(uri), this.cts.Token);

				return ws;
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				throw;
			}
		}

		/// <summary>
		/// Websocket response reading Task (Warning: long term task)
		/// </summary>
		/// <param name="socketReference"></param>
		/// <returns></returns>
		private async Task ManageResponseAsync(ClientWebSocket socketReference) {
			if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
				DebugManager.LogMessage("TwitchEventSubClient; building response thread");
			}
			while (socketReference.State == WebSocketState.Open && !this.cts.IsCancellationRequested) {
				using (MemoryStream ms = new MemoryStream()) {
					WebSocketReceiveResult result;
					do {
						result = await socketReference.ReceiveAsync(this.buffer, this.cts.Token);
						if (this.cts.IsCancellationRequested) {
							goto Close;
						}
						ms.Write(this.buffer, 0, result.Count);
					}
					while (!result.EndOfMessage);

					ms.Seek(0, SeekOrigin.Begin);

					if (result.MessageType == WebSocketMessageType.Text) {
						using (StreamReader reader = new StreamReader(ms, Encoding.UTF8)) {
							await this.ParseSocketMessageAsync(reader.ReadToEnd());
						}
					}
					else if (result.MessageType == WebSocketMessageType.Close) {
						await socketReference.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
					}
				}
			}
			Close:
			await socketReference.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
		}

		/// <summary>
		/// Parse json data received into an Object and process required action from data
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task ParseSocketMessageAsync(string message) {
			JsonValue parsedMessage = JsonReader.Parse(message);
			SocketResponse response;
			try {
				response = new SocketResponse(parsedMessage);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
				return;
			}
			if (response.metadata.message_type == TwitchWords.SESSION_KEEPALIVE) {
				// TODO : keepalive_timeout_seconds expired (Session class) 
				if (this.PostKeepAliveToLog && this.LogDebugLevel > DebugManager.DebugLevel.None) {
					JsonValue json = new JsonObject() {
							{ TwitchWords.MESSAGE, TwitchWords.SESSION_KEEPALIVE },
							{ TwitchWords.MESSAGE_TIMESTAMP, response.metadata.message_timestamp }
						};
					DebugManager.LogMessage("TwitchEventSubClient; Message Received: " + json.ToString().RichTextItalic());
				}
			}
			else {
				switch (this.LogDebugLevel) {
					case DebugManager.DebugLevel.Necessary:
						JsonValue json = new JsonObject() {
							{ TwitchWords.MESSAGE, response.metadata.subscription_type },
							{ TwitchWords.MESSAGE_TIMESTAMP, response.metadata.message_timestamp }
						};
						DebugManager.LogMessage("TwitchEventSubClient; Message Received: " + json.ToString().RichTextItalic());
						break;
					case DebugManager.DebugLevel.Normal:
						DebugManager.LogMessage("TwitchEventSubClient; Message Received: " + response.metadata.ToJSON().ToString().RichTextItalic());
						break;
					case DebugManager.DebugLevel.Full:
						DebugManager.LogMessage("TwitchEventSubClient; Message Received: " + message.RichTextItalic());
						break;
				}

				if (!this.receivedMessageIds.Contains(response.metadata.message_id)) {
					this.receivedMessageIds.Add(response.metadata.message_id);

					switch (response.metadata.message_type) {
						case TwitchWords.SESSION_WELCOME: // First Connection
							this.CurrentSessionState = response.payload.session;
							if (this.reconnecting) {
								await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnection", this.cts.Token);
								this.recieverThread?.Dispose();
								this.webSocket = this.webSocketReconnector;
								this.recieverThread = this.recieverThreadReconnector;
							}
							this.SessionTransport = new Transport() {
								method = "websocket",
								session_id = this.CurrentSessionState?.id
							};

							this.reconnecting = false;
							break;
						case TwitchWords.NOTIFICATION: // Events
							void invoke() { this.FireEventDelegate(response.payload.eventData); }

							MainThreadDispatchQueue.Enqueue(invoke);
							break;
						case TwitchWords.SESSION_RECONNECT: // Notified to reconnect
							this.CurrentSessionState = response.payload.session;
							this.webSocketReconnector = await this.BuildNewConnectionAsync(response.payload.session.reconnect_url);
							this.recieverThreadReconnector = this.ManageResponseAsync(this.webSocket);
							this.reconnecting = true;
							break;
						case TwitchWords.REVOCATION: // Subscription removed by Twitch
							switch (response.payload.subscription.status) {
								case TwitchWords.USER_REMOVED: // User deleted
									MainThreadDispatchQueue.Enqueue(() => this.OnUserRemovedSubscriptionRevoked?.Invoke(response.payload.subscription));
									break;
								case TwitchWords.AUTHORIZATION_REVOKED: // Auth to connect was invalidated
									this.SessionSubscriptions.Remove(response.payload.subscription.id);
									MainThreadDispatchQueue.Enqueue(() => this.OnAuthorizationRemoved?.Invoke(response.payload.subscription));
									break;
								case TwitchWords.VERSION_REMOVED: // Subscription type or version is no longer supported
									MainThreadDispatchQueue.Enqueue(() => this.OnVersionRemovedSubscriptionRevoked?.Invoke(response.payload.subscription));
									break;
							}
							break;
					}
				}
			}
		}

		private void FireEventDelegate(IEvent builtEvent) {
			Type thisType = this.GetType();
			Type eventBody = builtEvent.GetType();

			FieldInfo foundEventField = thisType.GetField($"On{eventBody.Name}");

			if (foundEventField == null) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("EventSub Event not found for body of type: " + eventBody.Name, DebugManager.ErrorLevel.Error);
				}
				return;
			}

			object unityEvent = foundEventField.GetValue(this);
			MethodInfo invokeMethod = unityEvent.GetType().GetMethod("Invoke");
			invokeMethod.Invoke(unityEvent, new object[] { Convert.ChangeType(builtEvent, eventBody) });
		}

		public async Task SubscribeToEvent<T>(Condition condition, APIScopeWarning scopeCheck = APIScopeWarning.WarnOnMissing) where T : IEvent, new() {
			if (this.apiClient == null && !TwitchAPIClient.GetInstance(out this.apiClient)) {
				DebugManager.LogMessage($"TwitchEventSubClient; Twitch API Client was not found, request aborted.", DebugManager.ErrorLevel.Error);
				return;
			}

			if (!EventSubConnectionActive) {
				DebugManager.LogMessage($"TwitchEventSubClient; EventSub is not currently connected to Twitch, please connect and try again.", DebugManager.ErrorLevel.Error);
				return;
			}

			TokenInstance credentials = this.EventSubToken;
			if (credentials == null) {
				if (this.apiClient.DefaultAPIToken == null) {
					DebugManager.LogMessage($"TwitchEventSubClient; No credentials were found to make the subscription request, request aborted.", DebugManager.ErrorLevel.Error);
					return;
				}
				credentials = this.apiClient.DefaultAPIToken;
			}
			
			T eventBody = new T();
			credentials.PerformScopeCheck(scopeCheck, eventBody);
			string eventType = eventBody.Enum.ToTwitchNameString();
			
			string JSON = CreateEventSubSubscription.BuildDataJson(
					eventType,
					eventBody.Enum.ToVersionString(),
					condition,
					this.SessionTransport
				);

			TwitchAPIDataContainer<CreateEventSubSubscription> returnedData = await TwitchAPIClient.MakeTwitchAPIRequestAsync<CreateEventSubSubscription>(
				credentials,
				Body: JSON,
				cancelToken: this.cts.Token);
			if (returnedData.HasErrored) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"TwitchEventSubClient; Failed to subscribe event; {{{eventType}}} with conditions; {JsonWriter.Serialize(condition)}. Error message: {{{returnedData.ErrorText}}}", DebugManager.ErrorLevel.Error);
				}
			}
			else {
				Subscription createdSub = new Subscription(returnedData.data[0]);
				this.SessionSubscriptions[createdSub.id] = createdSub;

				if (returnedData.EventSubData.max_total_cost > int.MinValue) {
					this.KnownMaxTotalCost = returnedData.EventSubData.max_total_cost;
				}
				if (returnedData.EventSubData.total_cost > int.MinValue) {
					this.UsedCost = returnedData.EventSubData.total_cost;
				}
			}
		}

		public Task SubscribeToEvent(string type, Condition condition) {
			TwitchEventSubSubscriptionsEnum enumValue = type.GetEnumFromTwitchName();
			return this.SubscribeToEvent(enumValue, condition);
		}

		public Task SubscribeToEvent(TwitchEventSubSubscriptionsEnum type, Condition condition) {
			try {
				Type t = typeof(TwitchEventSubClient);
				string methodName = nameof(SubscribeToEvent);
				foreach (MethodInfo a in t.GetMethods()) {
					if (a.ContainsGenericParameters && a.Name == methodName) {
						return (Task)a.MakeGenericMethod(type.ToLinkedType()).Invoke(this, new object[] { condition, APIScopeWarning.None });
					}
				}
				throw new Exception("No method found to execute subscription.");
			} catch (Exception ex) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				return Task.CompletedTask;
			}
		}

		/// <summary>
		/// Closes and resubscribes current subscriptions / Uses stored subscriptions to resubscribe
		/// </summary>
		/// <returns></returns>
		public async Task ResubscribeToSessionEvents() {
			foreach (Subscription sub in this.SessionSubscriptions.Values.ToArray()) {
				if (this.cts.IsCancellationRequested) {
					return;
				}
				this.SessionSubscriptions.Remove(sub.id);
				await this.SubscribeToEvent(sub.type, sub.condition);
			}
		}

		/// <summary>
		/// Makes the EventSub unsubscribe from the registered subscriptions
		/// </summary>
		/// <param name="Ids">Subscriptions</param>
		public void UnsubscribeFromEvents(params string[] Ids) {
			if (Ids.Length == 0) {
				return;
			}
			if (this.apiClient == null && TwitchAPIClient.GetInstance(out this.apiClient)) {
				DebugManager.LogMessage($"TwitchEventSubClient; Twitch API Client was not found, request aborted.", DebugManager.ErrorLevel.Error);
				return;
			}

			TokenInstance credentials = this.EventSubToken;
			if (credentials == null) {
				if (this.apiClient.DefaultAPIToken == null) {
					DebugManager.LogMessage($"TwitchEventSubClient; No credentials were found to make the subscription request, request aborted.", DebugManager.ErrorLevel.Error);
					return;
				}
				credentials = this.apiClient.DefaultAPIToken;
			}

			foreach (string id in Ids) {
				if (this.SessionSubscriptions.ContainsKey(id)) {
					this.StartCoroutine(TwitchAPIClient.MakeTwitchAPIRequest<DeleteEventSubSubscription>(
						credentials,
						QueryParameters: new (string, string)[] {
							(DeleteEventSubSubscription.ID, id)
						},
						SuccessCallback: r => {
							this.SessionSubscriptions.Remove(id);
						}
					));
				}
			}
		}
	}
}
