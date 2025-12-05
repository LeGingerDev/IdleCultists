namespace ScoredProductions.StreamLinked.IRC.Message.Interface {
	public interface IParsedMessage : ITwitchIRCMessage {
		public string Message { get; }
	}
}
