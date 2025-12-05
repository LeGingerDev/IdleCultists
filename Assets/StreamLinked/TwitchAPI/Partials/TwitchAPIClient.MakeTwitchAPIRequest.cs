using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ScoredProductions.StreamLinked.API.Auth;
using ScoredProductions.StreamLinked.API.AuthContainers;
using ScoredProductions.StreamLinked.API.Scopes;
using ScoredProductions.StreamLinked.LightJson;
using ScoredProductions.StreamLinked.LightJson.Serialization;
using ScoredProductions.StreamLinked.Utility;

using UnityEngine;
using UnityEngine.Networking;

namespace ScoredProductions.StreamLinked.API {

	public partial class TwitchAPIClient // .MakeTwitchAPIRequest
	{
		[Serializable]
		public struct MakeTwitchAPIRequestSettings {
			[SerializeField]
			public APIScopeWarning ScopeWarning;
			[SerializeField, Tooltip("Global value for when a request is hit with 401, does the request attempt to retry and queue a auth request for the token. False blocks all retrys.")]
			public bool RetryOnUnauthorised;
			[SerializeField, Tooltip("Doesnt consider the refresh token if the type supports it and proceeds to get a fresh token of the type.")]
			public bool GetFreshTokens;
		}

		public const string ENDPOINT = "Endpoint";
		public const string RESPONSE = "Response";
		public const string STATUS_CODE = "Status_Code";
		public const string HAS_ERRORED = "Has_Errored";
		public const string ERROR_TEXT = "Error_Text";

		private static readonly Queue<StringBuilder> sbQueue = new Queue<StringBuilder>(1);

		private static StringBuilder RequestStringBuilder(string startingValue) {
			if (sbQueue.TryDequeue(out StringBuilder sb)) {
				sb.Clear();
				sb.Append(startingValue);
				return sb;
			}

			return new StringBuilder(startingValue);
		}

		private static StringBuilder RequestStringBuilder() {
			if (sbQueue.TryDequeue(out StringBuilder sb)) {
				sb.Clear();
				return sb;
			}
			return new StringBuilder();
		}

		private static void ReturnStringBuilder(StringBuilder sb) {
			sb.Clear();
			if (!sbQueue.Contains(sb)) {
				sbQueue.Enqueue(sb);
			}
		}

		private (string, string) AquireClientIDParams() {
			return (TwitchWords.CLIENTID, this.TwitchClientID);
		}

		/// <summary>
		/// Synchronous call to Twitch API returning the data. Best for testing calls or making calls off the main thread. Will hang the frame until completion. Not recommended for deployment use.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Timeout">Time in milliseconds before the request is cancelled</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="ScopeSettings">Actions to take when the required scopes are missing.</param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public static TwitchAPIDataContainer<T> MakeTwitchAPIRequest<T>(
												int Timeout,
												TokenInstance Credentials = null,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
												bool blockRetry = false)
												where T : ITwitchAPIDataObject, new() {
			if (Timeout < 1) {
				Timeout = 1;
			}
			T requestObject = new T();
			TwitchAPIDataContainer<T> returnedObject = default;

			if (Task.Run(
					async () => {
						returnedObject = await MakeTwitchAPIRequestAsync<T>(
							Credentials,
							HeaderValues,
							QueryParameters,
							Body,
							ContentType,
							ScopeSettings,
							new CancellationTokenSource(Timeout).Token,
							blockRetry);
					}
				).Wait(Timeout)) {
				return returnedObject;
			}
			else {
				throw new TimeoutException($"MakeTwitchAPIRequest<{typeof(T).Name}> request timed out after {Timeout} milliseconds");
			}
		}

