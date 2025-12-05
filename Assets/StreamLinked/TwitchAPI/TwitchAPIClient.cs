using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;

using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;
using UnityEngine.Networking;

namespace ScoredProductions.StreamLinked.API {

	/// <summary>
	/// Primary singleton for API functionality.
	/// Handles OAuth management and requests.
	/// Contains the methods to make API requests.
	/// </summary>
	[DefaultExecutionOrder(-0x8)]
	[ExecuteAlways]
	public partial class TwitchAPIClient : SingletonDispatcher<TwitchAPIClient> {

		public const string WebResponseBackup = "<b>Unity Application (StreamLinked) has received the response from Twitch.\nApp will now continue.</b><br>(You can close this window now)";

		public static bool APIOAuthAvailable => GetInstance(out TwitchAPIClient instance) && instance.CheckOAuthExistsAndInDate();

		[SerializeField, HideInInspector, Delayed]
		private string _twitchClientID;
		public string TwitchClientID => this._twitchClientID;

		[SerializeField, HideInInspector, Delayed]
		private string _twitchSecret;
		public string TwitchSecret => this._twitchSecret;

		[SerializeField, HideInInspector]
		private TwitchClientType _twitchClientType = TwitchClientType.Confidential;
		public TwitchClientType TwitchClientType => this._twitchClientType;

		public CancellationToken APICancelToken => this.RequestAPICancellationToken?.Token ?? default;

		public bool GettingNewToken => this.currentTokenBeingRefreshed != null;

		public bool UsePlayerPrefsFirst = false;

		public bool LogAny => this.LogDebugLevel != DebugManager.DebugLevel.None;
		public bool LogAll => this.LogDebugLevel == DebugManager.DebugLevel.Full;

		// Default token settings to use
		[Tooltip("The default token used by the API to make requests if no specific one is provided.")]
		public TokenInstance DefaultAPIToken; // (Property Drawer) Need button to create one in inspector + button to create one if asset doesnt exist in AssetDatabase

		// Queue of tokens to update
		private Queue<TokenInstance> tokenUpdateQueue; // Be very unlikely to excede 1 at a time, but it doubles when needed so start low
		private Queue<TokenInstance> GetTokenUpdateQueue => this.tokenUpdateQueue ??= new Queue<TokenInstance>(1); // only allocate when needed
		public bool CheckTokenIsInQueue(TokenInstance token) => token == null || (this.currentTokenBeingRefreshed == token || (this.tokenUpdateQueue != null && this.tokenUpdateQueue.Contains(token)));

		// Current token being updated
		private TokenInstance currentTokenBeingRefreshed;

		/// <summary>
		/// Returns after the API starts aquiring a new token
		/// </summary>
		[HideInInspector]
		public ExtendedUnityEvent<TokenInstance> OnAuthenticationRefreshStarted;

		/// <summary>
		/// Returns after a successful call to Twitch API
		/// </summary>
		[HideInInspector]
		public ExtendedUnityEvent<TokenInstance> OnAuthenticationSuccess;

		/// <summary>
		/// Returns after a failed call to Twitch API
		/// </summary>
		[HideInInspector]
		public ExtendedUnityEvent<TokenInstance, Exception> OnAuthenticationFailure;

		/// <summary>
		/// Returns after a successful call to Twitch API
		/// </summary>
		[HideInInspector]
		public ExtendedUnityEvent<TokenInstance> OnAuthenticationLoadedFromStorage;

		/// <summary>
		/// Returns after a successful call to Twitch API
		/// </summary>
		[HideInInspector]
		public ExtendedUnityEvent<TokenInstance> OnAuthenticationMissingFromStorage;

		[Tooltip("Time auth webserver is active for Twitch to send information back to the app.")]
		public int AuthWebserverActiveTime = 60000; // 1 min

		[NonSerialized]
		private string TwitchState;

		[SerializeField]
		private bool persistBetweenScenes = true;

		[SerializeField]
		private MakeTwitchAPIRequestSettings DefaultRequestSettings;

		public override bool PersistBetweenScenes => this.persistBetweenScenes;

		private CancellationTokenSource HttpListenerCancellationToken; // For webserver shutdown
		private CancellationTokenSource RequestAPICancellationToken; // For general API state

		private AsyncCallback implicitGrantFlowReader;
		private AsyncCallback ImplicitGrantFlowReader => this.implicitGrantFlowReader ??= new AsyncCallback(this.ImplicitGrantFlowCallbackInitialResponse);

		private AsyncCallback authorizationCodeGrantFlowReader;
		private AsyncCallback AuthorizationCodeGrantFlowReader => this.authorizationCodeGrantFlowReader ??= new AsyncCallback(this.AuthorizationCodeGrantFlowCallback);

