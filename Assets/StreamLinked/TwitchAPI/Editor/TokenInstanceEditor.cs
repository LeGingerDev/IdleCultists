#if UNITY_EDITOR
namespace ScoredProductions.StreamLinked.Editor {
	using System;

	using ScoredProductions.StreamLinked.API.AuthContainers;
	using ScoredProductions.StreamLinked.Utility;

	using UnityEditor;

	using UnityEngine;
	using UnityEngine.UIElements;

	// https://github.com/DefaultLP/UnityEditorStyles

	[CustomEditor(typeof(TokenInstance))]
	public class TokenInstanceEditor : Editor {

		private SerializedProperty authenticationTypeSP;
		private SerializedProperty requestScopesSP;
		private SerializedProperty createLocalHostServerSP;
		private SerializedProperty redirectURISP;
		private SerializedProperty autoRetrieveNewAuthSP;
		private SerializedProperty userProvidedWebResponseSP;
		private GUIContent userProvidedWebResponseGUIC;
		private SerializedProperty userProvidedJSCodeSP;
		private GUIContent userProvidedJSCodeGUIC;
		private SerializedProperty manualRetrievalSP;
		private SerializedProperty pingIntervalSP;
		private SerializedProperty pingRetriesSP;
		private SerializedProperty startNewOnRefreshFailSP;

		private readonly GUILayoutOption BoxHeight = GUILayout.Height(100);
		//private GUIStyle BoldLabel;

		private string assetPath;

		public override VisualElement CreateInspectorGUI() {
			this.authenticationTypeSP = this.serializedObject.FindProperty("authenticationType");
			this.requestScopesSP = this.serializedObject.FindProperty("requestScopes");
			this.createLocalHostServerSP = this.serializedObject.FindProperty("createLocalHostServer");
			this.redirectURISP = this.serializedObject.FindProperty("redirectURI");
			this.autoRetrieveNewAuthSP = this.serializedObject.FindProperty("autoRetrieveNewAuth");

			this.userProvidedWebResponseSP = this.serializedObject.FindProperty("userProvidedWebResponse");
			this.userProvidedWebResponseGUIC = new GUIContent("Web Response HTML", this.userProvidedWebResponseSP.tooltip);
			this.userProvidedJSCodeSP = this.serializedObject.FindProperty("userProvidedJSCode");
			this.userProvidedJSCodeGUIC = new GUIContent("Additional JavaScript", this.userProvidedJSCodeSP.tooltip);

			this.manualRetrievalSP = this.serializedObject.FindProperty("manualRetrieval");
			this.pingIntervalSP = this.serializedObject.FindProperty("pingInterval");
			this.pingRetriesSP = this.serializedObject.FindProperty("pingRetries");
			this.startNewOnRefreshFailSP = this.serializedObject.FindProperty("startNewOnRefreshFail");

			//this.BoldLabel = new GUIStyle("BoldLabel") { alignment = TextAnchor.MiddleCenter };

			return base.CreateInspectorGUI();
		}

		public override void OnInspectorGUI() {
			if (this.target is TokenInstance instance) {
				if (Event.current.type == EventType.Repaint) {
					this.assetPath = AssetDatabase.GetAssetPath(instance.GetInstanceID());
				}

				if (!string.IsNullOrWhiteSpace(this.assetPath) && !this.assetPath.EndsWith(instance.name + ".asset")) { // Name of the file was updated
					ReadOnlySpan<char> name = this.assetPath.AsSpan();
					int index;
					while ((index = name.IndexOf('/')) != -1) {
						name = name[(index + 1)..];
					}
					// m_Name is the serialized value for Object.name
					this.serializedObject.FindProperty("m_Name").stringValue = new string(name[..name.IndexOf('.')]);
				}

				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(new GUIContent("ID", "Readonly ID of the Token"), GUILayout.Width(EditorGUIUtility.labelWidth));
				EditorGUILayout.SelectableLabel(instance.TokenID, GUILayout.Height(EditorGUIUtility.singleLineHeight));

				EditorGUILayout.EndHorizontal();

				using (new EditorGUI.DisabledScope(true)) {
					EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)this.target), this.GetType(), false);

