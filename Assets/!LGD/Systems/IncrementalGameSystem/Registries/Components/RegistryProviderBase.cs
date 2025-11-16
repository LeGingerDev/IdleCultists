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

        // Search for all ScriptableObjects in Assets/_Game
        // Note: Unity's FindAssets "t:TypeName" only finds exact matches, not derived types
        // So we search for all ScriptableObjects and filter by type
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Game" });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            // Check if this ScriptableObject is assignable to type T (includes derived types)
            if (asset != null && asset is T typedAsset)
            {
                _items.Add(typedAsset);
            }
        }

        // Mark the component as dirty so Unity saves the changes
        EditorUtility.SetDirty(this);

        DebugManager.Log($"[IncrementalGame] <color=green>Registry Populated:</color> Found {_items.Count} {typeof(T).Name}(s) and derived types in {gameObject.name}");
    }
#else
    public void PopulateRegistry() { }
#endif
}