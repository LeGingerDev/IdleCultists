using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {
	[IRCCommandType(TwitchIRCCommand.CAP)]
	public readonly struct CAP : ISenderMessage {

		//public const string PROTOTYPE = ":tmi.twitch.tv CAP * ACK :twitch.tv/membership twitch.tv/tags twitch.tv/commands";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.CAP;

		private readonly string sender;
		public string Sender => this.sender;

		/// <summary>
		/// True == ACK, False == NAK
		/// </summary>
		public readonly bool ACK;
		public readonly bool REQ;
		public readonly string[] Capabilities;

		public CAP(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();
			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int seperator;
			if (messageData[0] == ':') {
				seperator = messageData.IndexOf(' ');
				this.sender = new string(messageData[..seperator++]);
				messageData = messageData[seperator..];
			}
			else {
				this.sender = string.Empty;
			}

			seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];
			if (messageData[0] == '*') {
				this.REQ = false;
				this.ACK = messageData.IndexOf(TwitchWords.ACK.AsSpan()) > 0;

			}
			else {
				this.REQ = true;
				this.ACK = false;
			}

			seperator = messageData.IndexOf(':');
			messageData = messageData[++seperator..];
			if (messageData.Length > 0 && !messageData.IsWhiteSpace()) {
				int count = 0;
				int x = 0;
				for (; x < messageData.Length; x++) {
					if (messageData[x] == ' ') {
						count++;
					}
				}
				if (count == 0) {
					count++;
				}
				this.Capabilities = new string[count];

				int index;
				x = 0;

				do {
					ReadOnlySpan<char> slice;
					index = messageData.IndexOf(' '); 
					if (index > 0) {
						slice = messageData[..index];
						if (messageData.Length > ++index) {
							messageData = messageData[index..];
						}
						else {
							index = -1;
						}
					}
					else {
						slice = messageData;
					}
					this.Capabilities[x++] = new string(slice);
					if (index == -1) {

					}
				} while (count > x);
			}
			else {
				this.Capabilities = new string[0];
			}
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
