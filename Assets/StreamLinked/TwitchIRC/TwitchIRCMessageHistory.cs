using System;
using System.Collections;
using System.Collections.Generic;

using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;

namespace ScoredProductions.StreamLinked.IRC {

	public class TwitchIRCMessageHistory : SingletonInstance<TwitchIRCMessageHistory> {
		public override bool PersistBetweenScenes => true;

		[Range(1, int.MaxValue)]
		public int MessagesToStore = 1000;

		[Tooltip("When a message is removed from the store due to the store being exceeded, acts as the last chance to get the message.")]
		public ExtendedUnityEvent<ITwitchIRCMessage> OnMessageDiscarded;

		private Queue<ITwitchIRCMessage> ReceivedMessages;
		/// <summary>
		/// Returns a copy of the stored received messages
		/// </summary>
		public static List<ITwitchIRCMessage> StoredMessages => new List<ITwitchIRCMessage>(GetInstance(out TwitchIRCMessageHistory instance) ? instance.ReceivedMessages : Array.Empty<ITwitchIRCMessage>());

		private TwitchIRCClient ircClient;

		private bool listenerLoaded = false;

		private void OnEnable() {
			if (this.ReceivedMessages == null) {
				this.ReceivedMessages = new Queue<ITwitchIRCMessage>(this.MessagesToStore);
			}
			this.ReconnectToIRCInstance();
		}

		private void OnDisable() {
			this.DisconnectFromIRCEvents();
		}

		private void ConnectToIRCEvents() {
			if (this.listenerLoaded) {
				this.ircClient.OnANY.RemoveListener(this.ReceiveTwitchIRCMessage);
			}
			this.ircClient.OnANY.AddListener(this.ReceiveTwitchIRCMessage);
			this.listenerLoaded = true;
		}

		private void DisconnectFromIRCEvents() {
			if (this.listenerLoaded) {
				this.ircClient.OnANY.RemoveListener(this.ReceiveTwitchIRCMessage);

				this.listenerLoaded = false;
			}
		}

		public void ReconnectToIRCInstance() {
			this.DisconnectFromIRCEvents();
			this.ircClient = null;
			this.StartCoroutine(this.GetIRCInstance());
		}

		private IEnumerator GetIRCInstance() {
			while (this.ircClient == null) {
				if (!TwitchIRCClient.GetInstance(out this.ircClient)) {
					yield return TwitchStatic.OneSecondWait;
				}
			}
			this.ConnectToIRCEvents();
		}

		public void ReceiveTwitchIRCMessage(ITwitchIRCMessage message) {
			this.ReceivedMessages.Enqueue(message);

			while (this.ReceivedMessages.Count > this.MessagesToStore) {
				if (ExtendedUnityEvent.IsNullOrEmpty(this.OnMessageDiscarded)) {
					this.ReceivedMessages.Dequeue();
				} else {
					this.OnMessageDiscarded.Invoke(this.ReceivedMessages.Dequeue());
				}
			}
		}
	}
}