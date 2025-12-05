using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {

	[IRCCommandType(TwitchIRCCommand.PING)]
	public readonly struct PING : ISenderMessage {

		//public const string PROTOTYPE = "PING :tmi.twitch.tv";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.PING;

		private readonly string sender;
		public string Sender => this.sender;

		public PING(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			int seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];
			this.sender = new string(messageData);
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
