using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using ScoredProductions.StreamLinked.API.Auth;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API {

	public partial class TwitchAPIClient // .AuthorizationCodeGrantFlow
	{

		/// <summary>
		/// Start of Authorization Grant Flow token aquisition process
		/// </summary>
		private void GetAuthorizationGrantFlowToken(bool aquireFresh) {
			if (string.IsNullOrWhiteSpace(this.TwitchSecret)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("API Attempted to get a new OAuth without a Twitch Secret, please provide one and try again.".RichTextColour("red"));
				}
				this.currentTokenBeingRefreshed = null;
				return;
			}
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

				bool refreshCondition = this.currentTokenBeingRefreshed.OAuthToken is AuthorizationCodeGrantFlow ACGF && !string.IsNullOrEmpty(ACGF.Refresh_Token);

				if (aquireFresh || !refreshCondition) {
					string request = this.BuildGrantFlowLink(this.TwitchClientID,
							null,
							this.currentTokenBeingRefreshed.RedirectURI,
							TwitchWords.CODE,
							this.ConvertScopesToStringArray(this.currentTokenBeingRefreshed, true),
							Guid.NewGuid().ToString("N"));

					if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
						DebugManager.LogMessage($"Starting Auth Request: {request}".RichTextColour("olive"));
					}

					if (this.currentTokenBeingRefreshed.CreateLocalHostServer) {
						this.StartLocalWebServer(AuthRequestType.AuthorizationCodeGrantFlow);
					}

					MainThreadDispatchQueue.Enqueue(() => Application.OpenURL(request));
				}
				else {
					Task.Run(this.GetAuthorizationGrantFlowRefreshToken);
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
		/// Data received from Twitch to continue and build the next part of the token
		/// </summary>
		private void AuthorizationCodeGrantFlowCallback(IAsyncResult result) {
			HttpListener httpListener = (HttpListener)result.AsyncState;
			HttpListenerContext httpContext = httpListener.EndGetContext(result);
			HttpListenerResponse httpResponse = httpContext.Response;

			NameValueCollection query = httpContext.Request.QueryString;

			// Respond after message received and close
			byte[] outbuffer = Encoding.UTF8.GetBytes(this.ResponseBuilder(AuthRequestType.AuthorizationCodeGrantFlow));

			httpResponse.ContentLength64 = outbuffer.Length;
			Stream output = httpResponse.OutputStream;
			output.Write(outbuffer, 0, outbuffer.Length);
			output.Close();

			string state = query.Get(TwitchWords.STATE);

			httpListener.Stop();
			this.HttpListenerCancellationToken?.Cancel();

			if (this.currentTokenBeingRefreshed == null) {
				return;
			}

			try {
				if (!this.TwitchState.Equals(state)) {
					if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage("State returned from Twitch did not match locally sent value, State has errored".RichTextColour("red"), DebugManager.ErrorLevel.Exception);
					}
				}
				else {
					TwitchAPIDataContainer<GetAuthorizationCodeToken> returnData = MakeTwitchAPIRequestAsync<GetAuthorizationCodeToken>(
							this.currentTokenBeingRefreshed,
							Body: GetAuthorizationCodeToken.GenerateBodyString(this.TwitchClientID, this.TwitchSecret, query[TwitchWords.CODE], this.currentTokenBeingRefreshed.RedirectURI),
							cancelToken: this.RequestAPICancellationToken.Token).Result; // Its already on a seperate thread and as its a callback it cant be a task

					if (returnData.HasErrored) {
						throw new Exception(returnData.ErrorToJson());
					}
					else {
						if (this.currentTokenBeingRefreshed != null) {
							this.currentTokenBeingRefreshed.OAuthToken = new AuthorizationCodeGrantFlow(returnData.data[0]);
							TokenInstance token = this.currentTokenBeingRefreshed;
							this.WriteAuthenticationSuccess(token);
						}
					}
				}
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, ex);
			} finally {
				this.currentTokenBeingRefreshed = null;
				this.TwitchState = null;
			}
		}

		/// <summary>
		/// Uses the tokens Refresh details to refresh the Auth token
		/// </summary>
		/// <returns></returns>
		private async Task GetAuthorizationGrantFlowRefreshToken() {
			if (string.IsNullOrWhiteSpace(this.TwitchSecret)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("API Attempted to get a new OAuth without a Twitch Secret, please provide one and try again.".RichTextColour("red"));
				}
				this.currentTokenBeingRefreshed = null;
				return;
			}
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
				if (this.currentTokenBeingRefreshed.OAuthToken is AuthorizationCodeGrantFlow ACGF) {
					TwitchAPIDataContainer<GetTokenRefresh> tokenRefresh = await MakeTwitchAPIRequestAsync<GetTokenRefresh>(
							this.currentTokenBeingRefreshed,
							Body: GetTokenRefresh.GenerateBodyString(this.TwitchClientID, ACGF.Refresh_Token, this.TwitchSecret),
							cancelToken: this.RequestAPICancellationToken.Token);

					if (tokenRefresh.HasErrored) {
						throw new Exception(tokenRefresh.ErrorToJson());
					}
					else {
						GetTokenRefresh token = tokenRefresh.data[0];

						TwitchAPIDataContainer<GetValidatedTokenInfo> tokenValidatedInfo = await MakeTwitchAPIRequestAsync<GetValidatedTokenInfo>(
							this.currentTokenBeingRefreshed,
							new (string, string)[] {
								(GetValidatedTokenInfo.AUTHORIZATION, TwitchStatic.AppendBearerToOAuth(token.access_token))
							},
							cancelToken: this.RequestAPICancellationToken.Token);

						if (tokenValidatedInfo.HasErrored) {
							throw new Exception(tokenValidatedInfo.ErrorToJson());
						}
						else if (this.currentTokenBeingRefreshed != null) {
							this.currentTokenBeingRefreshed.OAuthToken = new AuthorizationCodeGrantFlow(token, tokenValidatedInfo.data[0]);
							TokenInstance tokenInst = this.currentTokenBeingRefreshed;
							this.WriteAuthenticationSuccess(tokenInst);
						}
					}
				}
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, ex);
			} finally {
				this.TwitchState = null;
				this.currentTokenBeingRefreshed = null;
			}
		}

	}
}
