using System;

namespace ScoredProductions.StreamLinked.API {
	[Flags]
	public enum APIScopeWarning {
		None = 0,
		/// <summary>
		/// Default
		/// </summary>
		WarnOnMissing = 1,
		ThrowOnMissing = 2,
		AddMissingScopes = 4,
	}
}