		private HttpListener httpListener;

		private Coroutine serverMonitorRoutine;

		private static readonly List<Guid> authRequestOrder = new List<Guid>();

		private TwitchAPIClient() { }

		protected override void Awake() {
			if (this.EstablishSingleton(true)) {
				this.BuildCancelTokens();

				if (this.UsePlayerPrefsFirst || string.IsNullOrWhiteSpace(this._twitchClientID)) {
					this.LoadClientID(this.LogDebugLevel == DebugManager.DebugLevel.Full);
				}
				if (!string.IsNullOrWhiteSpace(this._twitchClientID)) {
					InternalSettingsStore.EditSetting(SavedSettings.TwitchClientID, this._twitchClientID, this.LogDebugLevel == DebugManager.DebugLevel.Full);
				}

				if (this.UsePlayerPrefsFirst || string.IsNullOrWhiteSpace(this._twitchSecret)) {
					this.LoadClientSecret(this.LogDebugLevel == DebugManager.DebugLevel.Full);
				}
				if (!string.IsNullOrWhiteSpace(this._twitchSecret)) {
					InternalSettingsStore.EditSetting(SavedSettings.TwitchClientSecret, this._twitchSecret, this.LogDebugLevel == DebugManager.DebugLevel.Full);
				}

				if (this.UsePlayerPrefsFirst) {
					this.LoadClientType(this.LogDebugLevel == DebugManager.DebugLevel.Full);
				}

				if (Application.isPlaying && this.UsePlayerPrefsFirst) {
					this.CheckOAuthExistsAndInDate();
				}
			}
		}

		private void OnEnable() {
			this.BuildCancelTokens();
		}

		private void OnDestroy() {
			this.OnAuthenticationRefreshStarted?.RemoveAllListeners();
			this.OnAuthenticationSuccess?.RemoveAllListeners();
			this.OnAuthenticationFailure?.RemoveAllListeners();
			this.OnAuthenticationLoadedFromStorage?.RemoveAllListeners();
			this.OnAuthenticationMissingFromStorage?.RemoveAllListeners();
			this.EndCancelTokens();
		}

		private void OnDisable() {
			this.EndCancelTokens();
		}

		protected override void OnApplicationQuit() {
			this.EndCancelTokens();
			base.OnApplicationQuit();
		}

		protected override void LateUpdate() {
			if (!GetInstance(out _)) { // Protection vs Editor data wipes
				this.EstablishSingleton(true);
			}

			if (Application.isPlaying
					&& this.DefaultAPIToken != null
					&& this.HasSettingsToGetOAuth(this.DefaultAPIToken)
					&& !this.CheckTokenIsInQueue(this.DefaultAPIToken)
					&& this.DefaultAPIToken.AutoRetrieveNewAuth
					&& this.DefaultAPIToken.CheckRefreshNeeded()) {
				this.GetNewAuthToken(this.DefaultAPIToken);
			}

			if (this.currentTokenBeingRefreshed == null && this.GetTokenUpdateQueue.TryDequeue(out TokenInstance token)) {
				this.BeginTokenAquisition(token);
			}

			base.LateUpdate();
		}

		/// <summary>
		/// Stops all currently running API requests and clears the API Token request queue
		/// </summary>
		public void CancelAPIRequestsAndReset() {
			this.CancelCurrentTokenRequest("CancelAPIRequestsAndReset was called. API Client reset.");
			this.StopAllCoroutines();
			this.tokenUpdateQueue.Clear();
			if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
				DebugManager.LogMessage("CancelAPIRequestsAndReset was called. API Client reset".RichTextColour("orange"));
			}
		}

