using LGD.Core.Events;
using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manager for all purchasables (both StatPurchasables and EventPurchasables)
/// Handles purchasing, runtime data tracking, and provides stat modifiers for StatPurchasables
/// </summary>
public class PurchasableManager : MonoSingleton<PurchasableManager>, IStatProvider
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private List<BasePurchasableRuntimeData> _runtimeData = new List<BasePurchasableRuntimeData>();

    [SerializeField, FoldoutGroup("Settings")]
    private bool _autoSave = true;

    private PurchasableRegistry _purchasableRegistry;
    private SaveLoadProviderBase<BasePurchasableRuntimeData> _saveProvider;
    private bool _isInitialized = false;

    #region Initialization

    private void Start()
    {
        StartCoroutine(InitializePurchasables());
    }

    private IEnumerator InitializePurchasables()
    {
        _purchasableRegistry = RegistryManager.Instance.GetRegistry<BasePurchasable>() as PurchasableRegistry;
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<BasePurchasableRuntimeData>();

        if (_saveProvider != null)
        {
            yield return _saveProvider.Load();

            // Sync with registry to add any new purchasables that were added after the save file was created
            PurchasableSaveProvider purchasableProvider = _saveProvider as PurchasableSaveProvider;
            if (purchasableProvider != null)
            {
                yield return purchasableProvider.SyncWithRegistry();
            }

            _runtimeData = _saveProvider.GetData();
            DebugManager.Log($"[IncrementalGame] <color=cyan>Purchasable Manager initialized:</color> {_runtimeData.Count} purchasables loaded");
        }
        else
        {
            DebugManager.Error("[IncrementalGame] Purchasable save provider not found! Make sure PurchasableSaveProvider is in the scene.");
        }

        // Notify listeners that purchasables/runtime data are loaded so UI can refresh
        ServiceBus.Publish(PurchasableEventIds.ON_PURCHASABLES_INITIALIZED, this);

        _isInitialized = true;
    }

    #endregion

    #region Purchase Management

    /// <summary>
    /// Execute a purchase for a given purchasable ID
    /// </summary>
    public bool ExecutePurchase(string purchasableId)
    {
        BasePurchasable blueprint = _purchasableRegistry.GetItemById(purchasableId);
        if (blueprint == null)
        {
            DebugManager.Error($"[IncrementalGame] Purchasable blueprint not found: {purchasableId}");
            return false;
        }

        // Get runtime data
        BasePurchasableRuntimeData runtimeData = _runtimeData.Find(p => p.purchasableId == purchasableId);
        if (runtimeData == null)
        {
            DebugManager.Error($"[IncrementalGame] Purchasable runtime data not found: {purchasableId}");
            return false;
        }

        // Check if can purchase (OneTime/Capped/Infinite)
        if (!CanPurchaseMore(blueprint, runtimeData))
        {
            DebugManager.Warning($"[IncrementalGame] Cannot purchase {purchasableId} - already at max purchases");
            return false;
        }

        // Increment purchase count FIRST
        runtimeData.IncrementPurchase();
        DebugManager.Log($"[IncrementalGame] Incremented purchase count for {purchasableId}: now {runtimeData.purchaseCount}");

        // Execute the purchase (blueprint handles specific logic)
        blueprint.HandlePurchase(runtimeData);

        // Mark save provider as dirty
        MarkDirty();

        // Publish a generic purchasable purchased event so UI and other systems can react
        DebugManager.Log($"[IncrementalGame] ========== PUBLISHING EVENT: ON_PURCHASABLE_PURCHASED for {blueprint.purchasableId} (count: {runtimeData.purchaseCount}) ==========");
        ServiceBus.Publish(PurchasableEventIds.ON_PURCHASABLE_PURCHASED, this, blueprint, runtimeData);
        DebugManager.Log($"[IncrementalGame] Event published successfully");

        // If this is a StatPurchasable, request stat recalculation
        if (blueprint is StatPurchasable)
        {
            ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        }

        DebugManager.Log($"[IncrementalGame] Purchased {blueprint.displayName} (Purchase #{runtimeData.purchaseCount})");
        return true;
    }

    private bool CanPurchaseMore(BasePurchasable blueprint, BasePurchasableRuntimeData runtimeData)
    {
        return blueprint.purchaseType switch
        {
            PurchaseType.OneTime => runtimeData.purchaseCount == 0,
            PurchaseType.Capped => runtimeData.purchaseCount < blueprint.maxPurchases,
            PurchaseType.Infinite => true,
            _ => false
        };
    }

    #endregion

    #region Getters

    public BasePurchasableRuntimeData GetPurchasableRuntimeData(string purchasableId)
    {
        return _runtimeData.Find(p => p.purchasableId == purchasableId);
    }

    public int GetPurchaseCount(string purchasableId)
    {
        DebugManager.Log($"[IncrementalGame] GetPurchaseCount called for '{purchasableId}'. Searching {_runtimeData.Count} runtime data entries...");
        BasePurchasableRuntimeData data = _runtimeData.Find(p => p.purchasableId == purchasableId);

        if (data != null)
        {
            DebugManager.Log($"[IncrementalGame] Found runtime data for '{purchasableId}': purchaseCount={data.purchaseCount}, isActive={data.isActive}");
            return data.purchaseCount;
        }
        else
        {
            DebugManager.Warning($"[IncrementalGame] NO runtime data found for '{purchasableId}'!");
            // Log first few entries to help debug
            for (int i = 0; i < Mathf.Min(5, _runtimeData.Count); i++)
            {
                DebugManager.Log($"[IncrementalGame] Runtime data [{i}]: {_runtimeData[i].purchasableId} (count: {_runtimeData[i].purchaseCount})");
            }
            return 0;
        }
    }

    public List<BasePurchasableRuntimeData> GetAllPurchasables()
    {
        return _runtimeData;
    }

    public List<BasePurchasableRuntimeData> GetActivePurchasables()
    {
        return _runtimeData.Where(p => p.isActive).ToList();
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    #endregion

    #region IStatProvider Implementation

    /// <summary>
    /// Get all stat modifiers for a given stat type
    /// Only StatPurchasables provide modifiers
    /// </summary>
    public List<StatModifier> GetModifiersForStat(StatType statType)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        foreach (var runtimeData in _runtimeData)
        {
            if (!runtimeData.isActive) continue;

            BasePurchasable blueprint = _purchasableRegistry.GetItemById(runtimeData.purchasableId);
            if (blueprint == null) continue;

            // Only StatPurchasables provide modifiers
            if (blueprint is StatPurchasable statPurchasable)
            {
                List<StatModifier> purchasableMods = statPurchasable.GetModifiersAtTier(runtimeData.purchaseCount);

                // Filter for this stat type
                modifiers.AddRange(purchasableMods.Where(m => m.statType == statType));
            }
        }

        return modifiers;
    }

    #endregion

    #region Save Methods

    private void MarkDirty()
    {
        if (_saveProvider != null)
        {
            _saveProvider.MarkDirty();
        }
    }

    public IEnumerator ManualSave()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SetData(_runtimeData);
            yield return _saveProvider.Save();
        }
    }

    public IEnumerator SaveIfDirty()
    {
        if (_saveProvider != null)
        {
            yield return _saveProvider.SaveIfDirty();
        }
    }

    #endregion

    #region Debug Methods

    [Button("Manual Save"), FoldoutGroup("Debug")]
    private void DebugManualSave()
    {
        StartCoroutine(ManualSave());
    }

    [Button("Reset All Purchases"), FoldoutGroup("Debug")]
    private void DebugResetAllPurchases()
    {
        foreach (var purchasable in _runtimeData)
        {
            purchasable.purchaseCount = 0;
            purchasable.isActive = false;
        }

        MarkDirty();
        StartCoroutine(ManualSave());

        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[IncrementalGame] Reset all purchases to 0");
    }

    [Button("Set All to Max Purchases"), FoldoutGroup("Debug")]
    private void DebugMaxAllPurchases()
    {
        foreach (var runtimeData in _runtimeData)
        {
            BasePurchasable blueprint = _purchasableRegistry.GetItemById(runtimeData.purchasableId);
            if (blueprint == null) continue;

            if (blueprint.purchaseType == PurchaseType.Infinite)
            {
                // For infinite, set to a reasonable max (e.g., 100)
                runtimeData.purchaseCount = 100;
            }
            else if (blueprint.purchaseType == PurchaseType.Capped && blueprint.maxPurchases > 0)
            {
                runtimeData.purchaseCount = blueprint.maxPurchases;
            }
            else // OneTime
            {
                runtimeData.purchaseCount = 1;
            }

            runtimeData.isActive = true;
        }

        MarkDirty();
        StartCoroutine(ManualSave());

        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[IncrementalGame] Set all purchases to max");
    }

    #endregion
}
