using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using UnityEngine;


/// <summary>
/// Centralised debug manager to allow toggling logging on/off per build or at runtime.
/// Place this on your global object (same pattern as other managers).
/// Use the static wrappers: DebugManager.Log(...), DebugManager.Warning(...), DebugManager.Error(...)
/// If no instance exists in the scene, calls fall back to Unity's Debug.* so behaviour is unchanged.
/// </summary>
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

    // Basic log methods (support optional UnityEngine.Object context)
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

        if (inst._alsoLogToUnity)
        {
            if (context != null) UnityEngine.Debug.LogError(message, context);
            else UnityEngine.Debug.LogError(message);
        }
    }

    // Format helpers (and overloads with context)
    public static void LogFormat(string format, params object[] args) => Log(string.Format(format, args));
    public static void LogFormat(Object context, string format, params object[] args) => Log(string.Format(format, args), context);

    public static void WarningFormat(string format, params object[] args) => Warning(string.Format(format, args));
    public static void WarningFormat(Object context, string format, params object[] args) => Warning(string.Format(format, args), context);

    public static void ErrorFormat(string format, params object[] args) => Error(string.Format(format, args));
    public static void ErrorFormat(Object context, string format, params object[] args) => Error(string.Format(format, args), context);

    // Instance control API (runtime toggles)
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

