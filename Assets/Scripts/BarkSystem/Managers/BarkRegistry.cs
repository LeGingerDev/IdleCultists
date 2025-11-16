using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry that holds all BarkBlueprint ScriptableObjects.
/// Provides query methods for finding barks by context.
/// </summary>
public class BarkRegistry : RegistryProviderBase<BarkBlueprint>
{
    public override BarkBlueprint GetItemById(string id)
    {
        return _items.Find(item => item.barkId == id);
    }

    /// <summary>
    /// Get all bark blueprints for a specific context.
    /// </summary>
    public List<BarkBlueprint> GetBarksByContext(BarkContext context)
    {
        return _items.Where(b => b.context == context).ToList();
    }

    /// <summary>
    /// Get all bark strings for a specific context from all blueprints.
    /// </summary>
    public List<string> GetAllBarksForContext(BarkContext context)
    {
        var blueprints = GetBarksByContext(context);
        List<string> allBarks = new List<string>();

        foreach (var blueprint in blueprints)
        {
            allBarks.AddRange(blueprint.barks);
        }

        return allBarks;
    }

    /// <summary>
    /// Get barks for a context, respecting priority.
    /// Only returns barks from highest-priority blueprints.
    /// </summary>
    public List<string> GetPrioritizedBarksForContext(BarkContext context)
    {
        var blueprints = GetBarksByContext(context);

        if (blueprints.Count == 0)
            return new List<string>();

        // Get highest priority
        int maxPriority = blueprints.Max(b => b.priority);

        // Get only the highest priority blueprints
        var topBlueprints = blueprints.Where(b => b.priority == maxPriority).ToList();

        // Gather all barks from them
        List<string> allBarks = new List<string>();
        foreach (var blueprint in topBlueprints)
        {
            allBarks.AddRange(blueprint.barks);
        }

        return allBarks;
    }

    #region Debug Helpers

    [Button("List All Contexts"), FoldoutGroup("Debug")]
    private void DebugListContexts()
    {
        var contexts = _items.Select(b => b.context).Distinct().ToList();
        Debug.Log($"=== AVAILABLE BARK CONTEXTS ({contexts.Count}) ===");
        foreach (var context in contexts)
        {
            int barkCount = GetPrioritizedBarksForContext(context).Count;
            Debug.Log($"  - {context}: {barkCount} barks");
        }
    }

    [Button("Test Context Query"), FoldoutGroup("Debug")]
    private void DebugTestContext(BarkContext context)
    {
        var barks = GetPrioritizedBarksForContext(context);
        Debug.Log($"=== BARKS FOR {context} ({barks.Count}) ===");
        foreach (var bark in barks)
        {
            Debug.Log($"  - \"{bark}\"");
        }
    }

    #endregion
}

