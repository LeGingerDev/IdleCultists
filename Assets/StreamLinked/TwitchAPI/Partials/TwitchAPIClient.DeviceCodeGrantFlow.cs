using System;
using System.Collections;

using ScoredProductions.StreamLinked.API.Auth;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API {

	public partial class TwitchAPIClient // .DeviceCodeGrantFlow
	{

		/// <summary>
		/// Start of Device Code Grant Flow token aquisition process
		/// </summary>
		private void GetDeviceCodeGrantFlowToken(bool aquireFresh) {
			if (string.IsNullOrWhiteSpace(this.TwitchClientID)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("API Attempted to get a new OAuth without a Twitch ClientID, please provide one and try again.".RichTextColour("red"));
				}
				this.currentTokenBeingRefreshed = null;
				return;
			}
			if (!this.GettingNewToken) {
				return;
			}

			try {
				this.InvokeRefreshStarted();

				if (aquireFresh) {
					this.StartCoroutine(this.GetDeviceCodeGrantFlowInitial());
				}
				else {
					if (this.currentTokenBeingRefreshed.OAuthToken is DeviceCodeGrantFlow DCGF && !string.IsNullOrEmpty(DCGF.Refresh_Token)) {
						this.StartCoroutine(this.GetDeviceCodeGrantFlowRefresh());
					}
					else {
						this.StartCoroutine(this.GetDeviceCodeGrantFlowInitial());
					}
				}

			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, ex);
				this.TwitchState = null;
				this.currentTokenBeingRefreshed = null;
			}
		}

		/// <summary>
		/// Method to aquire a new Device Code Grant Flow Token
		/// </summary>
		private IEnumerator GetDeviceCodeGrantFlowInitial() {
			if (string.IsNullOrWhiteSpace(this.TwitchClientID)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("API Attempted to get a new OAuth without a Twitch ClientID, please provide one and try again.".RichTextColour("red"));
				}
				this.currentTokenBeingRefreshed = null;
				yield break;
			}
			if (!this.GettingNewToken) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("No token is currently on request, flow cancelled", DebugManager.ErrorLevel.Warning);
				}
				yield break;
			}

			IEnumerator initialTokenContainer = MakeTwitchAPIRequest<GetDeviceCodeGrantFlow>(
				this.currentTokenBeingRefreshed,
					QueryParameters: new (string, string)[] {
						(GetDeviceCodeGrantFlow.CLIENT_ID, this.TwitchClientID),
						(GetDeviceCodeGrantFlow.SCOPES, this.ScopesToString(this.currentTokenBeingRefreshed))
					},
					cancelToken: this.RequestAPICancellationToken.Token);

			yield return initialTokenContainer;

			TwitchAPIDataContainer<GetDeviceCodeGrantFlow> initialTokenRequest = (TwitchAPIDataContainer<GetDeviceCodeGrantFlow>)initialTokenContainer.Current;

			if (!initialTokenRequest.HasErrored && this.currentTokenBeingRefreshed != null) {
				GetDeviceCodeGrantFlow tempDeviceContainer = initialTokenRequest.data[0];
				this.currentTokenBeingRefreshed.ExpectedDeviceCode = tempDeviceContainer.device_code;

				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage($"Starting Device Code Confirm Request Code: {{{tempDeviceContainer.user_code}}} URL: {{{tempDeviceContainer.verification_uri}}}".RichTextColour("olive"));
				}

				MainThreadDispatchQueue.Enqueue(() => Application.OpenURL(tempDeviceContainer.verification_uri));

				if (this.currentTokenBeingRefreshed.ManualRetrieval) {
					DebugManager.LogMessage($"Device Code Retrieval settings has been set to Manual, flow has ended as is awaiting the token to be supplied to the API Client via ReceiveDeviceCodeGrantFlowManually(GetDeviceCodeGrantAuthorisation data).".RichTextColour("olive"));
				}
				else {
					int retryCount = 0;
					WaitForSeconds waitDelay = new WaitForSeconds(this.currentTokenBeingRefreshed.PingInterval / 1000);
					do {
						yield return waitDelay;

						IEnumerator tokenRequestAuthorisationContainer = MakeTwitchAPIRequest<GetDeviceCodeGrantAuthorisation>(
							this.currentTokenBeingRefreshed,
							QueryParameters: new (string, string)[] {
									(GetDeviceCodeGrantAuthorisation.CLIENT_ID, this.TwitchClientID),
									(GetDeviceCodeGrantAuthorisation.SCOPES, this.ScopesToString(this.currentTokenBeingRefreshed)),
									(GetDeviceCodeGrantAuthorisation.DEVICE_CODE, tempDeviceContainer.device_code),
									GetDeviceCodeGrantAuthorisation.GRANT_TYPE,
							},
							cancelToken: this.RequestAPICancellationToken.Token);

						yield return tokenRequestAuthorisationContainer;

						TwitchAPIDataContainer<GetDeviceCodeGrantAuthorisation> tokenRequestAuthorisation = (TwitchAPIDataContainer<GetDeviceCodeGrantAuthorisation>)tokenRequestAuthorisationContainer.Current;

						if (!this.GettingNewToken) {
							yield break;
						}

						if (tokenRequestAuthorisation.HasErrored) {
							string errorMessage = null;
							JsonValue errorBody = JsonReader.Parse(tokenRequestAuthorisation.RawResponse);
							if (tokenRequestAuthorisation.status == 400) {
								switch (errorBody[TwitchWords.MESSAGE].AsString) {
									case TokenInstance.InvalidRefreshToken:
										errorMessage = $"API call GetDeviceCodeGrantAuthorisation failed due to an invalid refresh token: {tokenRequestAuthorisation.ErrorText}";
										break;
									case TokenInstance.InvalidDeviceCode:
										errorMessage = $"API call GetDeviceCodeGrantAuthorisation failed due to an invalid device code submission: {tokenRequestAuthorisation.ErrorText}";
										break;
									case TokenInstance.AuthPending:
										if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
											DebugManager.LogMessage("DeviceCode Authorisation is still pending, queuing retry".RichTextColour("orange"));
										}
										continue;
								}
							}

							if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
								if (string.IsNullOrEmpty(errorMessage)) {
									errorMessage = $"API call GetDeviceCodeGrantAuthorisation failed: {tokenRequestAuthorisation.ErrorToJson()}";
								}
								DebugManager.LogMessage(errorMessage, DebugManager.ErrorLevel.Error);
							}
							this.currentTokenBeingRefreshed = null;
							this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, null);
						}
						else {
							GetDeviceCodeGrantAuthorisation container = tokenRequestAuthorisation.data[0];

							if (this.currentTokenBeingRefreshed != null) {
								this.currentTokenBeingRefreshed.OAuthToken = new DeviceCodeGrantFlow(container);
								TokenInstance token = this.currentTokenBeingRefreshed;
								this.WriteAuthenticationSuccess(token);
							}
							this.currentTokenBeingRefreshed = null;
							yield break;
						}
					} while (++retryCount < this.currentTokenBeingRefreshed.PingRetries);

					if (this.LogDebugLevel == DebugManager.DebugLevel.Full) {
						DebugManager.LogMessage("Ping count for automatic retry of GetDeviceCodeGrantAuthorisation as been exceeded without success, please request a new token or attempt to manually retreive on client confirmation.", DebugManager.ErrorLevel.Error);
					}
					this.currentTokenBeingRefreshed = null;
					this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, null);
				}
			}
		}

		/// <summary>
		/// Starts process to refresh current Device token
		/// </summary>
		private IEnumerator GetDeviceCodeGrantFlowRefresh() {
			if (string.IsNullOrWhiteSpace(this.TwitchClientID)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("API Attempted to get a new OAuth without a Twitch ClientID, please provide one and try again.".RichTextColour("red"));
				}
				this.currentTokenBeingRefreshed = null;
				yield break;
			}
			if (!this.GettingNewToken) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("No token is currently on request, flow cancelled", DebugManager.ErrorLevel.Warning);
				}
				yield break;
			}

			if (this.currentTokenBeingRefreshed.OAuthToken is DeviceCodeGrantFlow DCGF) {
				IEnumerator refreshContainer = MakeTwitchAPIRequest<GetTokenRefresh>(
					this.currentTokenBeingRefreshed,
					Body: GetTokenRefresh.GenerateBodyString(this.TwitchClientID, DCGF.Refresh_Token, this.TwitchSecret),
					cancelToken: this.RequestAPICancellationToken.Token);

				yield return refreshContainer;

				if (this.RequestAPICancellationToken.IsCancellationRequested) {
					yield break;
				}

				if (!this.GettingNewToken) {
					if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage("No token is currently on request, flow cancelled", DebugManager.ErrorLevel.Warning);
					}
					yield break;
				}

				TwitchAPIDataContainer<GetTokenRefresh> tokenRequest = (TwitchAPIDataContainer<GetTokenRefresh>)refreshContainer.Current;

				if (tokenRequest.HasErrored) {
					if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage(tokenRequest.ErrorToJson(), DebugManager.ErrorLevel.Error);
					}

					if (this.currentTokenBeingRefreshed.StartNewOnRefreshFail) {
						this.StartCoroutine(this.GetDeviceCodeGrantFlowInitial());
					}
					else {
						this.currentTokenBeingRefreshed = null;

						this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, null);
					}
					yield break;
				}
				else {
					GetTokenRefresh container = tokenRequest.data[0];

					DeviceCodeGrantFlow refreshedToken = new DeviceCodeGrantFlow(container);

					IEnumerator validationContainer = MakeTwitchAPIRequest<GetValidatedTokenInfo>(
						this.currentTokenBeingRefreshed,
						new (string, string)[] {
								(GetValidatedTokenInfo.AUTHORIZATION, TwitchStatic.AppendBearerToOAuth(refreshedToken.Access_Token))
						},
						cancelToken: this.RequestAPICancellationToken.Token);

					yield return validationContainer;

					TwitchAPIDataContainer<GetValidatedTokenInfo> tokenValidatedInfo = (TwitchAPIDataContainer<GetValidatedTokenInfo>)validationContainer.Current;

					if (tokenValidatedInfo.HasErrored) {
						this.currentTokenBeingRefreshed = null;
						if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
							DebugManager.LogMessage(tokenValidatedInfo.ErrorToJson(), DebugManager.ErrorLevel.Error);
						}
						this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, null);
						yield break;
					}
					else {
						GetValidatedTokenInfo tokenData = tokenValidatedInfo.data[0];

						if (tokenData.expires_in > int.MinValue) {
							refreshedToken.Expires_In = tokenData.expires_in;
						}

						if (this.currentTokenBeingRefreshed != null) {
							this.currentTokenBeingRefreshed.OAuthToken = refreshedToken;
							TokenInstance token = this.currentTokenBeingRefreshed;
							this.WriteAuthenticationSuccess(token);
						}
					}
				}
			}
			else {
				this.currentTokenBeingRefreshed = null;
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("UserAccessToken is not of type DeviceCodeGrantFlow, Refresh failed");
				}
				this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, null);
			}
		}

		/// <summary>
		/// Function to supply the Device Code Grant Flow token Directly to the API after being manually set to. Will error if not expecting code.
		/// </summary>
		/// <param name="data"></param>
		public void ReceiveDeviceCodeGrantFlowManually(GetDeviceCodeGrantAuthorisation data) {
			try {
				if (!this.GettingNewToken) {
					throw new Exception("Twitch Client API is no longer expecting a Device Code Grant Flow token and has no instance to populate.");
				}

				if (string.IsNullOrWhiteSpace(this.currentTokenBeingRefreshed.ExpectedDeviceCode)) {
					throw new Exception("Twitch Client API was not expecting a Device Code Grant Flow token. The API will only accept these when needed during a manual token wait. Please review your flow.");
				}

				this.currentTokenBeingRefreshed.OAuthToken = new DeviceCodeGrantFlow(data);
				TokenInstance token = this.currentTokenBeingRefreshed;
				this.WriteAuthenticationSuccess(token);
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, ex);
			} finally {
				this.currentTokenBeingRefreshed = null;
			}
		}

	}
}
