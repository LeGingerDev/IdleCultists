using System;
using System.Threading;

using UnityEngine.Events;

namespace ScoredProductions.StreamLinked.Utility {

	[Serializable]
	public class ExtendedUnityEvent : UnityEvent {
		private int listenerCount = 0;

		public new void AddListener(UnityAction call) {
			base.AddListener(call);
			Interlocked.Increment(ref this.listenerCount);
		}

		public new void RemoveListener(UnityAction call) {
			base.RemoveListener(call);
			Interlocked.Decrement(ref this.listenerCount);
		}

		public int GetListenersCount() {
			return this.listenerCount + this.GetPersistentEventCount();
		}

		public static bool IsNullOrEmpty(ExtendedUnityEvent value) {
			return value == null || value.GetListenersCount() == 0;
		}

		public static bool IsNullOrEmpty<T>(ExtendedUnityEvent<T> value) {
			return value == null || value.GetListenersCount() == 0;
		}

		public static bool IsNullOrEmpty<T1, T2>(ExtendedUnityEvent<T1, T2> value) {
			return value == null || value.GetListenersCount() == 0;
		}

		public static bool IsNullOrEmpty<T1, T2, T3>(ExtendedUnityEvent<T1, T2, T3> value) {
			return value == null || value.GetListenersCount() == 0;
		}

		public static bool IsNullOrEmpty<T1, T2, T3, T4>(ExtendedUnityEvent<T1, T2, T3, T4> value) {
			return value == null || value.GetListenersCount() == 0;
		}
	}
	
	[Serializable]
	public class ExtendedUnityEvent<T> : UnityEvent<T> {
		private int listenerCount = 0;

		public new void AddListener(UnityAction<T> call) {
			base.AddListener(call);
			Interlocked.Increment(ref this.listenerCount);
		}

		public new void RemoveListener(UnityAction<T> call) {
			base.RemoveListener(call);
			Interlocked.Decrement(ref this.listenerCount);
		}

		public int GetListenersCount() {
			return this.listenerCount + this.GetPersistentEventCount();
		}
	}

	[Serializable]
	public class ExtendedUnityEvent<T1, T2> : UnityEvent<T1, T2> {
		private int listenerCount = 0;

		public new void AddListener(UnityAction<T1, T2> call) {
			base.AddListener(call);
			Interlocked.Increment(ref this.listenerCount);
		}

		public new void RemoveListener(UnityAction<T1, T2> call) {
			base.RemoveListener(call);
			Interlocked.Decrement(ref this.listenerCount);
		}

		public int GetListenersCount() {
			return this.listenerCount + this.GetPersistentEventCount();
		}
	}

	[Serializable]
	public class ExtendedUnityEvent<T1, T2, T3> : UnityEvent<T1, T2, T3> {
		private int listenerCount = 0;

		public new void AddListener(UnityAction<T1, T2, T3> call) {
			base.AddListener(call);
			Interlocked.Increment(ref this.listenerCount);
		}

		public new void RemoveListener(UnityAction<T1, T2, T3> call) {
			base.RemoveListener(call);
			Interlocked.Decrement(ref this.listenerCount);
		}

		public int GetListenersCount() {
			return this.listenerCount + this.GetPersistentEventCount();
		}
	}

	[Serializable]
	public class ExtendedUnityEvent<T1, T2, T3, T4> : UnityEvent<T1, T2, T3, T4> {
		private int listenerCount = 0;

		public new void AddListener(UnityAction<T1, T2, T3, T4> call) {
			base.AddListener(call);
			Interlocked.Increment(ref this.listenerCount);
		}

		public new void RemoveListener(UnityAction<T1, T2, T3, T4> call) {
			base.RemoveListener(call);
			Interlocked.Decrement(ref this.listenerCount);
		}

		public int GetListenersCount() {
			return this.listenerCount + this.GetPersistentEventCount();
		}
	}

	public static class ExtendedUnityEventExtensions {
		public static bool IsNullOrEmpty(this ExtendedUnityEvent @event) => ExtendedUnityEvent.IsNullOrEmpty(@event);
		public static bool IsNullOrEmpty<T1>(this ExtendedUnityEvent<T1> @event) => ExtendedUnityEvent.IsNullOrEmpty(@event);
		public static bool IsNullOrEmpty<T1, T2>(this ExtendedUnityEvent<T1, T2> @event) => ExtendedUnityEvent.IsNullOrEmpty(@event);
		public static bool IsNullOrEmpty<T1, T2, T3>(this ExtendedUnityEvent<T1, T2, T3> @event) => ExtendedUnityEvent.IsNullOrEmpty(@event);
		public static bool IsNullOrEmpty<T1, T2, T3, T4>(this ExtendedUnityEvent<T1, T2, T3, T4> @event) => ExtendedUnityEvent.IsNullOrEmpty(@event);
	}
}
