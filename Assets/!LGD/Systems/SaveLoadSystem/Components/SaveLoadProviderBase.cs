using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public interface ISaveLoadProvider
{
    IEnumerator Save();
    IEnumerator SaveIfDirty();
    IEnumerator Load();
    bool IsDirty();
    void MarkDirty();
    void MarkClean();
    void ResetData();
}

public abstract class SaveLoadProviderBase<T> : MonoBehaviour, ISaveLoadProvider
{
    [SerializeField]
    protected List<T> _data = new List<T>();

    private bool _isDirty = false;

    protected abstract string GetSaveFileName();

    public bool IsDirty() => _isDirty;

    public void MarkDirty()
    {
        _isDirty = true;
    }

    public void MarkClean()
    {
        _isDirty = false;
    }

    protected virtual void Awake()
    {
        SaveLoadProviderManager.Instance.RegisterProvider(this);
    }

    public IEnumerator SetAndSave(List<T> data)
    {
        _data = new List<T>(data);
        MarkDirty();
        yield return Save();
    }

    public IEnumerator SetData(List<T> data)
    {
        _data = new List<T>(data);
        MarkDirty();
        yield return null;
    }

    public List<T> GetData() => _data;

    public IEnumerator Save()
    {
        string path = GetFilePath();
        string json = JsonConvert.SerializeObject(_data, Formatting.Indented);

        try
        {
            File.WriteAllText(path, json);
            MarkClean();
            DebugManager.Log($"[SaveLoad] <color=green>Saved:</color> {typeof(T).Name} to {path}");
        }
        catch (System.Exception e)
        {
            DebugManager.Error($"[SaveLoad] Failed to save {typeof(T).Name}: {e.Message}");
        }

        yield return null;
    }

    public IEnumerator SaveIfDirty()
    {
        if (_isDirty)
        {
            yield return Save();
        }
    }

    public IEnumerator Load()
    {
        string path = GetFilePath();
        bool needsDefault = false;

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                _data = JsonConvert.DeserializeObject<List<T>>(json);
                DebugManager.Log($"[SaveLoad] <color=cyan>Loaded:</color> {typeof(T).Name} from {path} ({_data.Count} items)");
            }
            catch (System.Exception e)
            {
                DebugManager.Error($"[SaveLoad] Failed to load {typeof(T).Name}: {e.Message}. Creating default data.");
                needsDefault = true;
            }
        }
        else
        {
            DebugManager.Log($"[SaveLoad] <color=yellow>No save file found</color> for {typeof(T).Name}, creating default data");
            needsDefault = true;
        }

        if (needsDefault)
        {
            yield return CreateDefault();
            yield return Save(); // IMPORTANT: Must yield to actually save!
            DebugManager.Log($"[SaveLoad] <color=green>Initial save created at:</color> {path}");
        }

        yield return null;
    }

    public void DeleteFileAndData()
    {
        string path = GetFilePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            DebugManager.Log($"[SaveLoad] <color=red>Deleted save file:</color> {path}");
        }
        _data.Clear();
        MarkDirty();
    }

    public void ResetData()
    {
        DeleteFileAndData();
        StartCoroutine(CreateDefault());
    }

    protected abstract IEnumerator CreateDefault();

    protected string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, GetSaveFileName());
    }

    /// <summary>
    /// Get the full file path for debugging purposes.
    /// </summary>
    public string GetFilePathDebug()
    {
        return GetFilePath();
    }

    public bool HasData()
    {
        return _data.Count > 0;
    }
}