using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

using ScoredProductions.StreamLinked.API.Auth;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;

namespace ScoredProductions.StreamLinked.API {

	public partial class TwitchAPIClient // .ImplicitGrantFlow
	{

		/// <summary>
		/// Start of Implicit Grant Flow token aquisition process
		/// </summary>
		private void GetImplicitGrantFlowToken() {
			if (string.IsNullOrWhiteSpace(this.TwitchClientID)) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage("API Attempted to get a new OAuth without a Twitch ClientID, please provide one and try again.".RichTextColour("red"));
				}
				return;
			}
			if (!this.GettingNewToken) {
				return;
			}

			try {
				this.InvokeRefreshStarted();

				string request = this.BuildGrantFlowLink(this.TwitchClientID,
						null,
						this.currentTokenBeingRefreshed.RedirectURI,
						TwitchWords.TOKEN,
						this.ConvertScopesToStringArray(this.currentTokenBeingRefreshed, true),
						Guid.NewGuid().ToString("N"));

				if (this.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
					DebugManager.LogMessage($"Starting Auth Request: {request}".RichTextColour("olive"));
				}

				if (this.currentTokenBeingRefreshed.CreateLocalHostServer) {
					this.StartLocalWebServer(AuthRequestType.ImplicitGrantFlow);
				}

				MainThreadDispatchQueue.Enqueue(() => Application.OpenURL(request));
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
		/// Runs when token is awaiting to be received from URI
		/// </summary>
		private void ImplicitGrantFlowCallbackInitialResponse(IAsyncResult result) {
			HttpListener httpListener = (HttpListener)result.AsyncState;
			httpListener.BeginGetContext(new AsyncCallback(this.ImplicitGrantFlowCallbackBuildToken), httpListener);

			HttpListenerContext httpContext = httpListener.EndGetContext(result);
			HttpListenerResponse httpResponse = httpContext.Response;

			byte[] buffer = Encoding.UTF8.GetBytes(this.ResponseBuilder(AuthRequestType.ImplicitGrantFlow));

			// send the output to the client browser
			httpResponse.ContentLength64 = buffer.Length;
			Stream output = httpResponse.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();
		}

		/// <summary>
		/// Runs after Javascript aquires the URI and sends it back down to Unity from the browser
		/// </summary>
		private void ImplicitGrantFlowCallbackBuildToken(IAsyncResult result) {
			HttpListener httpListener = (HttpListener)result.AsyncState;
			HttpListenerContext httpContext = httpListener.EndGetContext(result);
			HttpListenerRequest httpRequest = httpContext.Request;

			string returnedURL;
			using (StreamReader reader = new StreamReader(httpRequest.InputStream, httpRequest.ContentEncoding)) {
				returnedURL = reader.ReadToEnd();
			}

			httpListener.Stop();
			this.HttpListenerCancellationToken?.Cancel();

			try {
				if (!string.IsNullOrWhiteSpace(returnedURL)) {
					NameValueCollection query = ExtractURIQueryValues(returnedURL);

					string state = query.Get(TwitchWords.STATE);

					if (this.TwitchState.Equals(state)) {
						ImplicitGrantFlow igf = new ImplicitGrantFlow(query);

						TwitchAPIDataContainer<GetValidatedTokenInfo> tokenValidatedInfo = MakeTwitchAPIRequest<GetValidatedTokenInfo>(int.MaxValue,
												this.currentTokenBeingRefreshed,
												new (string, string)[] {
													(GetValidatedTokenInfo.AUTHORIZATION, TwitchStatic.AppendBearerToOAuth(igf.Access_Token))
												});

						if (tokenValidatedInfo.HasErrored) {
							throw new Exception(tokenValidatedInfo.ErrorToJson());
						}
						else {
							GetValidatedTokenInfo tokenData = tokenValidatedInfo.data[0];

							if (tokenData.expires_in > int.MinValue) {
								igf.Expires_In = tokenData.expires_in;
							}
						}

						if (this.currentTokenBeingRefreshed != null) {
							this.currentTokenBeingRefreshed.OAuthToken = igf;
							TokenInstance token = this.currentTokenBeingRefreshed;
							this.WriteAuthenticationSuccess(token);
						}
					}
					else {
						string message = "State returned from Twitch did not match locally sent value, State has errored";
						if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
							DebugManager.LogMessage(message.RichTextColour("red"), DebugManager.ErrorLevel.Exception);
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
