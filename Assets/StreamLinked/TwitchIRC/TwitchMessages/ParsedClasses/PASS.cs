using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {

	[IRCCommandType(TwitchIRCCommand.PASS)]
	public readonly struct PASS : IParsedMessage {

		//public const string PROTOTYPE = "PASS oauth:<token>\r\n";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.PASS;

		private readonly string message;
		public readonly string Message => this.message;

		public PASS(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];
			this.message = new string(messageData);
		}

		public string GetToken(bool withOauth) {
			if (withOauth) {
				if (this.message.StartsWith(TwitchWords.OAUTH)) {
					return this.message;
				} else {
					return $"{TwitchWords.OAUTH}:{this.message}";
				}
			} else {
				if (this.message.StartsWith(TwitchWords.OAUTH)) {
					return this.message[(TwitchWords.OAUTH.Length + 1)..];
				}
				else {
					return this.message;
				}
			}
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
