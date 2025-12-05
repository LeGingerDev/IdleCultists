namespace ScoredProductions.StreamLinked.IRC.Message.Interface {
	public interface ISenderMessage : ITwitchIRCMessage {
		public string Sender { get; }
	}
}
