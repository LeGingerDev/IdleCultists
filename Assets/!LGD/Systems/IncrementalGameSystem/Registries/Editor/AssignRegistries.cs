#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Microsoft.Win32;
using System.Linq;

public static class AssignRegistries
{
    [MenuItem("LGD/Registries/Auto-Populate All Registries")]
    private static void PopulateAllRegistries()
    {
        // Find all IRegistry implementations in the scene
        IRegistry[] registries = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IRegistry>()
            .ToArray();

        if (registries.Length == 0)
        {
            DebugManager.Warning("[IncrementalGame] <color=yellow>No registries found in the scene!</color>");
            return;
        }

        foreach (IRegistry registry in registries)
        {
            registry.PopulateRegistry();
        }

        DebugManager.Log($"[IncrementalGame] <color=green>Successfully populated {registries.Length} registries!</color>");
    }
}
#endif