#if UNITY_EDITOR
namespace ScoredProductions.StreamLinked.Editor {
	using ScoredProductions.StreamLinked.EventSub;
	using ScoredProductions.StreamLinked.TwitchSceneFiles;

	using UnityEditor;

	using UnityEngine;

	[CustomEditor(typeof(EventSubCustomRewardStartupExample))]
	public class EventSubCustomRewardStartupExampleEditor : Editor {

		private string builtString;

		public override void OnInspectorGUI() {
			this.DrawDefaultInspector();

			if (this.target is EventSubCustomRewardStartupExample source) {

				GUI.enabled = EditorApplication.isPlayingOrWillChangePlaymode && !TwitchEventSubClient.EventSubStartingUp && !TwitchEventSubClient.EventSubConnectionActive;

				if (GUILayout.Button("Generate Custom Channel Reward")) {
					source.BuildUserSubscriptionsForUser();
				}

				GUI.enabled = true;

				if (!string.IsNullOrWhiteSpace(source.RewardId)) {
					if (string.IsNullOrEmpty(this.builtString)) {
						this.builtString = "Reward ID: " + source.RewardId;
					}
					EditorGUILayout.SelectableLabel(this.builtString);
				} else {
					this.builtString = null;
				}
			}
		}
	}
}
#endif