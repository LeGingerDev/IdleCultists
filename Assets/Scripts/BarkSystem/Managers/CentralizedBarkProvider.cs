using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Centralized bark provider that queries the BarkRegistry.
/// Acts as the interface between BarkManager and the bark data.
/// </summary>
public class CentralizedBarkProvider : MonoBehaviour, IBarkProvider
{
    [SerializeField, FoldoutGroup("References")]
    private BarkRegistry _barkRegistry;

    private void Awake()
    {
        if (_barkRegistry == null)
        {
            _barkRegistry = RegistryManager.Instance.GetRegistry<BarkBlueprint>() as BarkRegistry;
        }

        if (_barkRegistry == null)
        {
            Debug.LogError("[CentralizedBarkProvider] BarkRegistry not found! Make sure it's registered in RegistryManager.");
        }
    }

    public List<string> GetBarksForContext(BarkContext context)
    {
        if (_barkRegistry == null)
        {
            Debug.LogError("[CentralizedBarkProvider] BarkRegistry is null!");
            return new List<string>();
        }

        // Get prioritized barks (only highest priority blueprints)
        return _barkRegistry.GetPrioritizedBarksForContext(context);
    }

    public int GetPriority()
    {
        // Priority is now per-blueprint, not per-provider
        return 5;
    }

    #region Debug Helpers

    [Button("Test Get Barks"), FoldoutGroup("Debug")]
    private void DebugGetBarks(BarkContext context)
    {
        var barks = GetBarksForContext(context);
        Debug.Log($"=== BARKS FOR {context} ({barks.Count}) ===");
        foreach (var bark in barks)
        {
            Debug.Log($"  - \"{bark}\"");
        }
    }

    #endregion
}

