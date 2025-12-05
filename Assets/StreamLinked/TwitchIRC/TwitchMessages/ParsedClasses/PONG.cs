using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {

	[IRCCommandType(TwitchIRCCommand.PONG)]
	public readonly struct PONG : ISenderMessage {

		//public const string PROTOTYPE = "PONG :tmi.twitch.tv";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.PONG;

		private readonly string fullSender;
		public string Sender => this.fullSender;

		public PONG(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> spanData = data.AsSpan();
			int seperator = spanData.IndexOf(' ');
			spanData = spanData[++seperator..];
			if (spanData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				this.fullSender = new string(spanData[..^2]);
			}
			else {
				this.fullSender = new string(spanData);
			}
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
