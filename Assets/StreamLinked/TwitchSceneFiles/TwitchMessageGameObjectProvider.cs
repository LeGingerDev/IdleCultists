using System.Collections.Generic;

using ScoredProductions.StreamLinked.IRC;
using ScoredProductions.StreamLinked.IRC.Message;

using UnityEngine;
using UnityEngine.UI;

namespace ScoredProductions.StreamLinked.TwitchSceneFiles {
	public class TwitchMessageGameObjectProvider : MonoBehaviour {

		public GameObject ListObjectLocation;
		public GameObject PrefabTwitchMessage;
		public ScrollRect scrollBar;
		public int MessageLimit = 200;

		private TwitchIRCClient ircClient;

		private readonly Queue<PRIVMSG> messageQueue = new Queue<PRIVMSG>();
		private readonly Queue<GameObject> createdQueue = new Queue<GameObject>();

		private void Awake() {
			if (this.ListObjectLocation == null || this.PrefabTwitchMessage == null) {
				this.enabled = false;
			}
		}

		private void OnEnable() {
			if (TwitchIRCClient.CreateOrGetInstance(out this.ircClient)) {
				this.ircClient.OnPRIVMSG.AddListener(this.TwitchIRCClient_OnMessageReceived);
			}
			else {
				this.enabled = false;
			}
		}

		private void OnDisable() {
			if (this.ircClient != null) {
				this.ircClient.OnPRIVMSG.RemoveListener(this.TwitchIRCClient_OnMessageReceived);
			}
		}

		private void Update() {
			if (this.MessageLimit < 1) {
				this.MessageLimit = 1;
			}

			if (this.messageQueue.TryDequeue(out PRIVMSG message)) {
				this.BuildMessage(message);
			}

			while (this.createdQueue.Count > this.MessageLimit) {
				Destroy(this.createdQueue.Dequeue());
			}
		}

		private void TwitchIRCClient_OnMessageReceived(PRIVMSG obj) {
			this.messageQueue.Enqueue(obj);
		}

		private void BuildMessage(PRIVMSG twitchMessage) {
			GameObject newMessage = Instantiate(this.PrefabTwitchMessage);
			this.createdQueue.Enqueue(newMessage);
			newMessage.transform.SetParent(this.ListObjectLocation.transform);
			RectTransform messageRec = newMessage.GetComponent<RectTransform>();
			messageRec.offsetMax = Vector2.zero;
			messageRec.offsetMin = Vector2.zero;
			TwitchMessageGameObject messageClass = newMessage.GetComponent<TwitchMessageGameObject>();
			messageClass.ReceiveTwitchMessage(twitchMessage);

			if (this.scrollBar) {
				this.scrollBar.verticalNormalizedPosition = 0;
			}
		}

	}
}
