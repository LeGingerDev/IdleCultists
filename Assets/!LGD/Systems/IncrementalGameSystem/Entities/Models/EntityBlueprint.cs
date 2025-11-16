using LargeNumbers;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityBlueprint_[NAME]", menuName = "LGD/Incremental/Entity/Create Blueprint")]
public class EntityBlueprint : ScriptableObject
{
    [FoldoutGroup("Identity")]
    public string id;

    [FoldoutGroup("Identity")]
    public string displayName;

    [FoldoutGroup("Identity")]
    public Sprite icon;

    [FoldoutGroup("Prefab")]
    public EntityController prefab;

    [FoldoutGroup("Stats")]
    [SerializeField] private List<StatDefinition> _stats = new List<StatDefinition>();

    public List<RuntimeStat> CopyDataToRuntime()
    {
        List<RuntimeStat> runtimeStats = new List<RuntimeStat>();

        foreach (StatDefinition stat in _stats)
        {
            runtimeStats.Add(stat.ToRuntimeStat());
        }

        return runtimeStats;
    }

    public AlphabeticNotation GetBaseStat(StatType statType)
    {
        StatDefinition stat = _stats.Find(s => s.statType == statType);
        return stat != null ? new AlphabeticNotation(stat.baseValue) : AlphabeticNotation.zero;
    }

    public string GetRandomName()
    {
        return NameRegistry.Instance.GetRandomName();
    }

    #region Editor Utilities
#if UNITY_EDITOR
    [Button("Rename Asset to Match Display Name"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("Display name is empty. Cannot rename asset.");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string directory = System.IO.Path.GetDirectoryName(assetPath);
        string extension = System.IO.Path.GetExtension(assetPath);
        string newName = $"EntityBlueprint_{displayName}";
        string newPath = System.IO.Path.Combine(directory, newName + extension);

        string result = UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);

        if (string.IsNullOrEmpty(result))
        {
            Debug.Log($"Successfully renamed asset to: {newName}");
        }
        else
        {
            Debug.LogError($"Failed to rename asset: {result}");
        }
    }
#endif
    #endregion
}