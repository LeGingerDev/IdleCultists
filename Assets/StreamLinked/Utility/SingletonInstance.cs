using System;

using UnityEngine;

namespace ScoredProductions.StreamLinked.Utility {

	/// <summary>
	/// Singleton base class.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[DefaultExecutionOrder(-0x1)] //Default execution of all singletons is before gameobject default
	public abstract class SingletonInstance<T> : MonoBehaviour where T : SingletonInstance<T> {
		public static readonly string Name = typeof(T).Name.ToReadable();

		protected static T _instance;

		private static readonly object Locker = new object();

		protected static bool preventNewInstances;

		/// <summary>
		/// If the Singleton is running in the scene. Good to prevent non-playtime functionality running.
		/// </summary>
		public static bool InstanceIsAlive => _instance != null;

		public abstract bool PersistBetweenScenes { get; }

		/// <summary>
		/// Gets the Singleton if it exists in the scene.
		/// </summary>
		public static bool GetInstance(out T instance) {
			instance = _instance;
			return instance != null;
		}

		/// <summary>
		/// Gets the current instance, if a singleton doesnt exist, it will create one.
		/// </summary>
		public static bool CreateOrGetInstance(out T instance) {
			instance = null;
			if (preventNewInstances) {
				return false;
			}
			try {
				if (_instance == null) {
					_instance = (new GameObject(Name)).AddComponent<T>();
				}
				instance = _instance;
				instance.EstablishSingleton();
				return true;
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);

				return false;
			}
		}
		
		/// <summary>
		/// Gets the current instance, if a singleton doesnt exist, it will create one with the provided prefab.
		/// </summary>
		public static bool CreateOrGetInstance(GameObject prefab, out T instance) {
			instance = null;
			if (preventNewInstances) {
				return false;
			}

			if (_instance != null) {
				instance = _instance;
				return true;
			}

			if (prefab == null) {
				bool state = CreateOrGetInstance(out T i);
				instance = i;
				return state;
			}

			try {
				if (prefab.TryGetComponent(out T _)) { // Check script is included
					instance = _instance = Instantiate(prefab).GetComponent<T>(); // Get script from created object (will be different)
					return true;
				} else {
					DebugManager.LogMessage("Provided prefab object does not contain the script required by this SingletonInstance " + typeof(T).Name, DebugManager.ErrorLevel.Error);
					return false;
				}
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);

				return false;
			}
		}

		/// <summary>
		/// <b>Must be called in Awake</b>, Ensures singleton is the only instance.
		/// </summary>
		protected virtual bool EstablishSingleton(bool updateName = false) {
			lock (Locker) {
				if (_instance != null && _instance != (T)this) {
					Destroy(this.gameObject);
					return false;
				}
				else {
					_instance = (T)this;
					if (updateName) {
						_instance.name = Name;
					}
					return true;
				}
			}
		}

		/// <summary>
		/// Delete singleton from the scene
		/// </summary>
		public virtual bool DeleteInstance() {
			lock (Locker) {
				if (_instance != null && _instance != (T)this) {
					Destroy(this.gameObject);
					return true;
				}
				else {
					return false;
				}
			}
		}

		protected virtual void Awake() {
			this.EstablishSingleton(true);
		}

		protected virtual void Start() {
			if (Application.isPlaying && this.PersistBetweenScenes) {
				DontDestroyOnLoad(this.gameObject);
			}
		}

		protected virtual void OnApplicationQuit() {
			preventNewInstances = true;
		}

	}
}