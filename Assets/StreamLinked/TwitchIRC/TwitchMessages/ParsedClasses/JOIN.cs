using System;

using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {

	[IRCCommandType(TwitchIRCCommand.JOIN)]
	public readonly struct JOIN : IUserMessage, IParsedMessage {

		//public const string PROTOTYPE = ":<user>!<user>@<user>.tmi.twitch.tv JOIN #<channel>";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.JOIN;

		private readonly string sender;
		public readonly string Sender => this.sender;
		private readonly string user;
		public readonly string User => this.user;

		private readonly string message;
		public readonly string Message => this.message;

		public JOIN(string data) {
			this.rawMessage = data;
			ReadOnlySpan<char> messageData = data.AsSpan();

			if (messageData.EndsWith(TwitchWords.END_MESSAGE_TAG.AsSpan())) {
				messageData = messageData[..^2];
			}

			this.user = new string(messageData[1..messageData.IndexOf('!')]);

			int seperator = messageData.IndexOf(' ');
			this.sender = new string(messageData[1..seperator++]);
			messageData = messageData[seperator..];
			seperator = messageData.IndexOf(' ');
			messageData = messageData[++seperator..];
			this.message = new string(messageData[1..]);
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
