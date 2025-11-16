using Sirenix.OdinInspector;
using System.Collections.Generic;

using UnityEngine;
/// <summary>
/// ScriptableObject that defines a collection of barks for a specific context.
/// Create one per context (e.g., BarkBlueprint_DevotingAmbient).
/// </summary>
[CreateAssetMenu(fileName = "BarkBlueprint_[NAME]", menuName = "LGD/Barks/Bark Blueprint")]
public class BarkBlueprint : ScriptableObject
{
    [FoldoutGroup("Identity"), Tooltip("Unique identifier for this bark set")]
    public string barkId;

    [FoldoutGroup("Identity"), Tooltip("When should these barks be used?")]
    public BarkContext context;

    [FoldoutGroup("Identity"), Tooltip("Higher priority = more specific. Zone-specific = 10, Global = 1")]
    [Range(1, 10)]
    public int priority = 1;

    [FoldoutGroup("Barks"), TextArea(3, 10)]
    [InfoBox("Add multiple bark variations. One will be picked randomly when triggered.")]
    public List<string> barks = new List<string>();

    [FoldoutGroup("Metadata"), ReadOnly]
    public int barkCount;

    private void OnValidate()
    {
        barkCount = barks.Count;

        // Auto-generate ID from context if empty
        if (string.IsNullOrEmpty(barkId))
        {
            barkId = context.ToString().ToLower();
        }
    }

    #region Editor Utilities
#if UNITY_EDITOR
    [Button("Rename Asset to Match Context"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string newName = $"BarkBlueprint_{context}";
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