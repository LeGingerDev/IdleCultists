using System.Collections;
using UnityEngine;

/// <summary>
/// Debug utility to help locate save files.
/// Attach to a GameObject and it will log the persistent data path and all save file locations.
/// </summary>
public class SaveFileDebugger : MonoBehaviour
{
    [SerializeField]
    private bool _logOnStart = true;

    [SerializeField]
    private bool _openFolderOnStart = false;

    private void Start()
    {
        if (_logOnStart)
        {
            StartCoroutine(LogSaveLocations());
        }

        if (_openFolderOnStart)
        {
            OpenPersistentDataFolder();
        }
    }

    private IEnumerator LogSaveLocations()
    {
        // Wait a frame for providers to register
        yield return null;

        DebugManager.Log($"[SaveLoad] <color=cyan>=== SAVE FILE LOCATIONS ===</color>");
        DebugManager.Log($"[SaveLoad] <color=yellow>Persistent Data Path:</color> {Application.persistentDataPath}");

        // Log achievement save location
        SaveLoadProviderBase<AchievementRuntimeData> achievementProvider =
            SaveLoadProviderManager.Instance?.GetProvider<AchievementRuntimeData>();

        if (achievementProvider != null)
        {
            string path = achievementProvider.GetFilePathDebug();
            bool exists = System.IO.File.Exists(path);
            DebugManager.Log($"[SaveLoad] <color=yellow>Achievement Save:</color> {path} <color={(exists ? "green" : "red")}>[{(exists ? "EXISTS" : "NOT FOUND")}]</color>");
        }

        DebugManager.Log($"[SaveLoad] <color=cyan>=== END SAVE FILE LOCATIONS ===</color>");
    }

    [ContextMenu("Open Persistent Data Folder")]
    public void OpenPersistentDataFolder()
    {
        string path = Application.persistentDataPath;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start("xdg-open", path);
#else
        DebugManager.Warning("[SaveLoad] Opening folder not supported on this platform");
#endif

        DebugManager.Log($"[SaveLoad] <color=green>Opened folder:</color> {path}");
    }

    [ContextMenu("Log All Save Locations")]
    public void LogAllSaveLocations()
    {
        StartCoroutine(LogSaveLocations());
    }

    [ContextMenu("Check Achievement Save Exists")]
    public void CheckAchievementSave()
    {
        SaveLoadProviderBase<AchievementRuntimeData> provider =
            SaveLoadProviderManager.Instance?.GetProvider<AchievementRuntimeData>();

        if (provider != null)
        {
            string path = provider.GetFilePathDebug();
            bool exists = System.IO.File.Exists(path);

            if (exists)
            {
                long fileSize = new System.IO.FileInfo(path).Length;
                DebugManager.Log($"[SaveLoad] <color=green>Achievement save found!</color>\nPath: {path}\nSize: {fileSize} bytes");
            }
            else
            {
                DebugManager.Warning($"[SaveLoad] <color=red>Achievement save NOT found!</color>\nExpected path: {path}");
            }
        }
        else
        {
            DebugManager.Error("[SaveLoad] Achievement save provider not registered!");
        }
    }
}