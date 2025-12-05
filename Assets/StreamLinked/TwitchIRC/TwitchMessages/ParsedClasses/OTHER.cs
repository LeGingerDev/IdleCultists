using ScoredProductions.StreamLinked.IRC.Extensions;
using ScoredProductions.StreamLinked.IRC.Message.Interface;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;

namespace ScoredProductions.StreamLinked.IRC.Message {

	[IRCCommandType(TwitchIRCCommand.NONE)]
	public readonly struct OTHER : ITwitchIRCMessage {


		private readonly string rawMessage;
		public readonly string RawMessage => this.rawMessage;
		public readonly TwitchIRCCommand Command => TwitchIRCCommand.NONE;

		public readonly JsonValue TagData;

		public OTHER(string data) {
			this.rawMessage = data;
			if (!string.IsNullOrEmpty(data) && data[0] == '@') {
				this.TagData = ITagContainer.ExtractTags(data[..data.IndexOf(' ')]);
			}
			else {
				this.TagData = JsonValue.Null;
			}
		}

		public readonly override string ToString() => JsonWriter.Serialize(this, true);
	}
}
