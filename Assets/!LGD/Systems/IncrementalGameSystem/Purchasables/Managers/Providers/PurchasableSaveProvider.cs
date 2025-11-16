using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasableSaveProvider : SaveLoadProviderBase<PurchasableRuntimeData>
{
    protected override string GetSaveFileName()
    {
        return "purchasables.json";
    }

    protected override IEnumerator CreateDefault()
    {
        _data.Clear();

        RegistryProviderBase<PurchasableBlueprint> registry = RegistryManager.Instance.GetRegistry<PurchasableBlueprint>();

        if (registry != null)
        {
            List<PurchasableBlueprint> allPurchasables = registry.GetAllItems();

            foreach (PurchasableBlueprint purchasable in allPurchasables)
            {
                if (purchasable != null)
                {
                    // Create default runtime data: timesPurchased = 0
                    PurchasableRuntimeData runtimeData = new PurchasableRuntimeData(purchasable.purchasableId);
                    _data.Add(runtimeData);
                }
            }

            DebugManager.Log($"[IncrementalGame] <color=green>Created default purchasable data:</color> {_data.Count} purchasables");
        }
        else
        {
            DebugManager.Error("[IncrementalGame] PurchasableRegistry not found! Cannot create default purchasable data");
        }

        yield return null;
    }

    /// <summary>
    /// Syncs new purchasables from the registry into the existing save data.
    /// Call this after loading to ensure any newly added purchasables are included.
    /// </summary>
    public IEnumerator SyncWithRegistry()
    {
        RegistryProviderBase<PurchasableBlueprint> registry = RegistryManager.Instance.GetRegistry<PurchasableBlueprint>();

        if (registry != null)
        {
            List<PurchasableBlueprint> allPurchasables = registry.GetAllItems();
            int addedCount = 0;

            foreach (PurchasableBlueprint purchasable in allPurchasables)
            {
                if (purchasable == null) continue;

                // Check if this purchasable already exists in save data
                bool exists = _data.Exists(runtime => runtime.purchasableId == purchasable.purchasableId);

                if (!exists)
                {
                    // New purchasable detected - add it with default values
                    PurchasableRuntimeData runtimeData = new PurchasableRuntimeData(purchasable.purchasableId);
                    _data.Add(runtimeData);
                    addedCount++;
                    DebugManager.Log($"[IncrementalGame] <color=yellow>New purchasable detected:</color> {purchasable.purchasableId}");
                }
            }

            if (addedCount > 0)
            {
                MarkDirty();
                yield return Save(); // Save immediately to persist new purchasables
                DebugManager.Log($"[IncrementalGame] <color=green>Synced {addedCount} new purchasable(s) from registry</color>");
            }
            else
            {
                DebugManager.Log($"[IncrementalGame] <color=cyan>Registry sync complete:</color> No new purchasables to add");
            }
        }
        else
        {
            DebugManager.Error("[IncrementalGame] PurchasableRegistry not found! Cannot sync with registry");
        }

        yield return null;
    }
}