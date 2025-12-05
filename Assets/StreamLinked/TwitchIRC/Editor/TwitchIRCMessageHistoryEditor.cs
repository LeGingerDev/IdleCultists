#if UNITY_EDITOR
namespace ScoredProductions.StreamLinked.Editor {
	using System;
	using System.Collections.Generic;
	using System.Timers;

	using ScoredProductions.StreamLinked.IRC;
	using ScoredProductions.StreamLinked.IRC.Message.Interface;

	using UnityEditor;

	using UnityEngine;

	[CustomEditor(typeof(TwitchIRCMessageHistory))]
	public class TwitchIRCMessageHistoryEditor : Editor {

		private const int refreshTimer = 1000;
		private const int numPerPage = 10;

		public int pageNum = 1;
		public int possiblePages;

		private bool allowRefresh = true;
		private static List<string> cache = new List<string>();

		private Vector2 scrollPos;
		private Lazy<Timer> interval = new Lazy<Timer>(() => {
			Timer t = new Timer(refreshTimer) { AutoReset = true };
			t.Elapsed += OnRefresh;
			return t;
		});

		public override void OnInspectorGUI() {
			if (!this.interval.IsValueCreated) {
				this.interval.Value.Start();
			}

			this.DrawDefaultInspector();

			this.interval.Value.Enabled = this.allowRefresh;

			if (this.target is TwitchIRCMessageHistory value) {
				EditorGUILayout.Space(10);

				List<string> list = new List<string>(cache);
				this.possiblePages = (list.Count / numPerPage) + 1;
				if (this.pageNum > this.possiblePages) {
					this.pageNum = this.possiblePages;
				}

				GUILayout.Label($"Messages loaded: {list.Count}");

				EditorGUILayout.Space(5);
				GUILayout.Label("Parsed Messages:");
				if (list.Count > 0) {
					GUILayout.BeginHorizontal();
					if (GUILayout.Button("<", GUILayout.Width(25))) {
						this.pageNum--;
						if (this.pageNum < 1) {
							this.pageNum = 1;
						}
					}
					string pageNumChanged = GUILayout.TextField(this.pageNum.ToString(), GUILayout.Width(25));
					if (int.TryParse(pageNumChanged, out int v)) {
						if (v > 0 && v <= this.possiblePages) {
							this.pageNum = v;
						}
					}
					if (GUILayout.Button(">", GUILayout.Width(25))) {
						this.pageNum++;
						if (this.pageNum > this.possiblePages) {
							this.pageNum = this.possiblePages;
						}
					}
					GUILayout.EndHorizontal();
				}

				this.scrollPos = GUILayout.BeginScrollView(this.scrollPos);
				int lowerbound = (this.pageNum - 1) * numPerPage;
				int upperbound = this.pageNum * numPerPage;
				for (int x = lowerbound; x < Math.Min(upperbound,list.Count); x++) {
					EditorGUILayout.HelpBox(list[x], MessageType.None);
				}
				GUILayout.EndScrollView();
			}
		}

		private static void OnRefresh(object sender, ElapsedEventArgs e) {
			cache.Clear();
			List<ITwitchIRCMessage> store = TwitchIRCMessageHistory.StoredMessages;

			for (int x = 0; x < store.Count; x++) {
				cache.Add(store[x].ToString());
			}
		}
	}
}
#endif