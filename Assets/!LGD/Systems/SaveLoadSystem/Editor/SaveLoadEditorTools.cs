#if UNITY_EDITOR
using LGD.SceneManagement;
using UnityEditor;
using UnityEngine;

public static class SaveLoadEditorTools
{
    [MenuItem("LGD/SaveLoading/Reset All Save Data and Load MainMenu")]
    public static void ResetAllSaveDataAndLoadMainMenu()
    {
        // Confirm dialog to prevent accidental resets
        bool confirmed = EditorUtility.DisplayDialog(
            "Reset All Save Data",
            "This will DELETE all save files and reset all data to defaults.\n\n" +
            "After reset, the MainMenu scene will be loaded.\n\n" +
            "Are you sure you want to continue?",
            "Yes, Reset Everything",
            "Cancel"
        );

        if (!confirmed)
        {
            DebugManager.Log("[SaveLoad] <color=yellow>Save data reset cancelled</color>");
            return;
        }

        // Find the SaveLoadProviderManager in the scene
        SaveLoadProviderManager manager = SaveLoadProviderManager.Instance;

        if (manager == null)
        {
            DebugManager.Error("[SaveLoad] SaveLoadProviderManager not found in scene! Make sure it exists before resetting.");
            return;
        }

        // Reset all providers
        manager.ResetAllProviders();

        DebugManager.Log("[SaveLoad] <color=green>All save data has been reset to defaults!</color>");

        // Load MainMenu scene using custom SceneManager
        SceneManager.Instance.GoToLevel("MainMenu");
        DebugManager.Log("[SaveLoad] <color=cyan>Loading MainMenu scene...</color>");
    }

    [MenuItem("LGD/SaveLoading/Delete All Save Files Only")]
    public static void DeleteAllSaveFilesOnly()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Delete All Save Files",
            "This will DELETE all save files from the persistent data path.\n\n" +
            "The data in memory will remain until providers are reloaded.\n\n" +
            "Are you sure?",
            "Yes, Delete Files",
            "Cancel"
        );

        if (!confirmed)
        {
            DebugManager.Log("[SaveLoad] <color=yellow>File deletion cancelled</color>");
            return;
        }

        SaveLoadProviderManager manager = GameObject.FindObjectOfType<SaveLoadProviderManager>();

        if (manager == null)
        {
            DebugManager.Error("[SaveLoad] SaveLoadProviderManager not found in scene!");
            return;
        }

        // Just delete files without resetting data
        int deletedCount = 0;
        System.Type managerType = manager.GetType();
        var providersField = managerType.GetField("_providers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (providersField != null)
        {
            var providers = (System.Collections.IDictionary)providersField.GetValue(manager);

            foreach (var providerObj in providers.Values)
            {
                ISaveLoadProvider provider = providerObj as ISaveLoadProvider;
                if (provider is MonoBehaviour providerMono)
                {
                    var method = providerMono.GetType().GetMethod("DeleteFileAndData");
                    if (method != null)
                    {
                        method.Invoke(providerMono, null);
                        deletedCount++;
                    }
                }
            }
        }

        DebugManager.Log($"[SaveLoad] <color=red>Deleted {deletedCount} save file(s)</color>");
    }

    [MenuItem("LGD/SaveLoading/Open Persistent Data Folder")]
    public static void OpenPersistentDataFolder()
    {
        string path = Application.persistentDataPath;

        if (System.IO.Directory.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
            DebugManager.Log($"[SaveLoad] Opened persistent data folder: {path}");
        }
        else
        {
            DebugManager.Warning($"[SaveLoad] Persistent data folder does not exist: {path}");
        }
    }
}
#endif