					EditorGUILayout.ObjectField("Object", instance, typeof(TokenInstance), false);
				}

				EditorGUILayout.PropertyField(this.authenticationTypeSP);
				if (InternalSettingsStore.TryGetSetting(SavedSettings.TwitchClientType, out int clientType, false)) {
					TwitchClientType t = (TwitchClientType)clientType;
					switch (instance.AuthenticationType) {
						//case AuthRequestType.ImplicitGrantFlow:
						//	break;
						case AuthRequestType.AuthorizationCodeGrantFlow:
							// TODO : Change when Flow has been rewritten to accept server and client style modes instead of just Hybrid
							if (t == TwitchClientType.Public) {
								EditorGUILayout.HelpBox("Selected Client Type is set to Public, this token requires a Twitch Secret to work which is not provided for Public Client Type applications.", MessageType.Error);
							}
							break;
						case AuthRequestType.DeviceCodeGrantFlow:
							if (t == TwitchClientType.Confidential) {
								EditorGUILayout.HelpBox("Selected Client Type is set to Confidential, to refresh instead of aquiring a new token a secret must be provided.", MessageType.Warning);
							}
							break;
						case AuthRequestType.ClientCredentialsGrantFlow:
							if (t == TwitchClientType.Public) {
								EditorGUILayout.HelpBox("Selected Client Type is set to Public, this token requires a Twitch Secret to work which is not provided for Public Client Type applications.", MessageType.Error);
							}
							break;
					}
				}

				EditorGUILayout.PropertyField(this.autoRetrieveNewAuthSP);

				if (instance.AuthenticationType != AuthRequestType.ClientCredentialsGrantFlow) {
					EditorGUILayout.PropertyField(this.requestScopesSP);
				}

				if (instance.AuthenticationType is AuthRequestType.ImplicitGrantFlow or AuthRequestType.AuthorizationCodeGrantFlow) {
					//EditorGUILayout.LabelField("Flow direction", this.BoldLabel);

					EditorGUILayout.PropertyField(this.createLocalHostServerSP);
					EditorGUILayout.PropertyField(this.redirectURISP);

					//EditorGUILayout.LabelField("Webserver Response and JavaScript", this.BoldLabel);

					EditorGUILayout.LabelField(this.userProvidedWebResponseGUIC);
					this.userProvidedWebResponseSP.stringValue = EditorGUILayout.TextArea(this.userProvidedWebResponseSP.stringValue, this.BoxHeight);

					EditorGUILayout.LabelField(this.userProvidedJSCodeGUIC);
					this.userProvidedJSCodeSP.stringValue = EditorGUILayout.TextArea(this.userProvidedJSCodeSP.stringValue, this.BoxHeight);
				}

				if (instance.AuthenticationType == AuthRequestType.DeviceCodeGrantFlow) {
					//EditorGUILayout.LabelField("Device Code Detection Settings", this.BoldLabel);
					EditorGUILayout.PropertyField(this.manualRetrievalSP);
					if (instance.ManualRetrieval) {
						EditorGUILayout.HelpBox("Twitch API Client will send up the request to Twitch. The client will NOT automatically ping Twitch to Authenticate and needs to be completed by submitting via TwitchAPIClient instance method ReceiveDeviceCodeGrantFlowManually", MessageType.Warning);
					} 
					else {
						EditorGUILayout.PropertyField(this.pingIntervalSP);
						EditorGUILayout.PropertyField(this.pingRetriesSP);
						EditorGUILayout.PropertyField(this.startNewOnRefreshFailSP);
					}
				}

				if (this.serializedObject.ApplyModifiedProperties()) {
					this.serializedObject.UpdateIfRequiredOrScript();
				}
			}
		}
	}
}
#endif