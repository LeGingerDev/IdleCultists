using System;
using System.Reflection;

using ScoredProductions.StreamLinked.IRC.Message.Interface;

namespace ScoredProductions.StreamLinked.IRC.Extensions {

	[Serializable]
	public class IRCCommandType : Attribute {

		public TwitchIRCCommand Command { get; private set; }

		public IRCCommandType(TwitchIRCCommand command) {
			this.Command = command;
		}
	}

	public static class CommandTypeExtensions {

		public static TwitchIRCCommand GetCommandEnum<T>(this T _) where T : ITagContainer, ITwitchIRCMessage {
			return ((IRCCommandType)typeof(T).GetCustomAttribute(typeof(IRCCommandType), true)).Command;
		}

	}
}