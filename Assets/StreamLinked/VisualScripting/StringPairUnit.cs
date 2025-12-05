using Unity.VisualScripting;

namespace ScoredProductions.StreamLinked.VisualScripting {

	[UnitTitle("String Pair")]
	[UnitCategory("StreamLinked")]
	[TypeIcon(typeof(string))]
	public class StringPairUnit : Unit {

		[DoNotSerialize]
		[PortLabelHidden]
		public ValueInput LeftValue;
		
		[DoNotSerialize]
		[PortLabelHidden]
		public ValueInput RightValue;
		
		[DoNotSerialize]
		[PortLabelHidden]
		public ValueOutput Value;

		protected override void Definition() {
			this.LeftValue = this.ValueInput(nameof(this.LeftValue), "");
			this.RightValue = this.ValueInput(nameof(this.RightValue), "");
			this.Value = this.ValueOutput(nameof(this.Value), this.BuildPair).Predictable();
		}

		public StringPair BuildPair(Flow flow) {
			return new StringPair() {
				Item1 = flow.GetValue<string>(this.LeftValue),
				Item2 = flow.GetValue<string>(this.RightValue),
			};
		}
	}
}
