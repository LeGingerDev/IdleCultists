using System;
using System.Collections;
using System.Threading.Tasks;

using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API.AuthContainers {

	//https://docs.unity3d.com/ScriptReference/ScriptableObject.html

	[Serializable]
	[CreateAssetMenu(menuName = "StreamLinked/OAuth Token")]
	public partial class TokenInstance : ScriptableObject {


		// Device code settings
		public const string AuthPending = "authorization_pending";
		public const string InvalidDeviceCode = "invalid device code";
		public const string InvalidRefreshToken = "Invalid refresh token";

		[NonSerialized]
		private bool tokenUpdateRequired = false;
		public bool TokenUpdateRequired => this.tokenUpdateRequired;

		[SerializeField]
		private AuthRequestType authenticationType;
		public AuthRequestType AuthenticationType
		{
			get => this.authenticationType;
			set
			{
				if (this.authenticationType != value) {
					this.authenticationType = value;
					this.tokenUpdateRequired = true;
					this.OAuthToken = null;
				}
			}
		}

		[SerializeField]
		private FlaggedEnum<TwitchScopesEnum> requestScopes = new FlaggedEnum<TwitchScopesEnum>();
		public FlaggedEnum<TwitchScopesEnum> RequestScopes => new FlaggedEnum<TwitchScopesEnum>(this.requestScopes);

		[Tooltip("Turn on if you would like Unity to host a local server to receive the OAuth request. Turn off if the RedirectURI address provided manages it for you and can return it to Unity.")]
		[SerializeField]
		private bool createLocalHostServer = true;
		public bool CreateLocalHostServer
		{
			get => this.createLocalHostServer;
			set { this.createLocalHostServer = value; }
		}

		[SerializeField]
		private string redirectURI = "http://localhost:3000/";
		/// <summary>
		/// Set in the Twitch Dev Console: 
		/// Will receive the result of all client authorizations: 
		/// either an access token or a failure message. 
		/// This must exactly match the redirect_uri parameter passed to the authorization endpoint. 
		/// When testing locally, you can set this to http://localhost. 
		/// A maximum of 10 redirect URLs is supported.
		/// </summary>
		public string RedirectURI
		{
			get => this.redirectURI;
			set
			{ this.redirectURI = value; }
		}

		[SerializeField]
		private bool autoRetrieveNewAuth = true;
		public bool AutoRetrieveNewAuth
		{
			get => this.autoRetrieveNewAuth;
			set { this.autoRetrieveNewAuth = value; }
		}

		[SerializeField]
		private string userProvidedWebResponse = TwitchAPIClient.WebResponseBackup;
		public string UserProvidedWebResponse
		{
			get => this.userProvidedWebResponse;
			set { this.userProvidedWebResponse = value; }
		}

		[SerializeField]
		private string userProvidedJSCode;
		public string UserProvidedJSCode
		{
			get => this.userProvidedJSCode;
			set { this.userProvidedJSCode = value; }
		}

		[SerializeField, Tooltip("For when after the code has been requested to Twitch, the API client will NOT automatically ping Twitch to Authenticate and needs to be completed by submitting via TwitchAPIClient method ReceiveDeviceCodeGrantFlowManually")]
		private bool manualRetrieval = false;
		public bool ManualRetrieval
		{
			get => this.manualRetrieval;
			set { this.manualRetrieval = value; }
		}

		[SerializeField, Tooltip("In Milliseconds")]
		private int pingInterval = 10000; // 10 seconds
		/// <summary>
		/// Milliseconds
		/// </summary>
		public int PingInterval
		{
			get => this.pingInterval;
			set { this.pingInterval = value; }
		}

		[SerializeField]
		private int pingRetries = 6; // How many pingIntervals should occure
		public int PingRetries
		{
			get => this.pingRetries;
			set { this.pingRetries = value; }
		}

		[SerializeField]
		private bool startNewOnRefreshFail = true;
		public bool StartNewOnRefreshFail
		{
			get => this.startNewOnRefreshFail;
			set { this.startNewOnRefreshFail = value; }
		}

		[field: NonSerialized] public string ExpectedDeviceCode { get; set; }

		[SerializeField]
		private string tokenID = Guid.NewGuid().ToString();
		public string TokenID
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.tokenID)) {
					this.tokenID = Guid.NewGuid().ToString();
				}
				return this.tokenID;
			}
		}

		/// <summary>
		/// Checks if the backing OAuthToken is null without attempting to load it in
		/// </summary>
		public bool HasToken => this.oAuthToken != null;

		private IAuthFlow oAuthToken;
		/// <summary>
		/// Warning, Not thread safe, do not check if this value is null if you know its null, use <c>HasToken</c> instead
		/// </summary>
		public IAuthFlow OAuthToken
		{
			get
			{
				if (this.oAuthToken == null) {
					this.LoadTokenFromSettings();
				}
				return this.oAuthToken;
			}
			set
			{
				if (value == null) {
					this.oAuthToken = null;
					this.tokenUpdateRequired = true;
					this.ClearTokenFromSettings();
					return;
				}

				bool log = false;
				if (TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
					log = client.LogDebugLevel > DebugManager.DebugLevel.Necessary;
				}
				this.oAuthToken = value;
				this.tokenUpdateRequired = false; // Force CheckRefreshNeeded to do a recheck and set the value
				this.CheckRefreshNeeded(log);
			}
		}

		private void OnEnable() {
			if (TwitchAPIClient.GetInstance(out _)) {
				this.LoadTokenFromSettings();
			}
			this.tokenUpdateRequired = false; // Reset value
		}

		/// <summary>
		/// <b>Requires</b> Main Thread
		/// </summary>
		public bool CheckRefreshNeeded(bool log = false) => CheckRefreshNeeded(this, log);

		/// <summary>
		/// <b>Requires</b> Main Thread
		/// </summary>
		public static bool CheckRefreshNeeded(TokenInstance token, bool log = false) {
			if (token == null) {
				throw new ArgumentNullException("token");
			}
			if (token.TokenUpdateRequired) {
				return true;
			}
			if (token.oAuthToken == null) {
				token.LoadTokenFromSettings(log);
			}

			if (token.oAuthToken != null
				&& token.oAuthToken.ExpiryDate > DateTime.Now
				&& token.oAuthToken.TypeEnum == token.authenticationType
				&& !string.IsNullOrWhiteSpace(token.oAuthToken.FlowName)
				&& !string.IsNullOrWhiteSpace(token.oAuthToken.Token_Type)
				&& !string.IsNullOrWhiteSpace(token.oAuthToken.Access_Token)) {

				switch (token.oAuthToken) {
					case IAppAccessToken:
						if (token.authenticationType != AuthRequestType.ClientCredentialsGrantFlow) {
							return token.tokenUpdateRequired = true;
						}
						break;
					case IUserAccessToken iuat:
						// scope check
						TwitchScopesEnum[] tokenScopes = new TwitchScopesEnum[iuat.Scope.Length];
						for (int x = 0; x < tokenScopes.Length; x++) {
							tokenScopes[x] = TwitchScopes.GetLinkedStringToEnum(iuat.Scope[x]);
						}
						TwitchScopesEnum[] storedScopes = token.RequestScopes.GetAllFlaggedAsArray();
						for (int x = 0; x < storedScopes.Length; x++) {
							bool found = false;
							for (int y = 0; y < tokenScopes.Length; y++) {
								if (storedScopes[x] == tokenScopes[y]) {
									found = true;
									break;
								}
							}
							if (!found) {
								return token.tokenUpdateRequired = true;
							}
						}

						switch (iuat) {
							case ImplicitGrantFlow:
								if (token.authenticationType != AuthRequestType.ImplicitGrantFlow) {
									return token.tokenUpdateRequired = true;
								}
								break;
							case AuthorizationCodeGrantFlow:
								if (token.authenticationType != AuthRequestType.AuthorizationCodeGrantFlow) {
									return token.tokenUpdateRequired = true;
								}
								break;
							case DeviceCodeGrantFlow:
								if (token.authenticationType != AuthRequestType.DeviceCodeGrantFlow) {
									return token.tokenUpdateRequired = true;
								}
								break;
						}
						break;
					default:
						return token.tokenUpdateRequired = true;

				}
				return token.tokenUpdateRequired = false;
			}
			else {
				return token.tokenUpdateRequired = true;
			}
		}

		/// <summary>
		/// Wraps CheckRefreshNeeded into a coroutine and sends it to the Twitch API Clients main thread it to do the check
		/// </summary>
		/// <param name="log"></param>
		/// <returns></returns>
		public Task<bool> CheckRefreshNeededAsync(bool log = false) {
			return CheckRefreshNeededAsync(this, log);
		}

		/// <summary>
		/// Wraps CheckRefreshNeeded into a coroutine and sends it to the Twitch API Clients main thread it to do the check
		/// </summary>
		/// <param name="log"></param>
		/// <returns></returns>
		public static async Task<bool> CheckRefreshNeededAsync(TokenInstance token, bool log = false) {
			IEnumerator CheckRefreshNeededWrapper(bool log = false) {
				yield return CheckRefreshNeeded(token, log);
			}

			if (TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
				IEnumerator work = CheckRefreshNeededWrapper(log);
				client.QueueActionOnMainThread(() => client.StartCoroutine(work));
				while (work.Current == null) {
					await Task.Delay(100);
				}
				return (bool)work.Current;
			}
			DebugManager.LogMessage("TwitchAPIClient not found, please ensure it exists in a scene before checking tokens.", DebugManager.ErrorLevel.Assertion);
			return false;
		}

		/// <summary>
		/// Method to add scope, notifies to get a new token on new scope added
		/// </summary>
		public void AddScope(params TwitchScopesEnum[] scopes) {
			if (this.requestScopes.Add(scopes)) {
				this.tokenUpdateRequired = true;
			}
		}

		/// <summary>
		/// Method to add scope, notifies to get a new token on new scope added
		/// </summary>
		public void AddScope(FlaggedEnum<TwitchScopesEnum> scopes) {
			if (this.requestScopes.Add(scopes)) {
				this.tokenUpdateRequired = true;
			}
		}

		/// <summary>
		/// Removes scopes from current scopes
		/// </summary>
		public void RemoveScope(params TwitchScopesEnum[] scopes) {
			this.requestScopes.Subtract(scopes);
		}

		/// <summary>
		/// Removes scopes from current scopes
		/// </summary>
		public void RemoveScope(FlaggedEnum<TwitchScopesEnum> scopes) {
			this.requestScopes.Subtract(scopes);
		}

		public JsonValue RetrieveTokenAsJson() {
			return new JsonObject() {
				{ TwitchWords.ID, this.TokenID },
				{ TwitchWords.DATA, this.oAuthToken?.AsJsonValue() ?? "{ }" },
			};
		}

		/// <summary>
		/// <b>Requires</b> Main Thread
		/// </summary>
		public void LoadTokenFromSettings(bool log = false) {
			if (TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
				client.LoadTokenFromStorage(this, log);
			}
			else {
				DebugManager.LogMessage("TwitchAPIClient not found, please ensure it exists in a scene before loading tokens.", DebugManager.ErrorLevel.Assertion);
			}
		}

		/// <summary>
		/// <b>Requires</b> Main Thread
		/// </summary>
		public void ClearTokenFromSettings(bool log = false) {
			if (TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
				client.CleanPlayerPrefTokens(log, this.tokenID);
			}
			else {
				DebugManager.LogMessage("TwitchAPIClient not found, please ensure it exists in a scene before loading tokens.", DebugManager.ErrorLevel.Assertion);
			}
		}

		public void PerformScopeCheck(in APIScopeWarning ScopeSettings, in IScope reference) {
			if (ScopeSettings != APIScopeWarning.None && !this.RequestScopes.HasFlag(reference.Scopes)) {
				if (ScopeSettings.HasFlag(APIScopeWarning.AddMissingScopes)) {
					this.AddScope(reference.Scopes);
				}
				if (ScopeSettings.HasFlag(APIScopeWarning.ThrowOnMissing)) {
					throw new Exception($"Scopes are missing from the provided credentials when making a request to {reference.GetType().Name}, request cancelled.");
				}
				if (ScopeSettings.HasFlag(APIScopeWarning.WarnOnMissing)) {
					DebugManager.LogMessage($"Scopes are missing from the provided credentials when making a request to {reference.GetType().Name}", DebugManager.ErrorLevel.Warning);
				}
			}
		}
	}
}
