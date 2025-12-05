using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {

	[IRCCommandType(TwitchIRCCommand.PART)]
	public readonly struct PART : IParsedMessage {

		//public const string PROTOTYPE = "PART #<channel>\r\n";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.PART;

		private readonly string message;
		public readonly string Message => this.message;

		public PART(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];
			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				this.message = new string(messageData[..^2]);
			}
			else {
				this.message = new string(messageData);
			}
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
