using LGD.Core.Events;
using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeManager : MonoSingleton<UpgradeManager>, IStatProvider
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private List<UpgradeRuntimeData> _runtimeData = new List<UpgradeRuntimeData>();

    [SerializeField, FoldoutGroup("Settings")]
    private bool _autoSave = true;

    private UpgradeRegistry _upgradeRegistry;
    private SaveLoadProviderBase<UpgradeRuntimeData> _saveProvider;
    private bool _isInitialized = false;

    #region Initialization

    private void Start()
    {
        StartCoroutine(InitializeUpgrades());
    }

    private IEnumerator InitializeUpgrades()
    {
        _upgradeRegistry = RegistryManager.Instance.GetRegistry<UpgradeBlueprint>() as UpgradeRegistry;
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<UpgradeRuntimeData>();

        if (_saveProvider != null)
        {
            yield return _saveProvider.Load();

            // Sync with registry to add any new upgrades that were added after the save file was created
            UpgradeSaveProvider upgradeProvider = _saveProvider as UpgradeSaveProvider;
            if (upgradeProvider != null)
            {
                yield return upgradeProvider.SyncWithRegistry();
            }

            _runtimeData = _saveProvider.GetData();
            DebugManager.Log($"[IncrementalGame] <color=cyan>Upgrade Manager initialized:</color> {_runtimeData.Count} upgrades loaded");
        }
        else
        {
            DebugManager.Error("[IncrementalGame] Upgrade save provider not found! Make sure UpgradeSaveProvider is in the scene.");
        }

        _isInitialized = true;
    }

    #endregion

    #region Purchase Management

    public bool PurchaseUpgradeTier(string upgradeId)
    {
        UpgradeBlueprint blueprint = _upgradeRegistry.GetItemById(upgradeId);
        if (blueprint == null)
        {
            DebugManager.Error($"[IncrementalGame] Upgrade blueprint not found: {upgradeId}");
            return false;
        }

        // Get runtime data
        UpgradeRuntimeData runtimeData = _runtimeData.Find(u => u.upgradeId == upgradeId);
        if (runtimeData == null)
        {
            DebugManager.Error($"[IncrementalGame] Upgrade runtime data not found: {upgradeId}");
            return false;
        }

        // Check if can upgrade
        if (blueprint.upgradeType != UpgradeType.Infinite &&
            blueprint.maxTier != -1 &&
            runtimeData.currentTier >= blueprint.maxTier)
        {
            DebugManager.Warning($"[IncrementalGame] Upgrade {upgradeId} is already at max tier");
            return false;
        }

        // Purchase the tier
        runtimeData.PurchaseTier();

        // Mark save provider as dirty
        MarkDirty();

        // Recalculate stats
        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);

        DebugManager.Log($"[IncrementalGame] Purchased {blueprint.displayName} Tier {runtimeData.currentTier}");
        return true;
    }

    #endregion

    #region Getters

    public UpgradeRuntimeData GetUpgradeRuntimeData(string upgradeId)
    {
        return _runtimeData.Find(u => u.upgradeId == upgradeId);
    }

    public int GetUpgradeTier(string upgradeId)
    {
        UpgradeRuntimeData data = _runtimeData.Find(u => u.upgradeId == upgradeId);
        return data != null ? data.currentTier : 0;
    }

    public List<UpgradeRuntimeData> GetAllUpgrades()
    {
        return _runtimeData;
    }

    public List<UpgradeRuntimeData> GetActiveUpgrades()
    {
        return _runtimeData.Where(u => u.isActive).ToList();
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    #endregion

    #region IStatProvider Implementation

    public List<StatModifier> GetModifiersForStat(StatType statType)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        foreach (var runtimeData in _runtimeData)
        {
            if (!runtimeData.isActive) continue;

            UpgradeBlueprint blueprint = _upgradeRegistry.GetItemById(runtimeData.upgradeId);
            if (blueprint == null) continue;

            List<StatModifier> upgradeMods = blueprint.GetModifiersAtTier(runtimeData.currentTier);

            // Filter for this stat type
            modifiers.AddRange(upgradeMods.Where(m => m.statType == statType));
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

    [Button("Reset All Upgrades"), FoldoutGroup("Debug")]
    private void DebugResetAllUpgrades()
    {
        foreach (var upgrade in _runtimeData)
        {
            upgrade.currentTier = 0;
            upgrade.isActive = false;
        }

        MarkDirty();
        StartCoroutine(ManualSave());

        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[IncrementalGame] Reset all upgrades to tier 0");
    }

    [Button("Set All Upgrades to Max"), FoldoutGroup("Debug")]
    private void DebugMaxAllUpgrades()
    {
        foreach (var runtimeData in _runtimeData)
        {
            UpgradeBlueprint blueprint = _upgradeRegistry.GetItemById(runtimeData.upgradeId);
            if (blueprint == null) continue;

            if (blueprint.upgradeType == UpgradeType.Infinite)
            {
                // For infinite upgrades, set to a reasonable max (e.g., 100)
                runtimeData.currentTier = 100;
            }
            else if (blueprint.maxTier > 0)
            {
                runtimeData.currentTier = blueprint.maxTier;
            }
            else
            {
                runtimeData.currentTier = 1;
            }

            runtimeData.isActive = true;
        }

        MarkDirty();
        StartCoroutine(ManualSave());

        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[IncrementalGame] Set all upgrades to max tier");
    }

    #endregion
}