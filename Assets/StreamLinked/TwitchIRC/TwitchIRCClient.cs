using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ScoredProductions.StreamLinked.API;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.API.Users;
using ScoredProductions.StreamLinked.IRC.Message;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.ManagersAndBuilders;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;

namespace ScoredProductions.StreamLinked.IRC {

	/// <summary>
	/// Twitch IRC Singleton.
	/// Handles connection and processing of messages.
	/// </summary>
	[DefaultExecutionOrder(-0x4)]
	public class TwitchIRCClient : SingletonDispatcher<TwitchIRCClient> {

		/// <summary>
		/// 10 KB
		/// </summary>
		public const int BUFFER_SIZE = 10240;

		[SerializeField]
		private bool ircEnabled = false;
		public bool IRCEnabled
		{
			get => this.ircEnabled;
			set
			{
				if (this.ircEnabled != value) {
					this.ircEnabled = value;
					if (InstanceIsAlive) {
						if (this.ircEnabled) {
							this.ReconnectToTwitch();
						}
						else {
							this.CloseIRC();
						}
					}
				}
			}
		}

		[SerializeField, HideInInspector, Tooltip("Token to use for the IRC, if left blank it will default to the one provided to the API Client")]
		private TokenInstance ircToken;
		public TokenInstance IRCToken
		{
			get => this.ircToken;
			set
			{
				if (this.ircToken != value) {
					this.EndFunctionality();
					this.BuildCancelTokens();
					this.ircToken = value;
					this.ircTokenUserInfo = null;
				}
			}
		}

		private GetUsers? ircTokenUserInfo;

		public bool IsConnected => this.tcpClient != null && this.tcpClient.Connected;
		public bool IsConnecting => this.ConnectingToTwitchRoutine != null;
		public bool IsConnectingOrConnected => this.IsConnected | this.IsConnecting;

		public bool SaveTargetToPlayerPrefs = true;

		public bool CommandsEnabled = true;
		public bool MembershipEnabled = true;
		public bool TagsEnabled = true;
		public bool SSLConnection = false;
		[Tooltip("Moves the stream reading from the connection to an external thread instead of a coroutine in this object. [Original Method]")]
		public bool UseAsyncToRead = false;

		public bool OverwriteFromInternalSettings;

		[HideInInspector, Tooltip("Event to track all messages passing through the IRC, more expensive than individual Events due to boxing")]
		public ExtendedUnityEvent<ITwitchIRCMessage> OnANY;
		[HideInInspector]
		public ExtendedUnityEvent<JOIN> OnJOIN;
		[HideInInspector]
		public ExtendedUnityEvent<NICK> OnNICK;
		[HideInInspector]
		public ExtendedUnityEvent<NOTICE> OnNOTICE;
		[HideInInspector]
		public ExtendedUnityEvent<PART> OnPART;
		[HideInInspector]
		public ExtendedUnityEvent<PASS> OnPASS;
		[HideInInspector]
		public ExtendedUnityEvent<PING> OnPING;
		[HideInInspector]
		public ExtendedUnityEvent<PONG> OnPONG;
		[HideInInspector]
		public ExtendedUnityEvent<PRIVMSG> OnPRIVMSG;
		[HideInInspector]
		public ExtendedUnityEvent<CLEARCHAT> OnCLEARCHAT;
		[HideInInspector]
		public ExtendedUnityEvent<CLEARMSG> OnCLEARMSG;
		[HideInInspector]
		public ExtendedUnityEvent<GLOBALUSERSTATE> OnGLOBALUSERSTATE;
		[HideInInspector]
		public ExtendedUnityEvent<RECONNECT> OnRECONNECT;
		[HideInInspector]
		public ExtendedUnityEvent<ROOMSTATE> OnROOMSTATE;
		[HideInInspector]
		public ExtendedUnityEvent<USERNOTICE> OnUSERNOTICE;
		[HideInInspector]
		public ExtendedUnityEvent<USERSTATE> OnUSERSTATE;
		[HideInInspector]
		public ExtendedUnityEvent<CAP> OnCAP;
		[HideInInspector, Tooltip("For messages with either no command specified or command not listed in available events")]
		public ExtendedUnityEvent<OTHER> OnOTHER;