		/// <summary>
		/// Cancels the latest OAuth request wherever it is with its request, cancelling the token and clearing the currently requested token from the queue.
		/// </summary>
		public void CancelCurrentTokenRequest(string reason = "") {
			this.CloseHttpListener();
			this.TwitchState = null;
			TokenInstance current = this.currentTokenBeingRefreshed;
			if (this.currentTokenBeingRefreshed != null) {
				this.currentTokenBeingRefreshed = null;
				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage($"Current token request cancelled. Token ID: {current.TokenID}{(!string.IsNullOrWhiteSpace(reason) ? @$" Reason: ""{reason}""" : "")}".RichTextColour("orange"));
				}
				this.InvokeAuthentiactionFailed(current, new Exception("Token Request Cancelled: " + reason));
			}
			this.EndCancelTokens();
			this.BuildCancelTokens();
		}

		private void CloseHttpListener() {
			if (this.httpListener != null) {
				try {
					this.httpListener.Close();
					this.CloseHttpRoutineMonitor();
				} catch (Exception ex) {
					if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
						DebugManager.LogMessage(ex);
					}
				}
			}
		}

		private void CloseHttpRoutineMonitor() {
			if (this.serverMonitorRoutine != null) {
				this.StopCoroutine(this.serverMonitorRoutine);
				this.serverMonitorRoutine = null;
			}
		}

		/// <summary>
		/// Get the type of the currently used auth token, null if no token
		/// </summary>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public AuthRequestType? GetCurrentAuthenticationType() {
			if (this.GettingNewToken || this.DefaultAPIToken == null) {
				return null;
			}
			return this.DefaultAPIToken.AuthenticationType;
		}

		/// <summary>
		/// Change authentication type to new value
		/// </summary>
		public void SetAuthenticationType(AuthRequestType type, TokenInstance token = null, bool aquireNewToken = true) {
			if (token == null && this.DefaultAPIToken == null) {
				return;
			}
			else if (token == null) {
				token = this.DefaultAPIToken;
			}

			token.AuthenticationType = type;

			if (aquireNewToken) {
				this.GetNewAuthToken(token);
			}
		}

		public void BuildCancelTokens() {
			this.RequestAPICancellationToken ??= new CancellationTokenSource();
		}

		/// <summary>
		/// Cancels available tokens to stop current requests and servers
		/// </summary>
		public void EndCancelTokens() {
			if (this.RequestAPICancellationToken != null) {
				try {
					this.RequestAPICancellationToken.Cancel();
					this.RequestAPICancellationToken.Dispose();
					this.RequestAPICancellationToken = null;
				} catch {
					if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
						DebugManager.LogMessage("RequestAPICancellationToken was Canceled".RichTextColour("orange"));
					}
				}
			}

			if (this.HttpListenerCancellationToken != null) {
				try {
					this.HttpListenerCancellationToken.Cancel();
					this.HttpListenerCancellationToken.Dispose();
					this.HttpListenerCancellationToken = null;
				} catch {
					if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
						DebugManager.LogMessage("HttpListenerCancellationToken was Canceled".RichTextColour("orange"));
					}
				}
			}
		}

		public bool HasSettingsToGetOAuth() {
			return this.HasSettingsToGetOAuth(null);
		}

		public bool HasSettingsToGetOAuth(AuthRequestType AuthType) {
			return AuthType switch {
				AuthRequestType.ImplicitGrantFlow
				or AuthRequestType.DeviceCodeGrantFlow => !string.IsNullOrWhiteSpace(this.TwitchClientID),
				AuthRequestType.AuthorizationCodeGrantFlow
				or AuthRequestType.ClientCredentialsGrantFlow => this.TwitchClientType != TwitchClientType.Public
					&& !string.IsNullOrWhiteSpace(this.TwitchClientID) && !string.IsNullOrWhiteSpace(this.TwitchSecret),
				_ => false,
			};
		}

		public bool HasSettingsToGetOAuth(TokenInstance tokenInstance) {
			if (tokenInstance == null) {
				if (this.DefaultAPIToken == null) {
					return false;
				}
				else {
					tokenInstance = this.DefaultAPIToken;
				}
			}
			return this.HasSettingsToGetOAuth(tokenInstance.AuthenticationType);
		}

		public string[] ConvertScopesToStringArray(TokenInstance token, bool escape) {
			List<TwitchScopesEnum> scopes = token.RequestScopes.GetAllFlagged();
			string[] array = new string[scopes.Count];

			for (int x = 0; x < scopes.Count; x++) {
				array[x] = escape ? Uri.EscapeDataString(scopes[x].GetLinkedEnumToString()) : scopes[x].GetLinkedEnumToString();
			}

			return array;
		}

		/// <summary>
		/// Loads the Client ID from PlayerPrefs
		/// </summary>
		/// <returns>Found value in PlayerPrefs</returns>
		public bool LoadClientID(bool log) {
			bool returnValue;
			if (returnValue = InternalSettingsStore.TryGetSetting(SavedSettings.TwitchClientID, out string twitchClientID, log)) {
				this._twitchClientID = twitchClientID;
			}
			return returnValue;
		}

		/// <summary>
		/// Loads the Client ID from PlayerPrefs
		/// </summary>
		/// <returns>Found value in PlayerPrefs</returns>
		public bool LoadClientID() {
			return this.LoadClientID(this.LogDebugLevel > DebugManager.DebugLevel.Necessary);
		}

		/// <summary>
		/// Loads the Client Secret from PlayerPrefs
		/// </summary>
		/// <returns>Found value in PlayerPrefs</returns>
		public bool LoadClientSecret(bool log) {
			bool returnValue;

			if (returnValue = InternalSettingsStore.TryGetSetting(SavedSettings.TwitchClientSecret, out string twitchSecret, log)) {
				this._twitchSecret = twitchSecret;
			}
			return returnValue;
		}

		/// <summary>
		/// Loads the Client Secret from PlayerPrefs
		/// </summary>
		/// <returns>Found value in PlayerPrefs</returns>
		public bool LoadClientSecret() {
			return this.LoadClientSecret(this.LogDebugLevel > DebugManager.DebugLevel.Necessary);
		}

		/// <summary>
		/// Loads the Client Type from PlayerPrefs
		/// </summary>
		/// <returns>Found value in PlayerPrefs</returns>
		public bool LoadClientType(bool log) {
			bool returnValue;

			if (returnValue = InternalSettingsStore.TryGetSetting(SavedSettings.TwitchClientType, out int twitchClientType, log)) {
				this._twitchClientType = (TwitchClientType)twitchClientType;
			}
			return returnValue;
		}

		/// <summary>
		/// Loads the Client Type from PlayerPrefs
		/// </summary>
		/// <returns>Found value in PlayerPrefs</returns>
		public bool LoadClientType() {
			return this.LoadClientSecret(this.LogDebugLevel > DebugManager.DebugLevel.Necessary);
		}

		/// <summary>
		/// API will check and perform a get on the provided <c>TokenInstance</c>, if non is provided it will check the one provided for the API to use.
		/// </summary>
		/// <param name="tokenInstance"></param>
		public bool CheckOAuthExistsAndInDate(TokenInstance tokenInstance = null) {
			return this.CheckOAuthExistsAndInDate(tokenInstance, true);
		}

		/// <summary>
		/// API will check and perform a get on the provided <c>TokenInstance</c>, if non is provided it will check the one provided for the API to use.
		/// </summary>
		/// <param name="tokenInstance"></param>
		public bool CheckOAuthExistsAndInDate(TokenInstance tokenInstance, bool refreshIfNeeded = false) {
			if (tokenInstance == null) {
				if (this.DefaultAPIToken == null) {
					return false;
				}
				tokenInstance = this.DefaultAPIToken;
			}
			if (this.currentTokenBeingRefreshed == tokenInstance) {
				return false;
			}

			bool check = tokenInstance.CheckRefreshNeeded(this.LogDebugLevel > DebugManager.DebugLevel.Necessary);

			if (check
				&& refreshIfNeeded
				&& (this.DefaultAPIToken != tokenInstance || !this.GettingNewToken)
				&& this.HasSettingsToGetOAuth(tokenInstance)
				&& InstanceIsAlive) {
				this.GetNewAuthToken(tokenInstance);
			}
			return !check;
		}

		public void WriteTokenToSettings(TokenInstance token, bool log = false) {
			JsonValue retrieveToken = token.RetrieveTokenAsJson();

			JsonArray arrayContainer;
			JsonObject parsed;
			if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchAuthenticationTokens, out string tokens, log)) {
				parsed = JsonReader.Parse(tokens);

				JsonValue data = parsed[TwitchWords.DATA];
				if (data.IsJsonArray
					&& parsed[TwitchWords.CLIENT_ID].AsString == this._twitchClientID
					&& parsed[TwitchWords.CLIENT_SECRET].AsString == this._twitchSecret) {
					arrayContainer = data.AsJsonArray;
				}
				else {
					arrayContainer = new JsonArray();
				}
			}
			else {
				arrayContainer = new JsonArray();
				parsed = new JsonObject();
			}

			parsed[TwitchWords.CLIENT_ID] = this._twitchClientID;
			parsed[TwitchWords.CLIENT_SECRET] = this._twitchSecret;

			int x = 0;
			for (; x < arrayContainer.Count; x++) {
				if (arrayContainer[x][TwitchWords.ID] == token.TokenID) {
					break;
				}
			}
			if (x == arrayContainer.Count) {
				arrayContainer.Add(retrieveToken);
			}
			else {
				arrayContainer[x] = retrieveToken;
			}
			parsed[TwitchWords.DATA] = arrayContainer;

			InternalSettingsStore.EditSetting(SavedSettings.TwitchAuthenticationTokens, parsed.ToString(), log);
		}

		public bool CheckStoreHasTokens(bool log = false) {
			if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchAuthenticationTokens, out string tokens, log)) {
				JsonObject parsed = JsonReader.Parse(tokens);

				if (parsed[TwitchWords.DATA] != JsonValue.Null) {
					JsonArray arrayContainer = parsed[TwitchWords.DATA];
					if (arrayContainer.Count > 0) {
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Saves the ClientID and ClientSecret to PlayerPrefs
		/// </summary>
		public void SaveCurrentToPlayerPrefs() {
			InternalSettingsStore.EditSetting(SavedSettings.TwitchClientID, this.TwitchClientID, this.LogDebugLevel > DebugManager.DebugLevel.None);
			InternalSettingsStore.EditSetting(SavedSettings.TwitchClientSecret, this.TwitchSecret, this.LogDebugLevel > DebugManager.DebugLevel.None);
			InternalSettingsStore.EditSetting(SavedSettings.TwitchClientType, ((int)this.TwitchClientType).ToString(), this.LogDebugLevel > DebugManager.DebugLevel.None);
		}

		/// <summary>
		/// Clears the ClientID and ClientSecret from PlayerPrefs
		/// </summary>
		public void ClearCurrentPlayerPrefs() {
			InternalSettingsStore.EditSetting(SavedSettings.TwitchClientID, null, this.LogDebugLevel > DebugManager.DebugLevel.None);
			InternalSettingsStore.EditSetting(SavedSettings.TwitchClientSecret, null, this.LogDebugLevel > DebugManager.DebugLevel.None);
			InternalSettingsStore.EditSetting(SavedSettings.TwitchClientType, null, this.LogDebugLevel > DebugManager.DebugLevel.None);
		}

		public void CleanPlayerPrefTokens(bool log) {
			this.CleanPlayerPrefTokens(log, null);
		}

		public void CleanPlayerPrefTokens(bool log, params string[] IDs) {
			if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchAuthenticationTokens, out string tokens, log)) {
				JsonObject parsed = JsonReader.Parse(tokens);
				if (IDs.IsNullOrEmpty(out int len)) {
					parsed[TwitchWords.DATA] = new JsonArray();
					InternalSettingsStore.EditSetting(SavedSettings.TwitchAuthenticationTokens, parsed.ToString(), log);
					return;
				}
				JsonValue data = parsed[TwitchWords.DATA];
				JsonValue foundValue = JsonValue.Null;
				if (data.IsJsonArray) {
					JsonArray arrayContainer = data.AsJsonArray;
					bool arrayChanged = false;

					for (int x = 0; x < len; x++) {
						string id = IDs[x];
						int y = 0;
						for (; y < arrayContainer.Count; y++) {
							JsonValue value = arrayContainer[y];
							if (value[TwitchWords.ID] == id) {
								foundValue = value;
								arrayChanged = true;
								break;
							}
						}

						if (foundValue != JsonValue.Null) {
							arrayContainer.Remove(y);
						}
					}

					parsed[TwitchWords.DATA] = arrayContainer;

					if (arrayChanged) {
						InternalSettingsStore.EditSetting(SavedSettings.TwitchAuthenticationTokens, parsed.ToString(), log);
					}
				}
			}
		}

		public void CleanPlayerPrefTokens(params string[] IDs) {
			this.CleanPlayerPrefTokens(this.LogDebugLevel != DebugManager.DebugLevel.None, IDs);
		}

		public void CleanPlayerPrefTokens(string[] IDs, bool log) {
			this.CleanPlayerPrefTokens(log, IDs);
		}

		public void CleanPlayerPrefToken(string ID, bool log) {
			this.CleanPlayerPrefTokens(log, ID);
		}

		public void CleanPlayerPrefToken(TokenInstance token, bool log) {
			this.CleanPlayerPrefTokens(log, token.TokenID);
		}

		public void LoadTokenFromStorage(TokenInstance token, bool log = false) {
			if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchAuthenticationTokens, out string tokens, log)) {
				JsonValue parsed = JsonReader.Parse(tokens);
				JsonValue arrayValue = parsed[TwitchWords.DATA];
				if (arrayValue.IsJsonArray) {
					JsonArray arrayContainer = arrayValue.AsJsonArray;
					foreach (JsonValue container in arrayContainer) {
						if (container[TwitchWords.ID] == token.TokenID) {
							IAuthFlow storedToken = token.AuthenticationType switch {
								AuthRequestType.ImplicitGrantFlow => new ImplicitGrantFlow(container[TwitchWords.DATA]),
								AuthRequestType.AuthorizationCodeGrantFlow => new AuthorizationCodeGrantFlow(container[TwitchWords.DATA]),
								AuthRequestType.DeviceCodeGrantFlow => new DeviceCodeGrantFlow(container[TwitchWords.DATA]),
								AuthRequestType.ClientCredentialsGrantFlow => new ClientCredentialsFlow(container[TwitchWords.DATA]),
								_ => throw new NotImplementedException("Authentication type not recognised")
							};

							if (storedToken.TypeEnum == token.AuthenticationType) {
								token.OAuthToken = storedToken;
							}
							else {
								if (this.LogDebugLevel > DebugManager.DebugLevel.Normal) {
									DebugManager.LogMessage($"Token {{{token.TokenID}}} was loaded from settings b", DebugManager.ErrorLevel.Warning);
								}
							}


							token.OAuthToken = token.AuthenticationType switch {
								AuthRequestType.ImplicitGrantFlow => new ImplicitGrantFlow(container[TwitchWords.DATA]),
								AuthRequestType.AuthorizationCodeGrantFlow => new AuthorizationCodeGrantFlow(container[TwitchWords.DATA]),
								AuthRequestType.DeviceCodeGrantFlow => new DeviceCodeGrantFlow(container[TwitchWords.DATA]),
								AuthRequestType.ClientCredentialsGrantFlow => new ClientCredentialsFlow(container[TwitchWords.DATA]),
								_ => throw new NotImplementedException("Authentication type not recognised")
							};

							if (!ExtendedUnityEvent.IsNullOrEmpty(this.OnAuthenticationLoadedFromStorage)) {
								MainThreadDispatchQueue.Enqueue(() => this.OnAuthenticationLoadedFromStorage.Invoke(token));
							}
							return;
						}
					}
				}
			}
			if (!ExtendedUnityEvent.IsNullOrEmpty(this.OnAuthenticationMissingFromStorage)) {
				MainThreadDispatchQueue.Enqueue(() => this.OnAuthenticationMissingFromStorage.Invoke(token));
			}
		}

		public void UpdateClientDetails(string clientID, string secret, TwitchClientType type, bool log = false) {
			bool changed = this._twitchClientID != clientID || this._twitchSecret != secret;
			if (changed) {
				this.CleanPlayerPrefTokens(log);
				this.currentTokenBeingRefreshed = null;
				this.GetTokenUpdateQueue.Clear();
			}

			this._twitchClientID = clientID;
			this._twitchSecret = secret;
			this._twitchClientType = type;

			if (changed && Application.isPlaying && this.DefaultAPIToken != null && this.DefaultAPIToken.AutoRetrieveNewAuth && this.HasSettingsToGetOAuth(this.DefaultAPIToken.AuthenticationType) && InstanceIsAlive) {
				this.GetNewAuthToken(this.DefaultAPIToken);
			}

			this.SaveCurrentToPlayerPrefs();
		}

		public void QueueActionOnMainThread(Action work) {
			MainThreadDispatchQueue.Enqueue(work);
		}

		/// <summary>
		/// Start up webserver from Unity to receive some Auth token responses
		/// </summary>
		private void StartLocalWebServer(AuthRequestType requestType) {
			if (this.currentTokenBeingRefreshed == null) { // dont start the server if there is no token
				return;
			}

			this.HttpListenerCancellationToken?.Cancel();
			this.HttpListenerCancellationToken = new CancellationTokenSource();

			this.CloseHttpListener();
			this.httpListener = new HttpListener();
			this.httpListener.Prefixes.Add(this.currentTokenBeingRefreshed.RedirectURI);
			this.httpListener.Start();

			switch (requestType) {
				case AuthRequestType.ImplicitGrantFlow:
					this.httpListener.BeginGetContext(this.ImplicitGrantFlowReader, this.httpListener);
					break;
				case AuthRequestType.AuthorizationCodeGrantFlow:
					this.httpListener.BeginGetContext(this.AuthorizationCodeGrantFlowReader, this.httpListener);
					break;
				default:
					throw new NotSupportedException($"Auth request type {{{requestType}}} does not support Webserver retrieval, please consult the Twitch API for instructions.");
			}

			TokenInstance associatedToken = this.currentTokenBeingRefreshed;
			this.serverMonitorRoutine = this.StartCoroutine(this.TokenServerMonitorRoutine(associatedToken));
		}

		private IEnumerator TokenServerMonitorRoutine(TokenInstance token) {
			float delta = 0;
			float authServerAllowedTime = this.AuthWebserverActiveTime / 1000.0f;
			while ((delta += Time.unscaledDeltaTime) < authServerAllowedTime) {
				if (this.currentTokenBeingRefreshed == null) {
					this.CloseHttpListener();
					goto End;
				}

				if (this.HttpListenerCancellationToken?.IsCancellationRequested == null) {
					if (token == this.currentTokenBeingRefreshed) {
						this.CancelCurrentTokenRequest("Cancellation Token Requested");
					}
					goto End;
				}

				yield return TwitchStatic.EndOfFrameWait;
			}

			if (token == this.currentTokenBeingRefreshed) {
				this.CancelCurrentTokenRequest("Auth Webserver Active Time Expired");
			}
			if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
				DebugManager.LogMessage("Webserver listening for token response timed out. Token request must be restarted.".RichTextColour("yellow"), DebugManager.ErrorLevel.Warning);
			}
			End:
			this.CloseHttpRoutineMonitor();
		}

		/// <summary>
		/// HTML/Javascript body and script to send up the webserver for clients after a response has been received
		/// </summary>
		/// <param name="type"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		private string ResponseBuilder(AuthRequestType type) {
			StringBuilder builder = new StringBuilder("<html><body>");

			if (string.IsNullOrWhiteSpace(this.currentTokenBeingRefreshed.UserProvidedWebResponse)) {
				builder.Append(WebResponseBackup);
			}
			else {
				builder.Append(this.currentTokenBeingRefreshed.UserProvidedWebResponse);
			}

			// URL is the response, retreive the URL and send it back down with Javascript
			if (type == AuthRequestType.ImplicitGrantFlow && this.currentTokenBeingRefreshed != null) {
				builder.Append($@"
					<script type=""text/javascript"">
					var xhr = new XMLHttpRequest();
					xhr.open(""POST"", ""{UnityWebRequest.EscapeURL(this.currentTokenBeingRefreshed.RedirectURI)}"");
					xhr.send(window.location);
					{this.currentTokenBeingRefreshed.UserProvidedJSCode}
					</script>
				");
			}

			builder.Append("</body></html>");
			return builder.ToString();
		}

		/// <summary>
		/// Gets a new Auth token of the current type
		/// </summary>
		public void GetNewAuthToken(TokenInstance tokenSettings) {
			if (tokenSettings == null) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("Token Instance provided to request a new token was null", DebugManager.ErrorLevel.Error);
				}
				return;
			}

			if (!this.HasSettingsToGetOAuth(tokenSettings)) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage($"Credentials for the API client have not been set to aquire this token: {{{tokenSettings.TokenID}}}", DebugManager.ErrorLevel.Error);
				}
				return;
			}

			if (!this.CheckTokenIsInQueue(tokenSettings)) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.Normal) {
					DebugManager.LogMessage("New Auth token requested for Token Instance: " + tokenSettings.TokenID);
				}
				this.GetTokenUpdateQueue.Enqueue(tokenSettings);
			}
			else {
				if (this.LogDebugLevel > DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("TokenInstance: " + tokenSettings.TokenID + " Is already in queue to request a new token");
				}
			}
		}

		private void BeginTokenAquisition(TokenInstance tokenSettings) {
			if (tokenSettings != null) {
				if (this.LogDebugLevel > DebugManager.DebugLevel.Normal) {
					DebugManager.LogMessage("Beginning aquisition for TokenInstance: " + tokenSettings.TokenID);
				}

				if (!this.HasSettingsToGetOAuth(tokenSettings.AuthenticationType)) {
					if (this.LogDebugLevel > DebugManager.DebugLevel.Normal) {
						DebugManager.LogMessage("TwitchAPIClient, Token aquisition cancelled, client doesnt have the credentials to aquire TokenInstance: " + tokenSettings.TokenID);
					}
					return;
				}

				this.currentTokenBeingRefreshed = tokenSettings;

				switch (tokenSettings.AuthenticationType) {
					case AuthRequestType.ImplicitGrantFlow:
						this.GetImplicitGrantFlowToken();
						break;
					case AuthRequestType.AuthorizationCodeGrantFlow:
						this.GetAuthorizationGrantFlowToken(this.DefaultRequestSettings.GetFreshTokens);
						break;
					case AuthRequestType.DeviceCodeGrantFlow:
						this.GetDeviceCodeGrantFlowToken(this.DefaultRequestSettings.GetFreshTokens);
						break;
					case AuthRequestType.ClientCredentialsGrantFlow:
						this.GetClientCredentialsFlowToken();
						break;
				}
			}
		}

		/// <summary>
		/// Extracts query values from a URI
		/// </summary>
		public static NameValueCollection ExtractURIQueryValues(string uri) {
			NameValueCollection query = new NameValueCollection();
			int index = uri.IndexOf('#') + 1;
			string urlQuery = index == 0 ? uri : uri[index..];
			string[] queryParams = urlQuery.Split('&');
			foreach (string item in queryParams) {
				int indexOf = item.IndexOf('=');
				if (indexOf <= 0) {
					continue;
				}
				string name = item[..indexOf];
				string value = item[(indexOf + 1)..];
				if (name.Equals(TwitchWords.SCOPE)) {
					value = value.Replace("%3A", ":");
				}
				query.Add(name, value);
			}
			return query;
		}

		/// <summary>
		/// Build query parameters for URL
		/// </summary>
		private string BuildQueryPiece(string name, params string[] value) {
			if (string.IsNullOrWhiteSpace(name)) {
				return string.Empty;
			}
			int x = 0;
			int count = value.Length;
			int arrayStringsLength = count > 0 ? count - 1 : 0;
			for (; x < count; x++) {
				arrayStringsLength += value[x].Length;
			}
			x = 0;
			int nameLen = name.Length;
			Span<char> output = stackalloc char[2 + nameLen + arrayStringsLength];
			output[x++] = '&';
			int y = 0;
			for (; y < nameLen; y++) {
				output[x++] = name[y];
			}
			y = 0;
			output[x++] = '=';
			int z = 0;
			int valueLen = 0;
			for (; y < count; y++) {
				if (y > 0) {
					output[x++] = '+';
				}
				string v = value[y];
				z = 0;
				valueLen = v.Length;
				for (; z < valueLen; z++) {
					output[x++] = v[z];
				}
			}

			return new string(output);
		}

		/// <summary>
		/// Converts the scopes array into a query value
		/// </summary>
		/// <returns></returns>
		private string ScopesToString(TokenInstance token) {
			StringBuilder builder = new StringBuilder();
			string[] scopes = this.ConvertScopesToStringArray(token, true);
			int max = scopes.Length;
			int appendCheck = max - 1;
			for (int x = 0; x < max; x++) {
				builder.Append(scopes[x]);
				if (x < appendCheck) {
					builder.Append("+");
				}
			}
			return builder.ToString();
		}

		/// <summary>
		/// Build method to produce a URI on the Authorise URI
		/// </summary>
		private string BuildGrantFlowLink(string client_id, bool? force_verify, string redirect_uri, string response_type, string[] scope, string state) {
			if (string.IsNullOrWhiteSpace(client_id)) {
				throw new ArgumentException("Required input, client_id was empty, please make sure its provided before making this call.");
			}
			if (string.IsNullOrWhiteSpace(redirect_uri)) {
				throw new ArgumentException("Required input, redirect_uri was empty, please make sure its provided before making this call.");
			}
			if (scope == null || scope.Length == 0) {
				throw new ArgumentException("Required input, scope was empty, please make sure the required scopes are provided before making this call.");
			}

			StringBuilder sb = new StringBuilder(TwitchAPILinks.GetAuthData);
			sb.Append('?');

			sb.Append(this.BuildQueryPiece(TwitchWords.CLIENT_ID, client_id));

			if (force_verify.HasValue) {
				sb.Append(this.BuildQueryPiece(TwitchWords.FORCE_VERIFY, force_verify.ToString()));
			}

			sb.Append(this.BuildQueryPiece(TwitchWords.REDIRECT_URI, redirect_uri));
			sb.Append(this.BuildQueryPiece(TwitchWords.RESPONSE_TYPE, response_type));
			sb.Append(this.BuildQueryPiece(TwitchWords.SCOPE, scope));

			if (!string.IsNullOrWhiteSpace(state)) {
				sb.Append(this.BuildQueryPiece(TwitchWords.STATE, state));

				this.TwitchState = state;
			}

			return sb.ToString();
		}

		private void InvokeRefreshStarted() {
			if (!ExtendedUnityEvent.IsNullOrEmpty(this.OnAuthenticationRefreshStarted)) {
				MainThreadDispatchQueue.Enqueue(() => this.OnAuthenticationRefreshStarted.Invoke(this.currentTokenBeingRefreshed));
			}
		}

		private void WriteAuthenticationSuccess(TokenInstance token) {
			MainThreadDispatchQueue.Enqueue(() => {
				this.WriteTokenToSettings(token, this.LogDebugLevel != DebugManager.DebugLevel.None);
				if (!ExtendedUnityEvent.IsNullOrEmpty(this.OnAuthenticationSuccess)) {
					this.OnAuthenticationSuccess.Invoke(this.currentTokenBeingRefreshed);
				}
			});
		}

		private void InvokeAuthentiactionFailed(TokenInstance token, Exception ex) {
			if (!ExtendedUnityEvent.IsNullOrEmpty(this.OnAuthenticationFailure)) {
				MainThreadDispatchQueue.Enqueue(() => this.OnAuthenticationFailure.Invoke(token, ex));
			}
		}

	}
}