using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor;
#endif

public abstract class RegistryProviderBase<T> : MonoBehaviour, IRegistry where T : ScriptableObject
{
    [SerializeField]
    protected List<T> _items = new List<T>();

    protected virtual void Awake()
    {
        RegistryManager.Instance.RegisterRegistry(this);
    }

    public abstract T GetItemById(string id);

    public List<T> GetAllItems() => _items;

#if UNITY_EDITOR
    [Button("Auto-Populate Registry", ButtonSizes.Large), PropertyOrder(-1)]
    public void PopulateRegistry()
    {
        _items.Clear();

        // Search for all ScriptableObjects of type T recursively in Assets/_Game
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_Game" });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset != null)
            {
                _items.Add(asset);
            }
        }

        // Mark the component as dirty so Unity saves the changes
        EditorUtility.SetDirty(this);

        DebugManager.Log($"[IncrementalGame] <color=green>Registry Populated:</color> Found {_items.Count} {typeof(T).Name}(s) in {gameObject.name}");
    }
#else
    public void PopulateRegistry() { }
#endif
}