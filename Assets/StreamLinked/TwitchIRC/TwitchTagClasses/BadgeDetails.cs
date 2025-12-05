namespace ScoredProductions.StreamLinked.IRC.Tags {

	public readonly struct BadgeDetails {
		public readonly string Name;
		public readonly int Value;

		public BadgeDetails(string name, int value) {
			this.Name = name;
			this.Value = value;
		}
	}
}