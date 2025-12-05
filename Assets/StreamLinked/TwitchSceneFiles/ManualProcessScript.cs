using System;
using System.Collections;

using ScoredProductions.StreamLinked.API;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.API.Users;
using ScoredProductions.StreamLinked.LightJson.Serialization;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;
using UnityEngine.UI;

namespace ScoredProductions
{
    public class ManualProcessScript : MonoBehaviour {

		public InputField UserInputField;
		public Text OutputText;
        public Button GetValidButton;
        public Button GetTokenButton;
        public Button GetUsersButton;

        private bool? TokenValid;

		private Coroutine requestRoutine;

		private bool TokenUpdating;

		private void Awake() {
			if (TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
				client.OnAuthenticationRefreshStarted.AddListener(this.TokenStartedRefresh);
				client.OnAuthenticationSuccess.AddListener(this.TokenEndedRefresh);
				client.OnAuthenticationFailure.AddListener(this.TokenRefreshFailed);
				this.UpdateButtons();
			} else {
				this.enabled = false;
			}
		}

		private void OnDestroy() {
			if (TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
				client.OnAuthenticationRefreshStarted.RemoveListener(this.TokenStartedRefresh);
				client.OnAuthenticationSuccess.RemoveListener(this.TokenEndedRefresh);
				client.OnAuthenticationFailure.RemoveListener(this.TokenRefreshFailed);
			}
		}

		private void TokenStartedRefresh(TokenInstance token) {
			this.TokenUpdating = true;
			this.UpdateButtons();
		}

		private void TokenRefreshFailed(TokenInstance token, Exception _) {
			this.TokenEndedRefresh(token);
		}

		private void TokenEndedRefresh(TokenInstance token) {
			this.TokenUpdating = false;
			this.UpdateButtons();
		}

		private void UpdateButtons() {
			if (this.TokenUpdating) {
				this.TokenValid = null;
				this.GetValidButton.interactable = false;
				this.GetTokenButton.interactable = false;
				this.GetUsersButton.interactable = false;
			} 
			else {
				this.GetValidButton.interactable = true;

				ColorBlock cb1 = this.GetValidButton.colors;
				switch (this.TokenValid) {
					case true:
						cb1.normalColor = cb1.highlightedColor = cb1.selectedColor = Color.green;
						this.GetValidButton.colors = cb1;
						this.GetTokenButton.interactable = true;
						this.GetUsersButton.interactable = !string.IsNullOrWhiteSpace(this.UserInputField.text);
						break;
					case false:
						cb1.normalColor = cb1.highlightedColor = cb1.selectedColor = Color.red;
						this.GetValidButton.colors = cb1;
						this.GetTokenButton.interactable = true;
						this.GetUsersButton.interactable = false;
						break;
					case null:
						cb1.normalColor = cb1.highlightedColor = cb1.selectedColor = Color.white;
						this.GetValidButton.colors = cb1;
						this.GetTokenButton.interactable = false;
						this.GetUsersButton.interactable = false;
						break;
				}
			}
		}

        public void CheckTokenIsValid() {
            if (TwitchAPIClient.GetInstance(out TwitchAPIClient client) && client.DefaultAPIToken != null) {
                if (client.DefaultAPIToken != null) {
					this.TokenValid = !client.DefaultAPIToken.CheckRefreshNeeded(true);
				}
				else {
					this.TokenValid = null;
                }
				this.UpdateButtons();
            }
        }

        public void GetNewToken() {
			if (TwitchAPIClient.GetInstance(out TwitchAPIClient client)) {
				if (client.DefaultAPIToken != null) {
					client.GetNewAuthToken(client.DefaultAPIToken);
				}
				this.TokenValid = null;
				this.UpdateButtons();
			}
		}

        public void GetUserData() {
			if (this.requestRoutine != null) {
				DebugManager.LogMessage("Request in progress, please wait.", DebugManager.ErrorLevel.Error);
				return;
			}

			string userName = this.UserInputField.text.Trim();

			IEnumerator enumerator = TwitchAPIClient.MakeTwitchAPIRequest<GetUsers>(QueryParameters: new (string, string)[] {
					(GetUsers.LOGIN, userName)
				},
				ScopeSettings: APIScopeWarning.None,
				SuccessCallback: this.SuccessCallback);
			this.requestRoutine = this.StartCoroutine(enumerator);
		}

		private void SuccessCallback(TwitchAPIDataContainer<GetUsers> returnedValue) {
			if (!returnedValue.HasErrored) {
				this.OutputText.text = JsonWriter.Serialize(returnedValue, true);
			}
			this.requestRoutine = null;
		}
	}
}
