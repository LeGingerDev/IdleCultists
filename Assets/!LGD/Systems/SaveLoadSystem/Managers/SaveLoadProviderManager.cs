using LGD.Core.Singleton;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadProviderManager : MonoSingleton<SaveLoadProviderManager>
{
    private Dictionary<Type, object> _providers = new Dictionary<Type, object>();

    private Coroutine _saveLoopCoroutine;

    public void Start()
    {
        _saveLoopCoroutine = StartCoroutine(SaveLoop());
    }



    private void OnApplicationQuit()
    {
        DebugManager.Log("[SaveLoad] <color=yellow>Application quitting - saving all dirty providers...</color>");

        // Save all dirty providers synchronously before quit
        int dirtySaved = 0;
        foreach (object providerObj in _providers.Values)
        {
            ISaveLoadProvider provider = providerObj as ISaveLoadProvider;
            if (provider != null && provider.IsDirty())
            {
                // We need to run the save synchronously since the app is quitting
                StartCoroutine(provider.Save());
                dirtySaved++;
            }
        }

        if (dirtySaved > 0)
        {
            DebugManager.Log($"[SaveLoad] <color=green>Saved {dirtySaved} dirty provider(s) on quit</color>");
        }
        else
        {
            DebugManager.Log("[SaveLoad] <color=cyan>No dirty providers to save on quit</color>");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Save when losing focus (player tabs out, minimizes, etc.)
        if (!hasFocus)
        {
            DebugManager.Log("[SaveLoad] <color=yellow>Application lost focus - saving dirty providers...</color>");
            StartCoroutine(SaveAllDirty());
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Save when pausing (mainly for mobile - app goes to background)
        if (pauseStatus)
        {
            DebugManager.Log("[SaveLoad] <color=yellow>Application paused - saving dirty providers...</color>");
            StartCoroutine(SaveAllDirty());
        }
    }

    public void RestartSaveLoop()
    {
        if (_saveLoopCoroutine != null)
        {
            StopCoroutine(_saveLoopCoroutine);
        }
        _saveLoopCoroutine = StartCoroutine(SaveLoop());
    }

    public IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); // Save every 60 seconds
            yield return SaveAllDirty();
        }
    }

    public void RegisterProvider<T>(SaveLoadProviderBase<T> provider)
    {
        Type type = typeof(T);
        if (_providers.ContainsKey(type))
        {
            DebugManager.Warning($"[SaveLoad] Save/Load provider already exists for {type.Name}");
            return;
        }
        _providers.Add(type, provider);
        DebugManager.Log($"[SaveLoad] <color=green>Registered</color> {type.Name} save/load provider");
    }

    public SaveLoadProviderBase<T> GetProvider<T>()
    {
        Type type = typeof(T);
        if (_providers.TryGetValue(type, out object provider))
        {
            return provider as SaveLoadProviderBase<T>;
        }

        DebugManager.Error($"[SaveLoad] No save/load provider found for {type.Name}");
        return null;
    }

    public IEnumerator SaveAll()
    {
        DebugManager.Log($"[SaveLoad] <color=cyan>Saving all providers...</color> ({_providers.Count} total)");

        foreach (object providerObj in _providers.Values)
        {
            ISaveLoadProvider provider = providerObj as ISaveLoadProvider;
            if (provider != null)
            {
                yield return provider.Save();
            }
        }

        RestartSaveLoop();

        DebugManager.Log($"[SaveLoad] <color=green>All providers saved!</color>");
    }

    public IEnumerator SaveAllDirty()
    {
        int dirtySaved = 0;

        foreach (object providerObj in _providers.Values)
        {
            ISaveLoadProvider provider = providerObj as ISaveLoadProvider;
            if (provider != null && provider.IsDirty())
            {
                yield return provider.Save();
                dirtySaved++;
            }
        }

        RestartSaveLoop();

        if (dirtySaved > 0)
        {
            DebugManager.Log($"[SaveLoad] <color=green>Saved {dirtySaved} dirty provider(s)</color>");
        }
    }

    public IEnumerator LoadAll()
    {
        DebugManager.Log($"[SaveLoad] <color=cyan>Loading all providers...</color> ({_providers.Count} total)");

        foreach (object providerObj in _providers.Values)
        {
            ISaveLoadProvider provider = providerObj as ISaveLoadProvider;
            if (provider != null)
            {
                yield return provider.Load();
            }
        }

        DebugManager.Log($"[SaveLoad] <color=green>All providers loaded!</color>");
    }

    public int GetDirtyProviderCount()
    {
        int count = 0;
        foreach (object providerObj in _providers.Values)
        {
            ISaveLoadProvider provider = providerObj as ISaveLoadProvider;
            if (provider != null && provider.IsDirty())
            {
                count++;
            }
        }
        return count;
    }

    public void ResetAllProviders()
    {
        foreach (object providerObj in _providers.Values)
        {
            ISaveLoadProvider provider = providerObj as ISaveLoadProvider;
            provider.ResetData();
        }
    }
}