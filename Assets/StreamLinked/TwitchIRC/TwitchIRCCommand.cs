using System;

namespace ScoredProductions.StreamLinked.IRC {

	[Serializable]
	public enum TwitchIRCCommand : byte {
		NONE = 0,
		JOIN = 1,
		NICK = 2,
		NOTICE = 3,
		PART = 4,
		PASS = 5,
		PING = 6,
		PONG = 7,
		PRIVMSG = 8,
		CLEARCHAT = 9,
		CLEARMSG = 10,
		GLOBALUSERSTATE = 11,
		//HOSTTARGET = 12,
		RECONNECT = 13,
		ROOMSTATE = 14,
		USERNOTICE = 15,
		USERSTATE = 16,
		CAP = 17,
	}


	public static class TwitchIRCCommandExtensions {

		// Reduce object allocation to a pre allocated set to stop constant generation of strings
		private static readonly string _JOIN = TwitchIRCCommand.JOIN.ToString();
		private static readonly string _NICK = TwitchIRCCommand.NICK.ToString();
		private static readonly string _NOTICE = TwitchIRCCommand.NOTICE.ToString();
		private static readonly string _PART = TwitchIRCCommand.PART.ToString();
		private static readonly string _PASS = TwitchIRCCommand.PASS.ToString();
		private static readonly string _PING = TwitchIRCCommand.PING.ToString();
		private static readonly string _PONG = TwitchIRCCommand.PONG.ToString();
		private static readonly string _PRIVMSG = TwitchIRCCommand.PRIVMSG.ToString();
		private static readonly string _CLEARCHAT = TwitchIRCCommand.CLEARCHAT.ToString();
		private static readonly string _CLEARMSG = TwitchIRCCommand.CLEARMSG.ToString();
		private static readonly string _GLOBALUSERSTATE = TwitchIRCCommand.GLOBALUSERSTATE.ToString();
		private static readonly string _RECONNECT = TwitchIRCCommand.RECONNECT.ToString();
		private static readonly string _ROOMSTATE = TwitchIRCCommand.ROOMSTATE.ToString();
		private static readonly string _USERNOTICE = TwitchIRCCommand.USERNOTICE.ToString();
		private static readonly string _USERSTATE = TwitchIRCCommand.USERSTATE.ToString();
		private static readonly string _CAP = TwitchIRCCommand.CAP.ToString();

		/// <summary>
		/// Gets the allocated readonly static string created from the enum, ToString creates a new one, this returns a pre existing one
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GetAllocatedString(this TwitchIRCCommand value) {
			return value switch {
				TwitchIRCCommand.JOIN => _JOIN,
				TwitchIRCCommand.NICK => _NICK,
				TwitchIRCCommand.NOTICE => _NOTICE,
				TwitchIRCCommand.PART => _PART,
				TwitchIRCCommand.PASS => _PASS,
				TwitchIRCCommand.PING => _PING,
				TwitchIRCCommand.PONG => _PONG,
				TwitchIRCCommand.PRIVMSG => _PRIVMSG,
				TwitchIRCCommand.CLEARCHAT => _CLEARCHAT,
				TwitchIRCCommand.CLEARMSG => _CLEARMSG,
				TwitchIRCCommand.GLOBALUSERSTATE => _GLOBALUSERSTATE,
				TwitchIRCCommand.RECONNECT => _RECONNECT,
				TwitchIRCCommand.ROOMSTATE => _ROOMSTATE,
				TwitchIRCCommand.USERNOTICE => _USERNOTICE,
				TwitchIRCCommand.USERSTATE => _USERSTATE,
				TwitchIRCCommand.CAP => _CAP,
				_ => null,
			};
		}

	}
}