		[HideInInspector]
		public ExtendedUnityEvent OnIRCStarted;
		[HideInInspector]
		public ExtendedUnityEvent OnIRCStopped;

		[SerializeField, HideInInspector]
		private string twitchTarget;
		public string TwitchTarget
		{
			get => this.twitchTarget;
			set
			{
				if (value != this.TwitchTarget) {
					this.twitchTarget = value?.Trim() ?? value;
					if (this.SaveTargetToPlayerPrefs) {
						InternalSettingsStore.EditSetting(SavedSettings.TwitchTarget, value, this.LogDebugLevel == DebugManager.DebugLevel.Full);
					}
					if (this.IsConnected && !string.IsNullOrWhiteSpace(value)) {
						this.JoinChannel(value);
					}
				}
			}
		}

		public string ConnectedChannelOnSocket { get; private set; }

		public bool ClientStopped => this.tcpClient == null;

		private GetUsers? joinedRoomUserData = null;
		public GetUsers? JoinedRoomUserData
		{
			get => this.joinedRoomUserData;
			private set
			{
				if (value == null && !this.joinedRoomUserData.HasValue) {
					return;
				}
				GetUsers? oldValue = this.joinedRoomUserData;
				if (value == null) {
					this.joinedRoomUserData = null;
					OnJoinedRoomUserUpdated?.Invoke(oldValue, null);
				}
				else if (this.joinedRoomUserData == null) {
					this.joinedRoomUserData = value;
					OnJoinedRoomUserUpdated?.Invoke(null, value);
				}
				else if (this.joinedRoomUserData.Value.login != value.Value.login) {
					this.joinedRoomUserData = value;
					OnJoinedRoomUserUpdated?.Invoke(this.joinedRoomUserData, value);
				}
			}
		}

		[SerializeField, HideInInspector]
		private bool persistBetweenScenes = true;
		public override bool PersistBetweenScenes => this.persistBetweenScenes;

		public delegate Task ReturnAPIData(GetUsers? OldUser, GetUsers? NewUser);
		public static ReturnAPIData OnJoinedRoomUserUpdated;

		private TcpClient tcpClient;

		private NetworkStream internalTCPStream;

		private Stream ProducedStream;

		private StreamWriter streamWriter;

		private readonly byte[] buffer = new byte[BUFFER_SIZE];

		private Coroutine ConnectingToTwitchRoutine;

		private Coroutine NetworkReader;

		private TwitchAPIClient apiReference;

		private WaitUntil _waitForAPIAuth;
		private WaitUntil WaitForAPIAuth => this._waitForAPIAuth ??= new WaitUntil(this.CheckIRCTokenIsInQueue);

		private CancellationTokenSource RequestAPICancellationToken;

		private CancellationTokenSource MessageReaderCancellationToken;

		private static readonly Encoding Encoder = Encoding.UTF8;

		private static readonly ConcurrentQueue<string> MessageQueue = new ConcurrentQueue<string>();

		private TwitchIRCClient() { }

		private void OnEnable() {
			this.BuildCancelTokens();
			if (this.ircEnabled) {
				this.ReconnectToTwitch();
			}
		}

		private void OnDestroy() {
			this.EndFunctionality();

			this.OnJOIN?.RemoveAllListeners();
			this.OnNICK?.RemoveAllListeners();
			this.OnNOTICE?.RemoveAllListeners();
			this.OnPART?.RemoveAllListeners();
			this.OnPASS?.RemoveAllListeners();
			this.OnPING?.RemoveAllListeners();
			this.OnPONG?.RemoveAllListeners();
			this.OnPRIVMSG?.RemoveAllListeners();
			this.OnCLEARCHAT?.RemoveAllListeners();
			this.OnCLEARMSG?.RemoveAllListeners();
			this.OnGLOBALUSERSTATE?.RemoveAllListeners();
			this.OnRECONNECT?.RemoveAllListeners();
			this.OnROOMSTATE?.RemoveAllListeners();
			this.OnUSERNOTICE?.RemoveAllListeners();
			this.OnUSERSTATE?.RemoveAllListeners();
			this.OnCAP?.RemoveAllListeners();
			this.OnOTHER?.RemoveAllListeners();
		}

