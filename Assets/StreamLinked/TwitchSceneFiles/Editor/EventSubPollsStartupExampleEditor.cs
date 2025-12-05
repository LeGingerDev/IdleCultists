#if UNITY_EDITOR
namespace ScoredProductions.StreamLinked.Editor {
	using ScoredProductions.StreamLinked.EventSub;
	using ScoredProductions.StreamLinked.TwitchSceneFiles;

	using UnityEditor;

	using UnityEngine;

	[CustomEditor(typeof(EventSubPollsStartupExample))]
	public class EventSubPollsStartupExampleEditor : Editor {

		public override void OnInspectorGUI() {
			this.DrawDefaultInspector();

			if (this.target is EventSubPollsStartupExample source) {

				GUI.enabled = EditorApplication.isPlayingOrWillChangePlaymode && !TwitchEventSubClient.EventSubStartingUp && !TwitchEventSubClient.EventSubConnectionActive;

				if (GUILayout.Button("Subscribe to Channel Polling events")) {
					source.BuildUserSubscriptionsForUser().ConfigureAwait(false);
				}
			}
		}
	}
}
#endif