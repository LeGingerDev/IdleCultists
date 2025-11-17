using System.Collections.Generic;
using System.Text;
using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using UnityEngine;

public class DebugManager : MonoSingleton<DebugManager>
{
    [SerializeField, FoldoutGroup("Settings")]
    private bool _enabled = true;

    [SerializeField, FoldoutGroup("Settings")]
    private bool _logInfo = true;

    [SerializeField, FoldoutGroup("Settings")]
    private bool _logWarning = true;

    [SerializeField, FoldoutGroup("Settings")]
    private bool _logError = true;

    [SerializeField, FoldoutGroup("Settings")]
    private bool _alsoLogToUnity = true;

    [SerializeField, FoldoutGroup("Log Cache")]
    private bool _cacheLogsInMemory = true;

    [SerializeField, FoldoutGroup("Log Cache")]
    private int _maxCachedLogs = 500;

    [SerializeField, FoldoutGroup("Log Cache"), ReadOnly, MultiLineProperty(10)]
    private string _cachedLogsDisplay = "";

    private List<string> _cachedLogs = new List<string>();

    // Existing log methods updated to cache
    public static void Log(object message, Object context = null)
    {
        var inst = Instance;
        if (inst == null)
        {
            if (context != null) UnityEngine.Debug.Log(message, context);
            else UnityEngine.Debug.Log(message);
            return;
        }

        if (!inst._enabled || !inst._logInfo)
            return;

        inst.CacheLog($"[INFO] {message}");

        if (inst._alsoLogToUnity)
        {
            if (context != null) UnityEngine.Debug.Log(message, context);
            else UnityEngine.Debug.Log(message);
        }
    }

    public static void Warning(object message, Object context = null)
    {
        var inst = Instance;
        if (inst == null)
        {
            if (context != null) UnityEngine.Debug.LogWarning(message, context);
            else UnityEngine.Debug.LogWarning(message);
            return;
        }

        if (!inst._enabled || !inst._logWarning)
            return;

        inst.CacheLog($"[WARNING] {message}");

        if (inst._alsoLogToUnity)
        {
            if (context != null) UnityEngine.Debug.LogWarning(message, context);
            else UnityEngine.Debug.LogWarning(message);
        }
    }

    public static void Error(object message, Object context = null)
    {
        var inst = Instance;
        if (inst == null)
        {
            if (context != null) UnityEngine.Debug.LogError(message, context);
            else UnityEngine.Debug.LogError(message);
            return;
        }

        if (!inst._enabled || !inst._logError)
            return;

        inst.CacheLog($"[ERROR] {message}");

        if (inst._alsoLogToUnity)
        {
            if (context != null) UnityEngine.Debug.LogError(message, context);
            else UnityEngine.Debug.LogError(message);
        }
    }

    private void CacheLog(string logMessage)
    {
        if (!_cacheLogsInMemory) return;

        _cachedLogs.Add(logMessage);

        if (_cachedLogs.Count > _maxCachedLogs)
        {
            _cachedLogs.RemoveAt(0);
        }
    }

    [Button("Refresh Logs Display"), FoldoutGroup("Log Cache")]
    private void RefreshLogsDisplay()
    {
        if (_cachedLogs.Count == 0)
        {
            _cachedLogsDisplay = "No logs cached.";
            return;
        }

        var sb = new StringBuilder();
        foreach (var log in _cachedLogs)
        {
            sb.AppendLine(log);
        }
        _cachedLogsDisplay = sb.ToString();
    }

    [Button("Clear Cached Logs"), FoldoutGroup("Log Cache")]
    private void ClearCachedLogs()
    {
        _cachedLogs.Clear();
        _cachedLogsDisplay = "";
    }

    [Button("Copy to Clipboard"), FoldoutGroup("Log Cache")]
    private void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = _cachedLogsDisplay;
        UnityEngine.Debug.Log("Logs copied to clipboard!");
    }

    // Format helpers
    public static void LogFormat(string format, params object[] args) => Log(string.Format(format, args));
    public static void LogFormat(Object context, string format, params object[] args) => Log(string.Format(format, args), context);

    public static void WarningFormat(string format, params object[] args) => Warning(string.Format(format, args));
    public static void WarningFormat(Object context, string format, params object[] args) => Warning(string.Format(format, args), context);

    public static void ErrorFormat(string format, params object[] args) => Error(string.Format(format, args));
    public static void ErrorFormat(Object context, string format, params object[] args) => Error(string.Format(format, args), context);

    // Instance control API
    public void SetEnabled(bool enabled) => _enabled = enabled;
    public void SetLogInfo(bool enabled) => _logInfo = enabled;
    public void SetLogWarning(bool enabled) => _logWarning = enabled;
    public void SetLogError(bool enabled) => _logError = enabled;
    public void SetAlsoLogToUnity(bool also) => _alsoLogToUnity = also;

    [Button("Toggle All Logs"), FoldoutGroup("Settings")]
    private void ToggleAllLogs()
    {
        _enabled = !_enabled;
    }
}