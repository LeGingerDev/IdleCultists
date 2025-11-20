using LGD.Core.Events;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Admin tab for managing purchasables
/// </summary>
public class AdminPurchasablesTab : AdminTabBase
{
    private List<BasePurchasableRuntimeData> _allPurchasables = new List<BasePurchasableRuntimeData>();
    private Dictionary<string, BasePurchasable> _purchasableBlueprintCache = new Dictionary<string, BasePurchasable>();

    public override void RefreshData()
    {
        if (PurchasableManager.Instance != null && PurchasableManager.Instance.IsInitialized())
        {
            _allPurchasables = PurchasableManager.Instance.GetAllPurchasables();

            // Cache blueprints
            var purchasableRegistry = RegistryManager.Instance?.GetRegistry<BasePurchasable>() as PurchasableRegistry;
            if (purchasableRegistry != null)
            {
                _purchasableBlueprintCache.Clear();
                foreach (var runtime in _allPurchasables)
                {
                    var blueprint = purchasableRegistry.GetItemById(runtime.purchasableId);
                    if (blueprint != null)
                    {
                        _purchasableBlueprintCache[runtime.purchasableId] = blueprint;
                    }
                }
            }
        }
    }

    public override void DrawTab()
    {
        GUILayout.Label("Purchasable Management", HeaderStyle);
        GUILayout.Space(5);

        if (_allPurchasables.Count == 0)
        {
            GUILayout.Label("No purchasables found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", ButtonStyle))
        {
            RefreshData();
        }
        if (GUILayout.Button("Reset All to 0", ButtonStyle))
        {
            ResetAllPurchases();
        }
        if (GUILayout.Button("Max All Purchases", ButtonStyle))
        {
            MaxAllPurchases();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(450));

        foreach (var runtimeData in _allPurchasables)
        {
            if (!_purchasableBlueprintCache.TryGetValue(runtimeData.purchasableId, out BasePurchasable blueprint))
                continue;

            GUILayout.BeginVertical(BoxStyle);

            GUILayout.BeginHorizontal();

            // Left side - Basic info and buttons
            GUILayout.BeginVertical(GUILayout.Width(400));

            GUILayout.Label($"{blueprint.displayName}", HeaderStyle);
            GUILayout.Label($"ID: {blueprint.purchasableId}");
            GUILayout.Label($"Type: {blueprint.purchaseType}");
            GUILayout.Label($"Purchase Count: {runtimeData.purchaseCount}");
            GUILayout.Label($"Active: {runtimeData.isActive}");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("-1", GUILayout.Width(50)))
            {
                ModifyPurchaseCount(runtimeData, -1);
            }

            if (GUILayout.Button("+1", GUILayout.Width(50)))
            {
                ModifyPurchaseCount(runtimeData, 1);
            }

            if (GUILayout.Button("+10", GUILayout.Width(50)))
            {
                ModifyPurchaseCount(runtimeData, 10);
            }

            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                runtimeData.purchaseCount = 0;
                runtimeData.isActive = false;
                SavePurchasables();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Right side - Stat modifiers (if applicable)
            if (blueprint is StatPurchasable statPurchasable && runtimeData.purchaseCount > 0)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Current Modifiers:", SubHeaderStyle);

                var modifiers = statPurchasable.GetModifiersAtTier(runtimeData.purchaseCount);
                foreach (var mod in modifiers)
                {
                    string displayValue = mod.modifierType == ModifierType.Multiplicative
                        ? $"+{mod.multiplicativeValue * 100:F1}%"
                        : $"+{mod.additiveValue}";

                    GUILayout.Label($"  {mod.statType}: {displayValue}");
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }

    private void ModifyPurchaseCount(BasePurchasableRuntimeData runtimeData, int delta)
    {
        runtimeData.purchaseCount = Mathf.Max(0, runtimeData.purchaseCount + delta);

        if (runtimeData.purchaseCount > 0)
        {
            runtimeData.isActive = true;
        }

        SavePurchasables();
        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
    }

    private void ResetAllPurchases()
    {
        // Destroy all entities first
        DestroyAllEntities();

        foreach (var purchasable in _allPurchasables)
        {
            purchasable.purchaseCount = 0;
            purchasable.isActive = false;
        }
        SavePurchasables();
        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[Admin] Reset all purchases and destroyed all entities");
    }

    private void MaxAllPurchases()
    {
        foreach (var runtimeData in _allPurchasables)
        {
            if (_purchasableBlueprintCache.TryGetValue(runtimeData.purchasableId, out BasePurchasable blueprint))
            {
                if (blueprint.purchaseType == PurchaseType.Infinite)
                {
                    runtimeData.purchaseCount = 100;
                }
                else if (blueprint.purchaseType == PurchaseType.Capped && blueprint.maxPurchases > 0)
                {
                    runtimeData.purchaseCount = blueprint.maxPurchases;
                }
                else
                {
                    runtimeData.purchaseCount = 1;
                }
                runtimeData.isActive = true;
            }
        }
        SavePurchasables();
        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[Admin] Maxed all purchases");
    }

    private void SavePurchasables()
    {
        Console.StartCoroutine(PurchasableManager.Instance.ManualSave());
    }

    private void DestroyAllEntities()
    {
        EntityController[] allEntities = Object.FindObjectsOfType<EntityController>();
        int destroyedCount = allEntities.Length;

        foreach (EntityController entity in allEntities)
        {
            Object.Destroy(entity.gameObject);
        }

        if (destroyedCount > 0)
        {
            DebugManager.Log($"[Admin] Destroyed {destroyedCount} entities");
        }
    }
}