		/// <summary>		
		/// Synchronous call to Twitch API returning the data. Best for testing calls or making calls off the main thread. Will hang the frame until completion. Not recommended for deployment use.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Timeout">Time in milliseconds before the request is cancelled</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public static JsonValue MakeTwitchAPIRequestJson(
											string Endpoint,
											TwitchAPIRequestMethod Method,
											int Timeout,
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											bool blockRetry = false) {
			if (Timeout < 1) {
				Timeout = 1;
			}
			JsonValue returnedObject = JsonValue.Null;

			if (Task.Run(
					async () => {
						returnedObject = await MakeTwitchAPIRequestJsonAsync(
							Endpoint,
							Method,
							Credentials,
							HeaderValues,
							QueryParameters,
							Body,
							ContentType,
							new CancellationTokenSource(Timeout).Token,
							blockRetry);
					}
				).Wait(Timeout)) {
				return returnedObject;
			}
			else {
				throw new TimeoutException($"MakeTwitchAPIRequest request timed out after {Timeout} milliseconds");
			}
		}

		/// <summary>		
		/// Synchronous call to Twitch API returning the data. Best for testing calls or making calls off the main thread. Will hang the frame until completion. Not recommended for deployment use.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Timeout">Time in milliseconds before the request is cancelled</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public static string MakeTwitchAPIRequestRaw(
											string Endpoint,
											TwitchAPIRequestMethod Method,
											int Timeout,
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											bool blockRetry = false) {
			if (Timeout < 1) {
				Timeout = 1;
			}
			string returnedObject = string.Empty;

			if (Task.Run(
					async () => {
						returnedObject = await MakeTwitchAPIRequestRawAsync(
							Endpoint,
							Method,
							Credentials,
							HeaderValues,
							QueryParameters,
							Body,
							ContentType,
							new CancellationTokenSource(Timeout).Token,
							blockRetry);
					}
				).Wait(Timeout)) {
				return returnedObject;
			}
			else {
				throw new TimeoutException($"MakeTwitchAPIRequest request timed out after {Timeout} milliseconds");
			}
		}

		/// <summary>
		/// Coroutine to make requests to Twitchs API. Access the data via SuccessCallback.
		/// </summary>
		/// <typeparam name="T">ITwitchAPIDataObject datatype inside a TwitchAPIDataContainer</typeparam>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="ScopeSettings">Actions to take when the required scopes are missing.</param>
		/// <exception cref="UnityException"></exception>
		public Coroutine MakeTwitchAPIRequest<T>(
												Action<TwitchAPIDataContainer<T>> SuccessCallback,
												TokenInstance Credentials = null,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
												bool blockRetry = false,
												CancellationToken cancelToken = default)
												where T : ITwitchAPIDataObject, new() {
			return this.StartCoroutine(MakeTwitchAPIRequest<T>(
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				ContentType,
				ScopeSettings,
				blockRetry,
				cancelToken,
				SuccessCallback));
		}

		/// <summary>
		/// Coroutine to make requests to Twitchs API. Access the data via returned SuccessCallback.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="IncludeAuthHeaders">Includes Client ID and Auth token for non-standard endpoints</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		public Coroutine MakeTwitchAPIRequestJson(
												string Endpoint,
												TwitchAPIRequestMethod Method,
												Action<JsonValue> SuccessCallback,
												TokenInstance Credentials = null,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												bool blockRetry = false,
												CancellationToken cancelToken = default) {
			return this.StartCoroutine(MakeTwitchAPIRequestJson(
				Endpoint,
				Method,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				ContentType,
				blockRetry,
				cancelToken,
				SuccessCallback));
		}

		/// <summary>
		/// Coroutine to make requests to Twitchs API. Access the data via returned SuccessCallback.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="IncludeAuthHeaders">Includes Client ID and Auth token for non-standard endpoints</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		public Coroutine MakeTwitchAPIRequestRaw(
												string Endpoint,
												TwitchAPIRequestMethod Method,
												Action<string> SuccessCallback,
												TokenInstance Credentials = null,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												bool blockRetry = false,
												CancellationToken cancelToken = default) {
			return this.StartCoroutine(MakeTwitchAPIRequestRaw(
				Endpoint,
				Method,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				ContentType,
				blockRetry,
				cancelToken,
				SuccessCallback));
		}

		/// <summary>
		/// Enumerator (Coroutine) to make requests to Twitchs API. Access the data via returned IEnumerator or SuccessCallback.
		/// </summary>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="ScopeSettings">Actions to take when the required scopes are missing.</param>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		/// <typeparam name="T">ITwitchAPIDataObject datatype inside a TwitchAPIDataContainer</typeparam>
		public static IEnumerator MakeTwitchAPIRequest<T>(
												TokenInstance Credentials = null,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
												bool blockRetry = false,
												CancellationToken cancelToken = default,
												Action<TwitchAPIDataContainer<T>> SuccessCallback = null)
												where T : ITwitchAPIDataObject, new() {
			T requestType = new T();

			IEnumerator internalRun = InternalMakeTwitchAPIRequest(
				requestType.Endpoint,
				requestType.HTTPMethod,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
					? AquireContentType<T>(HeaderValues)
					: ContentType,
				requestType,
				ScopeSettings,
				blockRetry: blockRetry);
			yield return internalRun;
			TwitchAPIDataContainer<T> objectResult;
			string result = string.Empty;
			try {
				result = (string)internalRun.Current;
				JsonValue parsedResult = JsonReader.Parse(result);
				objectResult = new TwitchAPIDataContainer<T>(parsedResult);
				SuccessCallback?.Invoke(objectResult);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
				objectResult = new TwitchAPIDataContainer<T>() {
					RawResponse = result,
					HasErrored = true,
					ErrorText = ex.Message,
				};
			}
			yield return objectResult;
		}

		/// <summary>
		/// Enumerator (Coroutine) to make requests to Twitchs API. Access the data via returned IEnumerator or SuccessCallback.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		public static IEnumerator MakeTwitchAPIRequestJson<T>(
												TokenInstance Credentials = null,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
												bool blockRetry = false,
												CancellationToken cancelToken = default,
												Action<JsonValue> SuccessCallback = null)
												where T : ITwitchAPIDataObject, new() {
			T requestType = new T();

			IEnumerator internalRun = InternalMakeTwitchAPIRequest(
				requestType.Endpoint,
				requestType.HTTPMethod,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
					? AquireContentType<T>(HeaderValues)
					: ContentType,
				requestType,
				ScopeSettings,
				blockRetry: blockRetry);
			yield return internalRun;
			JsonValue parsedResult = JsonValue.Null;
			try {
				string result = (string)internalRun.Current;
				parsedResult = JsonReader.Parse(result);
				SuccessCallback?.Invoke(parsedResult);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
			}
			yield return parsedResult;
		}

		/// <summary>
		/// Enumerator (Coroutine) to make requests to Twitchs API. Access the data via returned IEnumerator or SuccessCallback.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		public static IEnumerator MakeTwitchAPIRequestJson(
												string Endpoint,
												TwitchAPIRequestMethod Method,
												TokenInstance Credentials = null,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												bool blockRetry = false,
												CancellationToken cancelToken = default,
												Action<JsonValue> SuccessCallback = null) {
			IEnumerator internalRun = InternalMakeTwitchAPIRequest(
				Endpoint,
				Method,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
						? AquireContentType(HeaderValues)
						: ContentType,
				blockRetry: blockRetry);
			yield return internalRun;
			JsonValue parsedResult = JsonValue.Null;
			try {
				string result = (string)internalRun.Current;
				parsedResult = JsonReader.Parse(result);
				SuccessCallback?.Invoke(parsedResult);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
			}
			yield return parsedResult;
		}

		/// <summary>
		/// Enumerator (Coroutine) to make requests to Twitchs API. Access the data via returned IEnumerator or SuccessCallback.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		public static IEnumerator MakeTwitchAPIRequestRaw<T>(
												TokenInstance Credentials,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
												bool blockRetry = false,
												CancellationToken cancelToken = default,
												Action<string> SuccessCallback = null)
												where T : ITwitchAPIDataObject, new() {
			T requestType = new T();

			IEnumerator internalRun = InternalMakeTwitchAPIRequest(
				requestType.Endpoint,
				requestType.HTTPMethod,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
						? AquireContentType<T>(HeaderValues)
						: ContentType,
				requestType,
				ScopeSettings,
				blockRetry: blockRetry);
			yield return internalRun;
			string result = string.Empty;
			try {
				result = (string)internalRun.Current;
				SuccessCallback?.Invoke(result);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
			}
			yield return result;
		}

		/// <summary>
		/// Enumerator (Coroutine) to make requests to Twitchs API. Access the data via returned IEnumerator or SuccessCallback.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="SuccessCallback">Callback to occur on request success</param>
		public static IEnumerator MakeTwitchAPIRequestRaw(
												string Endpoint,
												TwitchAPIRequestMethod Method,
												TokenInstance Credentials,
												(string, string)[] HeaderValues = null,
												(string, string)[] QueryParameters = null,
												string Body = null,
												string ContentType = null,
												bool blockRetry = false,
												CancellationToken cancelToken = default,
												Action<string> SuccessCallback = null) {
			IEnumerator internalRun = InternalMakeTwitchAPIRequest(
				Endpoint,
				Method,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
						? AquireContentType(HeaderValues)
						: ContentType,
				blockRetry: blockRetry);
			yield return internalRun;
			string result = string.Empty;
			try {
				result = (string)internalRun.Current;
				SuccessCallback?.Invoke(result);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
			}
			yield return result;
		}

		/// <summary>
		/// Enumerator (Coroutine) to make requests to Twitchs API. Access the data via returned IEnumerator or SuccessCallback.
		/// </summary>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns><code>JsonValue</code> Raw response from server</returns>
		public static IEnumerator MakeTwitchAPIRequestRaw(
											Type type,
											TokenInstance Credentials,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											bool blockRetry = false,
											CancellationToken cancelToken = default,
											Action<string> SuccessCallback = null) {
			if (!typeof(ITwitchAPIDataObject).IsAssignableFrom(type)) {
				throw new ArgumentException("Provided class type is not a callable type to Twitch API");
			}
			ITwitchAPIDataObject requestType = (ITwitchAPIDataObject)Activator.CreateInstance(type);
			IEnumerator internalRun = InternalMakeTwitchAPIRequest(
				requestType.Endpoint,
				requestType.HTTPMethod,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
						? AquireContentType(HeaderValues)
						: ContentType,
				blockRetry: blockRetry);
			yield return internalRun;
			string result = string.Empty;
			try {
				result = (string)internalRun.Current;
				SuccessCallback?.Invoke(result);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
			}
			yield return result;
		}

		/// <summary>
		/// Main Task to make requests to Twitchs API.
		/// </summary>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <param name="ScopeSettings">Actions to take when the required scopes are missing.</param>
		/// <typeparam name="T">ITwitchAPIDataObject datatype inside a TwitchAPIDataContainer</typeparam>
		/// <returns><code>ITwitchAPIData</code> (If returned from endpoint)</returns>
		public static async Task<TwitchAPIDataContainer<T>> MakeTwitchAPIRequestAsync<T>(
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
											CancellationToken cancelToken = default,
											bool blockRetry = false)
											where T : ITwitchAPIDataObject, new() {
			cancelToken.ThrowIfCancellationRequested();
			T requestType = new T();

			TwitchAPIDataContainer<T> returned;
			string result = string.Empty;
			try {
				result = await InternalMakeTwitchAPIRequestAsync(
					requestType.Endpoint,
					requestType.HTTPMethod,
					Credentials,
					HeaderValues,
					QueryParameters,
					Body,
					string.IsNullOrWhiteSpace(ContentType)
						? AquireContentType<T>(HeaderValues)
						: ContentType,
					cancelToken,
					requestType,
					ScopeSettings,
					blockRetry);
				cancelToken.ThrowIfCancellationRequested();
				JsonValue parsedResult = JsonReader.Parse(result);
				returned = new TwitchAPIDataContainer<T>(parsedResult);
				return returned;
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
				returned = new TwitchAPIDataContainer<T>() {
					RawResponse = result,
					HasErrored = true,
					ErrorText = ex.Message,
				};
			}
			return returned;

		}

		/// <summary>
		/// Main Task to make requests to Twitchs API.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns><code>JsonValue</code> Raw response from server</returns>
		public static async Task<JsonValue> MakeTwitchAPIRequestJsonAsync<T>(
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
											CancellationToken cancelToken = default,
											bool blockRetry = false)
											where T : ITwitchAPIDataObject, new() {
			cancelToken.ThrowIfCancellationRequested();
			T requestType = new T();

			JsonValue returned = JsonValue.Null;
			try {
				string result = await InternalMakeTwitchAPIRequestAsync(
					requestType.Endpoint,
					requestType.HTTPMethod,
					Credentials,
					HeaderValues,
					QueryParameters,
					Body,
					string.IsNullOrWhiteSpace(ContentType)
						? AquireContentType<T>(HeaderValues)
						: ContentType,
					cancelToken,
					requestType,
					ScopeSettings,
					blockRetry);
				cancelToken.ThrowIfCancellationRequested();
				returned = JsonReader.Parse(result);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
			}
			return returned;
		}

		/// <summary>
		/// Main Task to make requests to Twitchs API.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns><code>JsonValue</code> Raw response from server</returns>
		public static async Task<JsonValue> MakeTwitchAPIRequestJsonAsync(
											string Endpoint,
											TwitchAPIRequestMethod Method,
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											CancellationToken cancelToken = default,
											bool blockRetry = false) {
			cancelToken.ThrowIfCancellationRequested();
			JsonValue returned = JsonValue.Null;
			try {
				string result = await InternalMakeTwitchAPIRequestAsync(Endpoint,
					Method,
					Credentials,
					HeaderValues,
					QueryParameters,
					Body,
					string.IsNullOrWhiteSpace(ContentType)
						? AquireContentType(HeaderValues)
						: ContentType,
					cancelToken,
					blockRetry: blockRetry);
				cancelToken.ThrowIfCancellationRequested();
				returned = JsonReader.Parse(result);
			} catch (Exception ex) {
				DebugManager.LogMessage(ex);
			}
			return returned;
		}

		/// <summary>
		/// Main Task to make requests to Twitchs API. Uses the OAuth token supplied in the API client. Returns the compartmented data sent to standard MakeTwitchAPIRequest methods. Keys available as consts from TwitchAPIClient.
		/// </summary>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns><code>JsonValue</code> Raw response from server</returns>
		public static Task<string> MakeTwitchAPIRequestRawAsync<T>(
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
											CancellationToken cancelToken = default,
											bool blockRetry = false)
											where T : ITwitchAPIDataObject, new() {
			T requestType = new T();
			return InternalMakeTwitchAPIRequestAsync(
				requestType.Endpoint,
				requestType.HTTPMethod,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
					? AquireContentType<T>(HeaderValues)
					: ContentType,
				cancelToken,
				requestType,
				ScopeSettings,
				blockRetry);
		}

		/// <summary>
		/// Main Task to make requests to Twitchs API. Uses the OAuth token supplied in the API client. Returns the compartmented data sent to standard MakeTwitchAPIRequest methods. Keys available as consts from TwitchAPIClient.
		/// </summary>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns><code>JsonValue</code> Raw response from server</returns>
		public static Task<string> MakeTwitchAPIRequestRawAsync(
											string Endpoint,
											TwitchAPIRequestMethod Method,
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											CancellationToken cancelToken = default,
											bool blockRetry = false) {
			return InternalMakeTwitchAPIRequestAsync(
				Endpoint,
				Method,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
					? AquireContentType(HeaderValues)
					: ContentType,
				cancelToken,
				blockRetry: blockRetry);
		}

		/// <summary>
		/// Main Task to make requests to Twitchs API. Uses the OAuth token supplied in the API client. Returns the compartmented data sent to standard MakeTwitchAPIRequest methods. Keys available as consts from TwitchAPIClient.
		/// </summary>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns><code>JsonValue</code> Raw response from server</returns>
		public static Task<string> MakeTwitchAPIRequestRawAsync(
											Type type,
											TokenInstance Credentials = null,
											(string, string)[] HeaderValues = null,
											(string, string)[] QueryParameters = null,
											string Body = null,
											string ContentType = null,
											APIScopeWarning ScopeSettings = (APIScopeWarning)(-1),
											CancellationToken cancelToken = default,
											bool blockRetry = false) {
			if (!typeof(ITwitchAPIDataObject).IsAssignableFrom(type)) {
				throw new ArgumentException("Provided class type is not a callable type to Twitch API");
			}
			ITwitchAPIDataObject requestType = (ITwitchAPIDataObject)Activator.CreateInstance(type);
			return InternalMakeTwitchAPIRequestAsync(
				requestType.Endpoint,
				requestType.HTTPMethod,
				Credentials,
				HeaderValues,
				QueryParameters,
				Body,
				string.IsNullOrWhiteSpace(ContentType)
					? AquireContentType(HeaderValues)
					: ContentType,
				cancelToken,
				requestType,
				ScopeSettings,
				blockRetry);
		}

		/// <summary>
		/// Main Task to make requests to Twitchs API. Returned in non pretty Json.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch</param>
		/// <returns><code>string</code> Raw response from server</returns>
		private static IEnumerator InternalMakeTwitchAPIRequest(
											string Endpoint,
											TwitchAPIRequestMethod Method,
											TokenInstance Credentials,
											(string, string)[] HeaderValues,
											(string, string)[] QueryParameters,
											string Body,
											string ContentType,
											IScope reference = null,
											APIScopeWarning ProvidedScopeSettings = (APIScopeWarning)(-1),
											CancellationToken cancelToken = default,
											bool blockRetry = false) {
			if (cancelToken.IsCancellationRequested) {
				yield break;
			}

			if (CreateOrGetInstance(out TwitchAPIClient client)) {
				if (Credentials == null) {
					if (client.DefaultAPIToken == null) {
						DebugManager.LogMessage($"TwitchAPIClient no token provided by method or available inside API client, Task Aborted: {Endpoint} {Method}", DebugManager.ErrorLevel.Error);
						yield break;
					}
					Credentials = client.DefaultAPIToken;
				}
			}
			else {
				DebugManager.LogMessage($"TwitchAPIClient does not currently exist, Task Aborted: {Endpoint} {Method}", DebugManager.ErrorLevel.Error);
				yield break;
			}

			if (cancelToken.IsCancellationRequested) {
				yield break;
			}

			APIScopeWarning ScopeSettings;
			if (ProvidedScopeSettings < 0) {
				ScopeSettings = client.DefaultRequestSettings.ScopeWarning;
			}
			else {
				ScopeSettings = ProvidedScopeSettings;
			}

			bool isAuthRequest = IAuth.EndpointIsAuthRequest(Endpoint);

			bool retryPerformed = false;

			StringBuilder builder = RequestStringBuilder();

			retry:
			builder.Clear();
			builder.Append(Endpoint);

			if (client.LogDebugLevel != DebugManager.DebugLevel.None) {
				DebugManager.LogMessage($"Attempting Call to Twitch API, Endpoint: {Endpoint}, Method: {Method}, HeaderValues: {HeaderValues.ToJSONString()}, QueryParameters: {QueryParameters.ToJSONString()}, Body: {Body}".RichTextColour(Color.cyan));
			}

			if (Credentials == null) {
				if (client.DefaultAPIToken == null) {
					DebugManager.LogMessage($"TwitchAPIClient no token provided by method or available inside API client, Task Aborted: {Endpoint} {Method}", DebugManager.ErrorLevel.Error);
					yield break;
				}
				Credentials = client.DefaultAPIToken;
			}

			if (reference != null && !retryPerformed) {
				Credentials.PerformScopeCheck(in ScopeSettings, reference);
			}

			if (isAuthRequest) {
				while (client.currentTokenBeingRefreshed != Credentials) {
					yield return TwitchStatic.OneSecondWait;
				}
			}
			else {
				if (Credentials.AutoRetrieveNewAuth && !client.CheckTokenIsInQueue(Credentials) && Credentials.CheckRefreshNeeded()) {
					client.GetNewAuthToken(Credentials);
				}

				if (client.GettingNewToken || authRequestOrder.Count > 0 || client.CheckTokenIsInQueue(Credentials)) {
					WaitWhile waiting = new WaitWhile(() => !cancelToken.IsCancellationRequested && (client.GettingNewToken || authRequestOrder.Count > 0 || client.CheckTokenIsInQueue(Credentials)));

					yield return waiting;

					if (cancelToken.IsCancellationRequested) {
						yield break;
					}
				}

				if (Credentials.CheckRefreshNeeded()) {
					DebugManager.LogMessage(Credentials.AutoRetrieveNewAuth
						? $"MakeTwitchAPIRequest; Auth token attempted to refresh however after waiting the token still needs refreshing before making the request, Token {{{Credentials.TokenID}}}, Task Aborted: {Endpoint} {Method}"
						: $"MakeTwitchAPIRequest; Auth token indicates it requires refreshing before continuing, Token {{{Credentials.TokenID}}}, Task Aborted: {Endpoint} {Method}", DebugManager.ErrorLevel.Error);
					yield break;
				}

				if (cancelToken.IsCancellationRequested) {
					yield break;
				}
			}

			if (!QueryParameters.IsNullOrEmpty(out int len)) {
				builder.Append('?');
				int x = 0;
				builder.Append(QueryParameters[x].Item1);
				builder.Append('=');
				builder.Append(QueryParameters[x].Item2);
				x++;
				for (; x < len; x++) {
					builder.Append('&');
					builder.Append(QueryParameters[x].Item1);
					builder.Append('=');
					builder.Append(QueryParameters[x].Item2);
				}
			}

			string builtEndpoint = builder.ToString();

			using (UnityWebRequest webRequest = new UnityWebRequest(builtEndpoint, Method.ToString()))
			using (webRequest.downloadHandler = new DownloadHandlerBuffer())
			using (webRequest.uploadHandler = string.IsNullOrEmpty(Body) ? null : new UploadHandlerRaw(Encoding.UTF8.GetBytes(Body)) { contentType = ContentType }) {

				if (!HeaderValues.IsNullOrEmpty(out int headerLen)) {
					for (int x = 0; x < headerLen; x++) {
						(string, string) header = HeaderValues[x];
						if (header.Item1.Equals(TwitchWords.CONTENT_TYPE)) {
							ContentType = header.Item2;
						}
						else {
							webRequest.SetRequestHeader(header.Item1, header.Item2);
						}
					}
				}
				if (string.IsNullOrWhiteSpace(ContentType)) {
					ContentType = TwitchWords.APPLICATION_URLENCODED;
				}
				webRequest.SetRequestHeader(TwitchWords.CONTENT_TYPE, ContentType);

				if (!isAuthRequest) {
					string authValue = TwitchStatic.AppendBearerToOAuth(Credentials.OAuthToken.Access_Token);
					webRequest.SetRequestHeader(TwitchWords.AUTHORIZATION, authValue);
					(string clientName, string clientValue) = client.AquireClientIDParams();
					webRequest.SetRequestHeader(clientName, clientValue);
				}

				if (cancelToken.IsCancellationRequested) {
					yield break;
				}

				UnityWebRequestAsyncOperation request = webRequest.SendWebRequest();

				while (!request.isDone) { // Allows mid request cancel for the web request
					yield return TwitchStatic.OneSecondWait;

					if (cancelToken.IsCancellationRequested) {
						webRequest.Abort();
						yield break;
					}
				}

				//yield return webRequest.SendWebRequest();

				builder.Clear();
				builder.Append('{');
				AppendValueToBuilder(ref builder, false, ENDPOINT, builtEndpoint, true);

				if (webRequest.result == UnityWebRequest.Result.Success) {
					if (client.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage($"Twitch API call successful, Endpoint: {builtEndpoint}".RichTextColour("green"));
					}

					AppendValueToBuilder(ref builder, true, RESPONSE, webRequest.downloadHandler.text, string.Equals(Endpoint, TwitchAPILinks.GetChanneliCalendar));

					AppendValueToBuilder(ref builder, true, STATUS_CODE, webRequest.responseCode.ToString(), false);

					AppendValueToBuilder(ref builder, true, HAS_ERRORED, webRequest.responseCode < 200 | webRequest.responseCode > 299 ? "true" : "false", false);

					AppendValueToBuilder(ref builder, true, ERROR_TEXT, "", true);
				}
				else {
					if (client.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage(webRequest.error);
					}
					if (!blockRetry
						&& !isAuthRequest
						&& webRequest.responseCode == 401
						&& client.DefaultRequestSettings.RetryOnUnauthorised
						&& Credentials.AutoRetrieveNewAuth) {
						if (cancelToken.IsCancellationRequested) {
							yield break;
						}

						client.GetNewAuthToken(Credentials);

						if (!retryPerformed) {
							retryPerformed = true;
							if (client.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
								DebugManager.LogMessage($"Attempting to Retry Call to Twitch API after Auth refresh, Endpoint: {builtEndpoint}".RichTextColour("orange"));
							}
							if (cancelToken.IsCancellationRequested) {
								yield break;
							}
							goto retry;
						}
					}

					AppendValueToBuilder(ref builder, true, RESPONSE, webRequest.downloadHandler.text, false);

					AppendValueToBuilder(ref builder, true, STATUS_CODE, webRequest.responseCode.ToString(), false);

					AppendValueToBuilder(ref builder, true, HAS_ERRORED, "true", false);

					AppendValueToBuilder(ref builder, true, ERROR_TEXT, webRequest.error, true);
				}
				builder.Append('}');
			}
			string returnValue = builder.ToString();
			ReturnStringBuilder(builder);
			yield return returnValue;
		}

		/// <summary>
		/// Main Task to make requests to Twitchs API. Returned in non pretty Json.
		/// </summary>
		/// <param name="Endpoint">API URL.</param>
		/// <param name="Method">HTTP Method Type.</param>
		/// <param name="Credentials">OAuth token to use for this request.</param>
		/// <param name="HeaderValues">Values to be loaded into the request Header.</param>
		/// <param name="QueryParameters">Values to be loaded into the UploadHandler.</param>
		/// <param name="Body">Data body to be sent to Twitch, encoded using <c>Encoding.UTF8</c></param>
		/// <returns><code>string</code> Raw response from server</returns>
		private static async Task<string> InternalMakeTwitchAPIRequestAsync(
											string Endpoint,
											TwitchAPIRequestMethod Method,
											TokenInstance Credentials,
											(string, string)[] HeaderValues,
											(string, string)[] QueryParameters,
											string Body,
											string ContentType,
											CancellationToken cancelToken,
											IScope reference = null,
											APIScopeWarning ProvidedScopeSettings = (APIScopeWarning)(-1),
											bool blockRetry = false) {
			if (CreateOrGetInstance(out TwitchAPIClient client)) {
				if (Credentials == null) {
					if (client.DefaultAPIToken == null) {
						throw new Exception($"TwitchAPIClient no token provided by method or available inside API client, Task Aborted: {Endpoint} {Method}");
					}
					Credentials = client.DefaultAPIToken;
				}
			}
			else {
				throw new Exception($"TwitchAPIClient does not currently exist, Task Aborted: {Endpoint} {Method}");
			}

			cancelToken.ThrowIfCancellationRequested();

			APIScopeWarning ScopeSettings;
			if (ProvidedScopeSettings < 0) {
				ScopeSettings = client.DefaultRequestSettings.ScopeWarning;
			}
			else {
				ScopeSettings = ProvidedScopeSettings;
			}

			bool isAuthRequest = IAuth.EndpointIsAuthRequest(Endpoint);

			bool retryPerformed = false;

			StringBuilder builder = RequestStringBuilder();

			retry:
			builder.Clear();
			builder.Append(Endpoint);

			if (cancelToken == default) {
				cancelToken = client.APICancelToken;
			}
			cancelToken.ThrowIfCancellationRequested();

			if (client.LogDebugLevel != DebugManager.DebugLevel.None) {
				DebugManager.LogMessage($"Attempting Call to Twitch API, Endpoint: {Endpoint}, Method: {Method}, HeaderValues: {HeaderValues.ToJSONString()}, QueryParameters: {QueryParameters.ToJSONString()}, Body: {Body}".RichTextColour(Color.cyan));
			}

			if (Credentials == null) {
				if (client.DefaultAPIToken == null) {
					throw new Exception($"TwitchAPIClient no token provided by method or available inside API client, Task Aborted: {Endpoint} {Method}");
				}
				Credentials = client.DefaultAPIToken;
			}

			if (reference != null && !retryPerformed) {
				Credentials.PerformScopeCheck(in ScopeSettings, reference);
			}

			if (isAuthRequest) {
				while (client.currentTokenBeingRefreshed != Credentials) {
					await Task.Delay(1000, cancelToken);
					cancelToken.ThrowIfCancellationRequested();
				}
			}
			else {
				if (Credentials.AutoRetrieveNewAuth && !client.CheckTokenIsInQueue(Credentials) && await Credentials.CheckRefreshNeededAsync()) {
					client.GetNewAuthToken(Credentials);
				}

				while (client.CheckTokenIsInQueue(Credentials)) {
					await Task.Delay(1000, cancelToken);
					cancelToken.ThrowIfCancellationRequested();
				}

				if (await Credentials.CheckRefreshNeededAsync()) {
					throw new Exception(Credentials.AutoRetrieveNewAuth
						? $"MakeTwitchAPIRequest; Auth token attempted to refresh however after waiting the token still needs refreshing before making the request, Token {{{Credentials.TokenID}}}, Task Aborted: {Endpoint} {Method}"
						: $"MakeTwitchAPIRequest; Auth token indicates it requires refreshing before continuing, Token {{{Credentials.TokenID}}}, Task Aborted: {Endpoint} {Method}");
				}
			}

			if (!QueryParameters.IsNullOrEmpty(out int len)) {
				builder.Append('?');
				int x = 0;
				builder.Append(QueryParameters[x].Item1);
				builder.Append('=');
				builder.Append(QueryParameters[x].Item2);
				x++;
				for (; x < len; x++) {
					builder.Append('&');
					builder.Append(QueryParameters[x].Item1);
					builder.Append('=');
					builder.Append(QueryParameters[x].Item2);
				}
			}

			string builtEndpoint = builder.ToString();

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(builtEndpoint);
			request.Method = Method.ToString();
			request.ContinueTimeout = 2500;
			request.Timeout = 5000;
			request.ContentType = string.IsNullOrWhiteSpace(ContentType) ? TwitchWords.APPLICATION_URLENCODED : ContentType;

			if (!string.IsNullOrWhiteSpace(Body)) {
				(await request.GetRequestStreamAsync()).Write(Encoding.UTF8.GetBytes(Body));
			}

			if (!isAuthRequest) {
				string authValue = TwitchStatic.AppendBearerToOAuth(Credentials.OAuthToken.Access_Token);
				request.Headers.Add(TwitchWords.AUTHORIZATION, authValue);
				(string clientName, string clientValue) = client.AquireClientIDParams();
				request.Headers.Add(clientName, clientValue);
			}
			cancelToken.ThrowIfCancellationRequested();

			if (!HeaderValues.IsNullOrEmpty(out int headerLen)) {
				for (int x = 0; x < headerLen; x++) {
					(string, string) header = HeaderValues[x];
					if (!header.Item1.Equals(TwitchWords.CONTENT_TYPE)) {
						request.Headers.Add(header.Item1, header.Item2);
					}
				}
			}

			cancelToken.ThrowIfCancellationRequested();
			builder.Clear();
			builder.Append('{');
			AppendValueToBuilder(ref builder, false, ENDPOINT, builtEndpoint, true);

			try {
				using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
				using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
					cancelToken.ThrowIfCancellationRequested();
					string responseString = reader.ReadToEnd();
					int statusCode = (int)response.StatusCode;

					AppendValueToBuilder(ref builder, true, RESPONSE, responseString, string.Equals(Endpoint, TwitchAPILinks.GetChanneliCalendar));

					AppendValueToBuilder(ref builder, true, STATUS_CODE, statusCode.ToString(), false);

					AppendValueToBuilder(ref builder, true, HAS_ERRORED, statusCode < 200 | statusCode > 299 ? "true" : "false", false);

					AppendValueToBuilder(ref builder, true, ERROR_TEXT, "", true);

					if (client.LogDebugLevel != DebugManager.DebugLevel.None) {
						DebugManager.LogMessage($"Twitch API call successful, Endpoint: {builtEndpoint}".RichTextColour("green"));
					}
				}
			} catch (WebException ex) {
				if (client.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				using (HttpWebResponse response = (HttpWebResponse)ex.Response)
				using (StreamReader reader = new StreamReader(response.GetResponseStream())) {

					cancelToken.ThrowIfCancellationRequested();
					string responseString = reader.ReadToEnd();
					int statusCode = (int)response.StatusCode;

					if (!blockRetry
						&& !isAuthRequest
						&& statusCode == 401
						&& client.DefaultRequestSettings.RetryOnUnauthorised
						&& Credentials.AutoRetrieveNewAuth) {
						cancelToken.ThrowIfCancellationRequested();
						client.GetNewAuthToken(Credentials);

						// If auth is incorrect perform a single retry
						if (!retryPerformed) {
							retryPerformed = true;
							if (client.LogDebugLevel > DebugManager.DebugLevel.Necessary) {
								DebugManager.LogMessage($"Attempting to Retry Call to Twitch API after Auth refresh, Endpoint: {builtEndpoint}".RichTextColour("orange"));
							}
							goto retry;
						}
					}

					AppendValueToBuilder(ref builder, true, RESPONSE, responseString, false);

					AppendValueToBuilder(ref builder, true, STATUS_CODE, statusCode.ToString(), false);

					AppendValueToBuilder(ref builder, true, HAS_ERRORED, "true", false);

					AppendValueToBuilder(ref builder, true, ERROR_TEXT, ex.Message, true);
				}
			} catch (Exception ex) {
				if (client.LogDebugLevel != DebugManager.DebugLevel.None) {
					DebugManager.LogMessage(ex);
				}
				cancelToken.ThrowIfCancellationRequested();

				AppendValueToBuilder(ref builder, true, RESPONSE, "", false);

				AppendValueToBuilder(ref builder, true, STATUS_CODE, "-1", false);

				AppendValueToBuilder(ref builder, true, HAS_ERRORED, "true", false);

				AppendValueToBuilder(ref builder, true, ERROR_TEXT, ex.Message, true);
			}

			builder.Append('}');
			string result = builder.ToString();
			ReturnStringBuilder(builder);
			return result;
		}

		private static void AppendValueToBuilder(ref StringBuilder builder, bool addComma, string name, string value, bool isString) {
			if (addComma) {
				builder.Append(',');
			}
			builder.Append('"');
			builder.Append(name);
			builder.Append('"');
			builder.Append(':');
			if (isString) {
				builder.Append('"');
				builder.Append(value);
				builder.Append('"');
			}
			else {
				builder.Append(value);
			}
		}

		private static string AquireContentType((string, string)[] headers) {
			for (int x = 0; x < headers?.Length; x++) {
				(string, string) header = headers[x];
				if (header.Item1.Equals(TwitchWords.CONTENT_TYPE)) {
					return header.Item2;
				}
			}
			return null;
		}

		private static string AquireContentType<T>((string, string)[] headers) where T : ITwitchAPIDataObject {
			string found = AquireContentType(headers);
			if (!string.IsNullOrWhiteSpace(found)) {
				return found;
			}
			if (typeof(IJsonRequest).IsAssignableFrom(typeof(T))) {
				return TwitchWords.APPLICATION_JSON;
			}
			else {
				return TwitchWords.APPLICATION_URLENCODED;
			}
		}
	}
}
