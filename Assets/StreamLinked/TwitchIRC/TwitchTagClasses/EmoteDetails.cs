using System;

namespace ScoredProductions.StreamLinked.IRC.Tags {

	/// <summary>
	/// Position data for an IRC chat message emote.
	/// </summary>
	public readonly struct EmoteDetails : IComparable<EmoteDetails> {
		public readonly string EmoteId;
		public readonly int Start;
		public readonly int End;
		public readonly string Name;

		public EmoteDetails(string emote, int start, int end, string name) {
			this.EmoteId = emote;
			this.Start = start;
			this.End = end;
			this.Name = name;
		}

		public readonly int CompareTo(EmoteDetails other) {
			if (this.Equals(other)) {
				return 0;
			}

			if (this.Start > other.End) {
				return 1;
			}
			if (other.Start > this.End) {
				return -1;
			}

			throw new InvalidOperationException("Sort cant place before or after value");
		}
	}
}