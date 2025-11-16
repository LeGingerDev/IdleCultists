using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PurchaseManager : MonoSingleton<PurchaseManager>, IStatProvider
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private List<PurchasableRuntimeData> _runtimeData = new List<PurchasableRuntimeData>();

    [SerializeField, FoldoutGroup("Settings")]
    private bool _autoSave = true;

    private PurchasableRegistry _purchasableRegistry;
    private SaveLoadProviderBase<PurchasableRuntimeData> _saveProvider;
    private bool _isInitialized = false;

    #region Initialization

    private void Start()
    {
        StartCoroutine(InitializePurchasables());
    }

    private IEnumerator InitializePurchasables()
    {
        _purchasableRegistry = RegistryManager.Instance.GetRegistry<PurchasableBlueprint>() as PurchasableRegistry;
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<PurchasableRuntimeData>();

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
            Debug.Log($"<color=cyan>Purchase Manager initialized:</color> {_runtimeData.Count} purchasables loaded");
        }
        else
        {
            Debug.LogError("Purchasable save provider not found! Make sure PurchasableSaveProvider is in the scene.");
        }

        _isInitialized = true;
    }

    #endregion

    #region Purchase Management

    public bool ExecutePurchase(string purchasableId)
    {
        PurchasableBlueprint blueprint = _purchasableRegistry.GetItemById(purchasableId);
        if (blueprint == null)
        {
            Debug.LogError($"Purchasable blueprint not found: {purchasableId}");
            return false;
        }

        // Get runtime data
        PurchasableRuntimeData runtimeData = _runtimeData.Find(p => p.purchasableId == purchasableId);
        if (runtimeData == null)
        {
            Debug.LogError($"Purchasable runtime data not found: {purchasableId}");
            return false;
        }

        // Execute the purchase (blueprint handles specific logic)
        blueprint.HandlePurchase(runtimeData);

        // Increment purchase count
        runtimeData.IncrementPurchase();

        // Mark save provider as dirty
        MarkDirty();

        Debug.Log($"Executed purchase: {blueprint.displayName} (Times purchased: {runtimeData.timesPurchased})");
        return true;
    }

    #endregion

    #region Getters

    public PurchasableRuntimeData GetPurchasableRuntimeData(string purchasableId)
    {
        return _runtimeData.Find(p => p.purchasableId == purchasableId);
    }

    public int GetTimesPurchased(string purchasableId)
    {
        PurchasableRuntimeData data = _runtimeData.Find(p => p.purchasableId == purchasableId);
        return data != null ? data.timesPurchased : 0;
    }

    public List<PurchasableRuntimeData> GetAllPurchasables()
    {
        return _runtimeData;
    }

    public List<PurchasableRuntimeData> GetPurchasedItems()
    {
        return _runtimeData.Where(p => p.timesPurchased > 0).ToList();
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    #endregion

    #region IStatProvider Implementation

    public List<StatModifier> GetModifiersForStat(StatType statType)
    {
        // MOST purchasables don't provide modifiers
        // But SOME might (like permanent ritual buffs)
        // For now, return empty - implement if needed later
        return new List<StatModifier>();
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
            purchasable.timesPurchased = 0;
        }

        MarkDirty();
        StartCoroutine(ManualSave());
        Debug.Log("Reset all purchases to 0");
    }

    [Button("Add 10 Purchases to All"), FoldoutGroup("Debug")]
    private void DebugAdd10ToAll()
    {
        foreach (var purchasable in _runtimeData)
        {
            purchasable.timesPurchased += 10;
        }

        MarkDirty();
        StartCoroutine(ManualSave());
        Debug.Log("Added 10 purchases to all purchasables");
    }

    #endregion
}