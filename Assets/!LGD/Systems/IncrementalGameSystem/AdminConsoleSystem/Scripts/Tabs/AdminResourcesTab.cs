using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Admin tab for managing resources
/// </summary>
public class AdminResourcesTab : AdminTabBase
{
    private List<Resource> _allResources = new List<Resource>();
    private Dictionary<Resource, string> _resourceInputs = new Dictionary<Resource, string>();

    public override void RefreshData()
    {
        var resourceRegistry = RegistryManager.Instance?.GetRegistry<Resource>() as ResourceRegistry;
        if (resourceRegistry != null && ResourceManager.Instance != null)
        {
            _allResources = resourceRegistry.GetAllItems();

            // Initialize input fields for resources
            foreach (var resource in _allResources)
            {
                if (!_resourceInputs.ContainsKey(resource))
                {
                    _resourceInputs[resource] = "1000";
                }
            }
        }
    }

    public override void DrawTab()
    {
        GUILayout.Label("Resource Management", HeaderStyle);
        GUILayout.Space(5);

        if (_allResources.Count == 0)
        {
            GUILayout.Label("No resources found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", ButtonStyle))
        {
            RefreshData();
        }
        if (GUILayout.Button("Clear All Resources", ButtonStyle))
        {
            ClearAllResources();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(450));

        foreach (var resource in _allResources)
        {
            GUILayout.BeginVertical(BoxStyle);

            GUILayout.Label($"{resource.displayName}", HeaderStyle);

            AlphabeticNotation currentAmount = ResourceManager.Instance.GetResourceAmount(resource);
            GUILayout.Label($"Current: {currentAmount.ToString()}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount:", GUILayout.Width(80));

            if (_resourceInputs.TryGetValue(resource, out string currentInput))
            {
                _resourceInputs[resource] = GUILayout.TextField(currentInput, GUILayout.Width(150));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add", GUILayout.Width(80)))
            {
                if (double.TryParse(_resourceInputs[resource], out double amount))
                {
                    ResourceManager.Instance.AddResource(resource, new AlphabeticNotation(amount));
                }
            }

            if (GUILayout.Button("Set", GUILayout.Width(80)))
            {
                if (double.TryParse(_resourceInputs[resource], out double amount))
                {
                    ResourceManager.Instance.SetResource(resource, new AlphabeticNotation(amount));
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                ResourceManager.Instance.SetResource(resource, AlphabeticNotation.zero);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }

    private void ClearAllResources()
    {
        foreach (var resource in _allResources)
        {
            ResourceManager.Instance.SetResource(resource, AlphabeticNotation.zero);
        }
        DebugManager.Log("[Admin] Cleared all resources");
    }
}