		private void OnDisable() {
			this.EndFunctionality();
		}

		protected override void OnApplicationQuit() {
			this.EndFunctionality();
			base.OnApplicationQuit();
		}

		private void EndFunctionality() {
			this.CloseIRC();

			this.JoinedRoomUserData = null;

			if (this.ConnectingToTwitchRoutine != null) {
				this.StopCoroutine(this.ConnectingToTwitchRoutine);
			}

			if (this.NetworkReader != null) {
				this.StopCoroutine(this.NetworkReader);
			}

			this.EndCancelTokens();
		}

		protected override void LateUpdate() {
			base.LateUpdate();

			while (MessageQueue.TryDequeue(out string message)) {
				this.ProcessMessage(message);
			}
		}

		public void SaveTwitchTargetToPlayerPrefs() {
			if (string.IsNullOrWhiteSpace(this.twitchTarget)) {
				InternalSettingsStore.EditSetting(SavedSettings.TwitchTarget, "", this.LogDebugLevel == DebugManager.DebugLevel.Full);
			}
			else {
				if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchTarget, out string target, this.LogDebugLevel == DebugManager.DebugLevel.Full)) {
					if (target != this.twitchTarget) {
						InternalSettingsStore.EditSetting(SavedSettings.TwitchTarget, this.twitchTarget, this.LogDebugLevel == DebugManager.DebugLevel.Full);
					}
				}
				else {
					InternalSettingsStore.EditSetting(SavedSettings.TwitchTarget, this.twitchTarget, this.LogDebugLevel == DebugManager.DebugLevel.Full);
				}
			}
		}

		private bool CheckIRCTokenIsInQueue() {
			return this.apiReference == null || this.apiReference.CheckTokenIsInQueue(this.ircToken);
		}

		private void EndCancelTokens() {
			if (this.RequestAPICancellationToken != null) {
				try {
					this.RequestAPICancellationToken.Cancel();
					this.RequestAPICancellationToken.Dispose();
					this.RequestAPICancellationToken = null;
				} catch {
					if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage("RequestAPICancellationToken was Canceled".RichTextColour("orange"));
					}
				}
			}
			if (this.MessageReaderCancellationToken != null) {
				try {
					this.MessageReaderCancellationToken.Cancel();
					this.MessageReaderCancellationToken.Dispose();
					this.MessageReaderCancellationToken = null;
				} catch {
					if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage("AsyncReaderCancellationToken was Canceled".RichTextColour("orange"));
					}
				}

			}
		}

		private void BuildCancelTokens() {
			this.RequestAPICancellationToken ??= new CancellationTokenSource();
		}

		/// <summary>
		/// Get Twitch information of Target room
		/// </summary>
		public async Task GetJoinedRoomUserData() {
			this.JoinedRoomUserData = null;
			if (!string.IsNullOrWhiteSpace(this.ConnectedChannelOnSocket)) {

				TwitchAPIDataContainer<GetUsers> returnedData
						= await TwitchAPIClient.MakeTwitchAPIRequestAsync<GetUsers>(
							QueryParameters: new (string, string)[] {
							(GetUsers.LOGIN, this.ConnectedChannelOnSocket)
							},
							cancelToken: this.RequestAPICancellationToken.Token);
				if (!returnedData.HasErrored) {
					this.JoinedRoomUserData = returnedData.data[0];
					if (TwitchBadgeManager.GetInstance(out TwitchBadgeManager manager)) {
						manager.GetChannelBadges(this.JoinedRoomUserData.Value, true, this.ircToken);
					}
				}
				else {
					if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage($"GetUsers API call failed to get Users. TwitchTarget: {{{this.ConnectedChannelOnSocket}}}, Error: {returnedData.ErrorText}", DebugManager.ErrorLevel.Assertion);
					}
				}
			}
			else {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"GetUsers API call failed to get Users. ConnectedChannelOnSocket is null or empty when it shouldnt be.", DebugManager.ErrorLevel.Assertion);
				}
			}
		}

		/// <summary>
		/// Attempts to aquire information required to start Twitch IRC
		/// </summary>
		private bool GetSettings() {
			if (this.OverwriteFromInternalSettings || string.IsNullOrEmpty(this.twitchTarget)) {
				if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchTarget, out string target)) {
					this.twitchTarget = target;
				}
			}
			if (this.SaveTargetToPlayerPrefs && !string.IsNullOrWhiteSpace(this.twitchTarget)) {
				InternalSettingsStore.EditSetting(SavedSettings.TwitchTarget, this.twitchTarget);
			}
			if (string.IsNullOrEmpty(this.twitchTarget)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"Failed to get a Target for {TwitchAPIClient.Name}, There is no channel to point to.", DebugManager.ErrorLevel.Error);
				}
				return false;
			}
			return true;
		}

		public void SendToTwitch(string message) => this.SendToTwitch(message.AsSpan());

		public void SendToTwitch(ReadOnlySpan<char> message) {
			if (this.streamWriter == null) {
				return;
			}
			try {
				if (message.EndsWith(TwitchWords.END_MESSAGE_TAG, StringComparison.InvariantCultureIgnoreCase)) {
					this.streamWriter.Write(message);
				}
				else {
					int messageLen = message.Length;
					Span<char> combine = stackalloc char[messageLen + 2];
					message.CopyTo(combine[..messageLen]);
					combine[messageLen++] = TwitchWords.END_MESSAGE_TAG_PART_1;
					combine[messageLen] = TwitchWords.END_MESSAGE_TAG_PART_2;
					this.streamWriter.Write(combine);
				}
				this.streamWriter.Flush();
				if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
					DebugManager.LogMessage(new string(message));
				}
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex, DebugManager.ErrorLevel.Exception);
				}
			}
		}

		/// <summary>
		/// Leaves and joins a new room, provide blank room to not join new room
		/// </summary>
		public void JoinChannel(string room) {
			this.LeaveChannel();
			this.BuildCancelTokens();

			if (!string.IsNullOrWhiteSpace(room)) {

				this.ConnectedChannelOnSocket = room.ToLower();

				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage($"IRC Attempting to join room: {this.ConnectedChannelOnSocket}");
				}

				ReadOnlySpan<char> jWord = TwitchWords.JOIN.AsSpan();
				int jLen = jWord.Length;
				ReadOnlySpan<char> channelWord = this.ConnectedChannelOnSocket.AsSpan();
				int cLen = channelWord.Length;

				Span<char> joinSpan = stackalloc char[jLen + 2 + cLen];
				int index = 0;
				jWord.CopyTo(joinSpan.Slice(index, jLen));
				index += jLen;
				joinSpan[index++] = ' ';
				joinSpan[index++] = '#';
				channelWord.CopyTo(joinSpan.Slice(index, cLen));
				index += cLen;
				this.SendToTwitch(joinSpan);

				const int Commands = 0x1;
				const int Membership = 0x2;
				const int Tags = 0x4;
				int capabilities
					= (this.CommandsEnabled ? Commands : 0)
					+ (this.MembershipEnabled ? Membership : 0)
					+ (this.TagsEnabled ? Tags : 0);

				if (capabilities > 0) {
					Span<char> capReqSpan = stackalloc char[128];
					string CAPREQ = "CAP REQ :";
					int capLen = CAPREQ.Length;
					index = 0;
					CAPREQ.AsSpan().CopyTo(capReqSpan.Slice(index, capLen));
					index += capLen;

					if ((capabilities & Commands) > 0) {
						int comLen = TwitchWords.TWITCH_CAPABILITIES_COMMANDS.Length;
						TwitchWords.TWITCH_CAPABILITIES_COMMANDS.AsSpan().CopyTo(capReqSpan.Slice(index, comLen));
						index += comLen;

						if (capabilities > Commands) {
							capReqSpan[index++] = ' ';
						}
					}

					if ((capabilities & Membership) > 0) {
						int comLen = TwitchWords.TWITCH_CAPABILITIES_MEMBERSHIP.Length;
						TwitchWords.TWITCH_CAPABILITIES_MEMBERSHIP.AsSpan().CopyTo(capReqSpan.Slice(index, comLen));
						index += comLen;

						if (capabilities > Membership) {
							capReqSpan[index++] = ' ';
						}
					}

					if ((capabilities & Tags) > 0) {
						int comLen = TwitchWords.TWITCH_CAPABILITIES_TAGS.Length;
						TwitchWords.TWITCH_CAPABILITIES_TAGS.AsSpan().CopyTo(capReqSpan.Slice(index, comLen));
						index += comLen;
					}

					capReqSpan[index++] = TwitchWords.END_MESSAGE_TAG_PART_1;
					capReqSpan[index++] = TwitchWords.END_MESSAGE_TAG_PART_2;
					this.SendToTwitch(capReqSpan[..index]);
				}

				Task.Run(this.GetJoinedRoomUserData);
			}
		}

		private async Task ReadAsyncCallback() {
			char[] receiveMessage = Array.Empty<char>();
			int receiveLen = 0;
			try {
				while (this.ProducedStream?.CanRead ?? false) {
					if (this.internalTCPStream.DataAvailable) {
						int bytesRead = this.ProducedStream.Read(this.buffer, 0, BUFFER_SIZE);
						Array.Fill(receiveMessage, '\0');
						if (bytesRead > receiveLen) {
							Array.Resize(ref receiveMessage, bytesRead);
							receiveLen = bytesRead;
						}

						Encoder.GetChars(this.buffer, 0, bytesRead, receiveMessage, 0);
						for (int x = 0, y = 1, z = 0; y < receiveLen; x++, y++) {
							if (receiveMessage[y] == '\0') {
								break;
							}
							if (receiveMessage[x] == '\r' && receiveMessage[y] == '\n') {
								MessageQueue.Enqueue(new string(receiveMessage[z..(y + 1)]));
								z = y + 1;
							}
						}

						if (this.ClientStopped || !this.IsConnected || this.ProducedStream == null || !this.ProducedStream.CanRead) {
							return;
						}
					}

					await Task.Delay(100);
				}
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
			}
		}

		private IEnumerator ReadCoroutineCallback() {
			char[] receiveMessage = Array.Empty<char>();
			int receiveLen = 0;

			while (this.ProducedStream?.CanRead ?? false) {
				if (this.internalTCPStream.DataAvailable) {
					try {
						int bytesRead = this.ProducedStream.Read(this.buffer, 0, BUFFER_SIZE);
						Array.Fill(receiveMessage, '\0');
						if (bytesRead > receiveLen) {
							Array.Resize(ref receiveMessage, bytesRead);
							receiveLen = bytesRead;
						}

						Encoder.GetChars(this.buffer, 0, bytesRead, receiveMessage, 0);
						for (int x = 0, y = 1, z = 0; x < receiveLen - 1; x++, y++) {
							if (receiveMessage[y] == '\0') {
								break;
							}
							if (receiveMessage[x] == '\r' && receiveMessage[y] == '\n') {
								MessageQueue.Enqueue(new string(receiveMessage[z..(y + 1)]));
								z = y;
							}
						}

						if (this.ClientStopped || !this.IsConnected || this.ProducedStream == null || !this.ProducedStream.CanRead) {
							yield break;
						}
					} catch (Exception ex) {
						if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
							DebugManager.LogMessage($"Problem occured in Callback, Error: {{{ex.Message}}} : StackTrace: {{{ex.StackTrace.RichTextColour("yellow")}}} : MessageState: {{{"receivedMessage.ToString()"}}}");
						}
					}
				}

				yield return TwitchStatic.EndOfFrameWait;
			}
		}

		public void StartUpIRC(string target = null) {
			if (this.IRCEnabled) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage($"Twitch IRC is already enabled, yet StartUpIRC was called.");
				}
				return;
			}

			if (!string.IsNullOrEmpty(target)) {
				this.twitchTarget = target.Trim();
			}

			this.IRCEnabled = true;
		}

		public void ShutdownIRC() {
			this.IRCEnabled = false;
		}

		/// <summary>
		/// Stops and restarts Twitch IRC connection
		/// </summary>
		public void ReconnectToTwitch() {
			if (this.ircEnabled) {
				MainThreadDispatchQueue.Enqueue(this.ThreadReconnectToTwitch);
			}
		}

		private void ThreadReconnectToTwitch() {
			if (this.ConnectingToTwitchRoutine != null) {
				this.StopCoroutine(this.ConnectingToTwitchRoutine);
			}
			this.ConnectingToTwitchRoutine = this.StartCoroutine(this.StartTwitchReconnectionRoutine());
			this.OnIRCStarted?.Invoke();
		}

		private IEnumerator StartTwitchReconnectionRoutine() {
			if (!this.ircEnabled) {
				DebugManager.LogMessage($"IRC Disabled, Connect process Canceled.".RichTextColour("yellow"), DebugManager.ErrorLevel.Assertion);
				goto End;
			}
			if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
				DebugManager.LogMessage($"Twitch IRC {(this.IsConnected ? "Reconnecting" : "Connecting")}.");
			}
			if (this.IsConnected) {
				this.CloseIRC();
			}

			if (this.apiReference != null || TwitchAPIClient.GetInstance(out this.apiReference)) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage($"Twitch IRC waiting for API credentials.");
				}

				if (this.ircToken == null) {
					if (this.apiReference.DefaultAPIToken != null) {
						this.ircToken = this.apiReference.DefaultAPIToken;
					}
					if (this.ircToken == null) {
						DebugManager.LogMessage("No Token Instance was found for either the IRC or the API clients. Connection cancelled.");
						goto End;
					}
				}

				if (this.ircToken.CheckRefreshNeeded(this.LogDebugLevel > DebugManager.DebugLevel.Necessary)) {
					this.apiReference.GetNewAuthToken(this.ircToken);
				}

				if (this.CheckIRCTokenIsInQueue()) {
					yield return this.WaitForAPIAuth;
				}

				if (!this.GetSettings()) {
					goto End;
				}

				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage($"Twitch IRC credentials aquired.");
				}

				if (!this.ircTokenUserInfo.HasValue) {
					yield return this.apiReference.MakeTwitchAPIRequest<GetUsers>(this.GetUserInfo, this.ircToken);
				}

				this.tcpClient = new TcpClient() {
					ReceiveBufferSize = this.buffer.Length,
					SendTimeout = int.MaxValue,
					ReceiveTimeout = int.MaxValue,
				};
				if (this.SSLConnection) {
					this.tcpClient.Connect(TwitchWords.CHAT_HOST_ADDRESS, TwitchStatic.SSL_IRC_PORT);
					this.internalTCPStream = this.tcpClient.GetStream();
					SslStream ssl = new SslStream(this.internalTCPStream, true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
					try {
						ssl.AuthenticateAsClient(TwitchWords.CHAT_HOST_ADDRESS);
					} catch { // abort						
						this.CloseIRC();
						yield break;
					}
					this.ProducedStream = ssl;
				}
				else {
					this.tcpClient.Connect(TwitchWords.CHAT_HOST_ADDRESS, TwitchStatic.NON_SSL_IRC_PORT);
					this.ProducedStream = this.internalTCPStream = this.tcpClient.GetStream();
				}

				this.streamWriter = new StreamWriter(this.ProducedStream);

				this.MessageReaderCancellationToken ??= new CancellationTokenSource();
				if (this.UseAsyncToRead) {
					Task.Run(this.ReadAsyncCallback, this.MessageReaderCancellationToken.Token);
				}
				else {
					this.NetworkReader = this.StartCoroutine(this.ReadCoroutineCallback());
				}

				this.SendToTwitch($"{TwitchWords.PASS} {TwitchStatic.AppendOAuthToOAuth(this.ircToken.OAuthToken.Access_Token)}{TwitchWords.END_MESSAGE_TAG}");
				this.SendToTwitch($"{TwitchWords.NICK} {this.ircTokenUserInfo.Value.login}{TwitchWords.END_MESSAGE_TAG}");

				this.JoinChannel(this.TwitchTarget);
			}
			else {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("TwitchAPIClient not available, cant authenticate with Twitch.", DebugManager.ErrorLevel.Assertion);
				}
			}

			End:
			this.ConnectingToTwitchRoutine = null;
		}

		private void GetUserInfo(TwitchAPIDataContainer<GetUsers> data) {
			if (data.HasErrored) {
				this.EndFunctionality();
			}
			else {
				this.ircTokenUserInfo = data.data[0];
			}
		}

		/// <summary>
		/// Leaves twitch IRC room
		/// </summary>
		/// <param name="newRoom"></param>
		private void LeaveChannel() {
			if (!string.IsNullOrEmpty(this.ConnectedChannelOnSocket)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"Leaving current room: {this.ConnectedChannelOnSocket}");
				}
				this.SendToTwitch($"{TwitchWords.PART} #{this.ConnectedChannelOnSocket}");
				this.ConnectedChannelOnSocket = null;
			}
		}

		private void CloseIRC() {
			this.LeaveChannel();

			if (this.streamWriter != null) {
				lock (this.streamWriter) {
					this.streamWriter.Flush();
					this.streamWriter.Close();
					this.streamWriter.Dispose();
				}
				this.streamWriter = null;
			}
			if (this.ProducedStream != null) {
				lock (this.ProducedStream) {
					this.ProducedStream.Flush();
					this.ProducedStream.Close();
					this.ProducedStream.Dispose();
				}
				this.ProducedStream = null;
			}
			if (this.tcpClient != null) {
				lock (this.tcpClient) {
					this.tcpClient.Close();
					this.tcpClient.Dispose();
				}
				this.tcpClient = null;
			}

			Array.Clear(this.buffer, 0, BUFFER_SIZE); // Clear buffer ready for new instance

			this.OnIRCStopped?.Invoke();
		}

		public void SendMessageToChat(string message) {
			this.SendToTwitch($"{TwitchWords.PRIVMSG} #{this.twitchTarget} : {message}");
		}

		/// <summary>
		/// Builds message into a TwitchMessage object
		/// </summary>
		/// <param name="rawMessage"></param>
		private void ProcessMessage(string rawMessage) {
			if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
				DebugManager.LogMessage($"Message Received: {rawMessage}");
			}

			try {
				TwitchIRCCommand a = ITwitchIRCMessage.EstablishMessageType(rawMessage);

				bool OnTypeAvailable;
				bool OnAnyAvilable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnANY);

				switch (ITwitchIRCMessage.EstablishMessageType(rawMessage)) {
					case TwitchIRCCommand.JOIN:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnJOIN);

						if (OnTypeAvailable | OnAnyAvilable) {
							JOIN msg1 = new JOIN(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnJOIN.Invoke(msg1));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg1));
							}
						}
						break;
					case TwitchIRCCommand.NICK:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnNICK);

						if (OnTypeAvailable | OnAnyAvilable) {
							NICK msg2 = new NICK(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnNICK.Invoke(msg2));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg2));
							}
						}
						break;
					// Auth
					case TwitchIRCCommand.NOTICE:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnNOTICE);

						if (OnTypeAvailable | OnAnyAvilable) {
							NOTICE msg3 = new NOTICE(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnNOTICE.Invoke(msg3));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg3));
							}
						}
						break;
					case TwitchIRCCommand.PART:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnPART);

						if (OnTypeAvailable | OnAnyAvilable) {
							PART msg4 = new PART(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnPART.Invoke(msg4));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg4));
							}
						}
						break;
					case TwitchIRCCommand.PASS:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnPASS);

						if (OnTypeAvailable | OnAnyAvilable) {
							PASS msg5 = new PASS(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnPASS.Invoke(msg5));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg5));
							}
						}
						break;
					// Responder
					case TwitchIRCCommand.PING:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnPING);

						this.SendToTwitch($"PONG {rawMessage[5..]}{TwitchWords.END_MESSAGE_TAG}");

						if (OnTypeAvailable | OnAnyAvilable) {
							PING msg6 = new PING(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnPING.Invoke(msg6));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg6));
							}
						}
						break;
					case TwitchIRCCommand.PONG:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnPONG);

						if (OnTypeAvailable | OnAnyAvilable) {
							PONG msg7 = new PONG(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnPONG.Invoke(msg7));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg7));
							}
						}
						break;
					// Chat message
					case TwitchIRCCommand.PRIVMSG:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnPRIVMSG);

						if (OnTypeAvailable | OnAnyAvilable) {
							PRIVMSG msg8 = new PRIVMSG(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnPRIVMSG.Invoke(msg8));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg8));
							}
						}
						break;
					case TwitchIRCCommand.CLEARCHAT:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnCLEARCHAT);

						if (OnTypeAvailable | OnAnyAvilable) {
							CLEARCHAT msg9 = new CLEARCHAT(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnCLEARCHAT.Invoke(msg9));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg9));
							}
						}
						break;
					case TwitchIRCCommand.CLEARMSG:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnCLEARMSG);

						if (OnTypeAvailable | OnAnyAvilable) {
							CLEARMSG msg10 = new CLEARMSG(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnCLEARMSG.Invoke(msg10));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg10));
							}
						}
						break;
					case TwitchIRCCommand.GLOBALUSERSTATE:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnGLOBALUSERSTATE);

						if (OnTypeAvailable | OnAnyAvilable) {
							GLOBALUSERSTATE msg11 = new GLOBALUSERSTATE(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnGLOBALUSERSTATE.Invoke(msg11));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg11));
							}
						}
						break;
					case TwitchIRCCommand.RECONNECT:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnRECONNECT);

						if (OnTypeAvailable | OnAnyAvilable) {
							RECONNECT msg12 = new RECONNECT(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnRECONNECT.Invoke(msg12));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg12));
							}
						}
						break;
					case TwitchIRCCommand.ROOMSTATE:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnROOMSTATE);

						if (OnTypeAvailable | OnAnyAvilable) {
							ROOMSTATE msg13 = new ROOMSTATE(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnROOMSTATE.Invoke(msg13));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg13));
							}
						}
						break;
					case TwitchIRCCommand.USERNOTICE:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnUSERNOTICE);

						if (OnTypeAvailable | OnAnyAvilable) {
							USERNOTICE msg14 = new USERNOTICE(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnUSERNOTICE.Invoke(msg14));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg14));
							}
						}
						break;
					case TwitchIRCCommand.USERSTATE:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnUSERSTATE);

						if (OnTypeAvailable | OnAnyAvilable) {
							USERSTATE msg15 = new USERSTATE(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnUSERSTATE.Invoke(msg15));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg15));
							}
						}
						break;
					case TwitchIRCCommand.CAP:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnCAP);

						if (OnTypeAvailable | OnAnyAvilable) {
							CAP msg16 = new CAP(rawMessage);
							if (OnTypeAvailable) {

								MainThreadDispatchQueue.Enqueue(() => this.OnCAP.Invoke(msg16));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg16));
							}
						}
						break;
					default:
						OnTypeAvailable = !ExtendedUnityEvent.IsNullOrEmpty(this.OnOTHER);

						if (OnTypeAvailable | OnAnyAvilable) {
							OTHER msg17 = new OTHER(rawMessage);

							if (OnTypeAvailable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnOTHER.Invoke(msg17));
							}

							if (OnAnyAvilable) {
								MainThreadDispatchQueue.Enqueue(() => this.OnANY.Invoke(msg17));
							}
						}
						break;
				}
			} catch (Exception exception) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"Exception occurred when processing rawMessage: {rawMessage} : {exception.ToString().RichTextColour("yellow")}", DebugManager.ErrorLevel.Error);
				}
			}
		}

		// The following method is invoked by the RemoteCertificateValidationDelegate.
		public static bool ValidateServerCertificate(
			  object sender,
			  X509Certificate certificate,
			  X509Chain chain,
			  SslPolicyErrors sslPolicyErrors) {
			if (sslPolicyErrors == SslPolicyErrors.None) {
				return true;
			}
			DebugManager.LogMessage($"IRC SSL Certificate error: {sslPolicyErrors}");
			// Do not allow this client to communicate with unauthenticated servers.
			return false;
		}

		public TcpState GetTCPSocketState() {
			if (this.tcpClient == null) {
				return TcpState.Unknown;
			}
			TcpConnectionInformation[] foo = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
			TcpConnectionInformation tcpci = null;
			foreach (TcpConnectionInformation tcp in foo) {
				if (tcp.LocalEndPoint.Equals(this.tcpClient.Client.LocalEndPoint)) {
					tcpci = tcp;
				}
			}
			return tcpci == null ? TcpState.Unknown : tcpci.State;
		}

	}
}