using System;

using ScoredProductions.StreamLinked.API.Auth;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.Utility;

namespace ScoredProductions.StreamLinked.API {

	public partial class TwitchAPIClient // .ClientCredentialsFlow
	{

		/// <summary>
		/// Method to aquire App Access Token
		/// </summary>
		public void GetClientCredentialsFlowToken() {
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
				this.MakeTwitchAPIRequest<GetClientCredentialsGrantFlow>(
					this.GetClientCredentialsResponse,
					this.currentTokenBeingRefreshed,
					Body: GetClientCredentialsGrantFlow.GenerateBodyString(this.TwitchClientID, this.TwitchSecret));
			} catch (Exception ex) {
				if (this.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				this.InvokeAuthentiactionFailed(this.currentTokenBeingRefreshed, ex);
				this.currentTokenBeingRefreshed = null;
			}
		}

		private void GetClientCredentialsResponse(TwitchAPIDataContainer<GetClientCredentialsGrantFlow> tokenRequest) {
			if (tokenRequest.HasErrored) {
				throw new Exception(tokenRequest.ErrorToJson());
			}
			else {
				this.currentTokenBeingRefreshed.OAuthToken = new ClientCredentialsFlow(tokenRequest.data[0]);
				TokenInstance token = this.currentTokenBeingRefreshed;
				this.WriteAuthenticationSuccess(token);
			}

			this.currentTokenBeingRefreshed = null;
		}

	}
}
