namespace ScoredProductions.StreamLinked.IRC.Message.Interface {
	public interface IUserMessage : ISenderMessage {
		public string User { get; }
	}
}
