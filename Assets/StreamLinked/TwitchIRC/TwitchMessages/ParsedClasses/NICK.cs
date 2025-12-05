using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {

	[IRCCommandType(TwitchIRCCommand.NICK)]
	public readonly struct NICK : ITwitchIRCMessage {

		//public const string PROTOTYPE = "NICK <login>";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.NICK;

		public readonly string Login;

		public NICK(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];
			this.Login = new string(messageData[1..]);
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
