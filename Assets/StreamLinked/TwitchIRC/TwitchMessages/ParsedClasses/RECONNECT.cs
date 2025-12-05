using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {
	/// <summary>
	/// <see href="https://dev.twitch.tv/docs/chat/irc/#reconnect-command">Twitch IRC Command</see>: Sent when the Twitch IRC server needs to terminate the connection.
	/// </summary>
	[IRCCommandType(TwitchIRCCommand.RECONNECT)]
	public readonly struct RECONNECT : ISenderMessage {

		//public const string PROTOTYPE = ":tmi.twitch.tv RECONNECT";

		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.RECONNECT;

		private readonly string sender;
		public string Sender => this.sender;

		public RECONNECT(string data) {
			this.rawMessage = data;
			this.sender = data[..data.IndexOf(' ')];
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
