using System;

namespace ScoredProductions.StreamLinked.IRC.Message.Interface {

	// https://dev.twitch.tv/docs/chat/irc/#notice-reference

	public interface ITwitchIRCMessage {
		public string RawMessage { get; }
		public TwitchIRCCommand Command { get; }

		public static TwitchIRCCommand EstablishMessageType(string message) => EstablishMessageType(message.AsSpan());
		public static TwitchIRCCommand EstablishMessageType(ReadOnlySpan<char> message) {
			if (message == ReadOnlySpan<char>.Empty || message.Length == 0) {
				return TwitchIRCCommand.NONE;
			}
			// Netstandard 2.1 requirements, unfortunatly Enum.TryParse(ReadOnlySpan<char>...) isnt available

			int index;
			do {
				ReadOnlySpan<char> slice;
				index = message.IndexOf(' '); // 0 based index of char { T == 0, e == 1, s == 2, t == 3, ' ' == 4 }
				if (index > 0) {
					slice = message[..index]; // Range is 1 based, specifically param 2 for length { T == 1, e == 2, s == 3, t == 4, ' ' == 5 }
					if (message.Length > ++index) { // If the index after increment is not longer than the chars then we are at the end
						message = message[index..]; // 0 based selection of start index is now 5 { T == 0, e == 1, s == 2, t == 3, ' ' == 4, S == 5 }
					}
					else {
						index = -1;
					}
				}
				else {
					slice = message;
				}

				if (char.IsLetter(slice[0])) { // Looking for letter, possible starts are ':', '@' ext...
					if (slice.SequenceEqual(TwitchIRCCommand.JOIN.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.JOIN;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.NICK.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.NICK;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.NOTICE.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.NOTICE;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.PART.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.PART;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.PASS.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.PASS;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.PING.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.PING;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.PONG.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.PONG;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.PRIVMSG.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.PRIVMSG;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.CLEARCHAT.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.CLEARCHAT;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.CLEARMSG.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.CLEARMSG;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.GLOBALUSERSTATE.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.GLOBALUSERSTATE;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.RECONNECT.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.RECONNECT;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.ROOMSTATE.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.ROOMSTATE;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.USERNOTICE.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.USERNOTICE;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.USERSTATE.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.USERSTATE;
					}
					else if (slice.SequenceEqual(TwitchIRCCommand.CAP.GetAllocatedString().AsSpan())) {
						return TwitchIRCCommand.CAP;
					}
				}
			} while (index > 0); // desired check area will never be the final slot, that is either a message or channel which we shouldnt check

			// loop through the message, identify spaces, check message between spaces for command value

			return TwitchIRCCommand.NONE;
		}
	}
}
