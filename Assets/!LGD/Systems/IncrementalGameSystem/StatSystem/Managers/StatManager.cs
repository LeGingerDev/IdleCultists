using LargeNumbers;
using LGD.Core.Events;
using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;
using UnityEngine;

public class StatManager : MonoSingleton<StatManager>
{
    [SerializeField, FoldoutGroup("Debug/Cached Stats")]
    private Dictionary<StatType, AlphabeticNotation> _cachedStats = new Dictionary<StatType, AlphabeticNotation>();

    [SerializeField, ReadOnly, FoldoutGroup("Debug/Info")]
    private int _recalculationCount = 0;

    [SerializeField, ReadOnly, FoldoutGroup("Debug/Info")]
    private int _registeredProviderCount = 0;

    private List<IStatProvider> _statProviders = new List<IStatProvider>();

    protected override void Awake()
    {
        base.Awake();
        InitializeCachedStats();
    }

    private void Start()
    {
        RegisterStaticProviders();
    }

    private void InitializeCachedStats()
    {
        if (_cachedStats == null)
        {
            _cachedStats = new Dictionary<StatType, AlphabeticNotation>();
            DebugManager.Warning("[IncrementalGame] [StatManager] _cachedStats was null, recreated it");
        }

        // Initialize all stat types to 0
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            _cachedStats[statType] = AlphabeticNotation.zero;
        }
    }

    private void RegisterStaticProviders()
    {
        // Register purchasable manager (provides stat modifiers from StatPurchasables)
        if (PurchasableManager.Instance != null)
        {
            RegisterStatProvider(PurchasableManager.Instance);
        }

        // NOTE: EntityManager is NOT registered - it doesn't provide stats anymore!
        // Zone behaviors will register themselves dynamically

        DebugManager.Log($"[IncrementalGame] [StatManager] Static providers registered. Total: {_statProviders.Count}");
    }

    #region Dynamic Provider Registration

    /// <summary>
    /// Register a stat provider dynamically (used by zone behaviors).
    /// </summary>
    public void RegisterStatProvider(IStatProvider provider)
    {
        if (provider == null)
        {
            DebugManager.Warning("[IncrementalGame] [StatManager] Attempted to register null provider");
            return;
        }

        if (_statProviders.Contains(provider))
        {
            DebugManager.Warning($"[IncrementalGame] [StatManager] Provider already registered: {provider.GetType().Name}");
            return;
        }

        _statProviders.Add(provider);
        _registeredProviderCount = _statProviders.Count;

        DebugManager.Log($"[IncrementalGame] [StatManager] Registered provider: {provider.GetType().Name} (Total: {_registeredProviderCount})");

        // Recalculate stats when a new provider is registered
        RecalculateAllStats();
    }

    /// <summary>
    /// Unregister a stat provider (used when zone behaviors are destroyed).
    /// </summary>
    public void UnregisterStatProvider(IStatProvider provider)
    {
        if (provider == null)
            return;

        if (_statProviders.Remove(provider))
        {
            _registeredProviderCount = _statProviders.Count;
            DebugManager.Log($"[IncrementalGame] [StatManager] Unregistered provider: {provider.GetType().Name} (Total: {_registeredProviderCount})");

            // Recalculate stats when a provider is removed
            RecalculateAllStats();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get a detailed breakdown of how a stat is calculated
    /// </summary>
    public StatBreakdown GetStatBreakdown(StatType statType)
    {
        // Base value starts at zero - providers add to it
        AlphabeticNotation baseValue = AlphabeticNotation.zero;

        // Gather all modifiers from stat providers
        AlphabeticNotation additiveTotal = AlphabeticNotation.zero;
        float multiplicativeTotal = 0f;

        foreach (var provider in _statProviders)
        {
            List<StatModifier> modifiers = provider.GetModifiersForStat(statType);

            foreach (var modifier in modifiers)
            {
                if (modifier.modifierType == ModifierType.Additive)
                    additiveTotal += modifier.additiveValue;
                else if (modifier.modifierType == ModifierType.Multiplicative)
                    multiplicativeTotal += modifier.multiplicativeValue;
            }
        }

        // Calculate final value
        AlphabeticNotation additiveSum = baseValue + additiveTotal;
        AlphabeticNotation finalValue = additiveSum * (1f + multiplicativeTotal);

        return new StatBreakdown(statType, baseValue, additiveTotal, multiplicativeTotal, finalValue);
    }

    /// <summary>
    /// Get the current value of a stat (returns cached value)
    /// </summary>
    public AlphabeticNotation QueryStat(StatType statType)
    {
        if (_cachedStats == null)
        {
            DebugManager.Error("[IncrementalGame] [StatManager] _cachedStats is null! Reinitializing...");
            InitializeCachedStats();
        }

        if (_cachedStats.TryGetValue(statType, out AlphabeticNotation value))
            return value;

        DebugManager.Warning($"[IncrementalGame] [StatManager] Stat {statType} not found in cache. Recalculating.");
        RecalculateAllStats();
        return _cachedStats[statType];
    }

    public AlphabeticNotation QueryStatWithBase(StatType statType, AlphabeticNotation baseValue)
    {
        Stat stat = new Stat();
        stat.SetBaseValue(baseValue);

        // Gather modifiers from all providers
        foreach (var provider in _statProviders)
        {
            List<StatModifier> modifiers = provider.GetModifiersForStat(statType);
            foreach (var modifier in modifiers)
            {
                stat.AddModifier(modifier);
            }
        }

        stat.Calculate();
        return stat.FinalValue;
    }

    /// <summary>
    /// Recalculate all stats from scratch
    /// </summary>
    public void RecalculateAllStats()
    {
        _recalculationCount++;

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            _cachedStats[statType] = CalculateStat(statType);
        }

        Publish(StatEventIds.ON_STATS_RECALCULATED);
        DebugManager.Log($"[IncrementalGame] [StatManager] Recalculated all stats (Count: {_recalculationCount}, Providers: {_registeredProviderCount})");
    }

    #endregion

    #region Calculation Logic

    private AlphabeticNotation CalculateStat(StatType statType)
    {
        Stat stat = new Stat();

        // Base value starts at zero
        stat.SetBaseValue(AlphabeticNotation.zero);

        // Gather modifiers from ALL stat providers (zones, upgrades, etc.)
        int totalModifiersFound = 0;
        foreach (var provider in _statProviders)
        {
            List<StatModifier> modifiers = provider.GetModifiersForStat(statType);
            totalModifiersFound += modifiers.Count;

            foreach (var modifier in modifiers)
            {
                stat.AddModifier(modifier);
            }
        }

        // Calculate final value
        stat.Calculate();

        return stat.FinalValue;
    }

    #endregion

    #region Topic Listeners

    [Topic(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED)]
    public void OnStatsRecalculationRequested(object sender)
    {
        RecalculateAllStats();
    }

    #endregion

    #region Debug Helpers

    [Button("Force Recalculate All Stats"), FoldoutGroup("Debug")]
    private void DebugRecalculate()
    {
        DebugManager.Log("[IncrementalGame] [StatManager] === FORCE RECALCULATE STATS ===");
        DebugManager.Log($"[IncrementalGame] [StatManager] Current stat provider count: {_statProviders.Count}");

        foreach (var provider in _statProviders)
        {
            DebugManager.Log($"[IncrementalGame] [StatManager]   - {provider.GetType().Name}");
        }

        RecalculateAllStats();

        DebugManager.Log($"[IncrementalGame] [StatManager] === CACHED STATS AFTER RECALCULATION ===");
        foreach (var kvp in _cachedStats)
        {
            DebugManager.Log($"[IncrementalGame] [StatManager] {kvp.Key}: {kvp.Value}");
        }
    }

    [Button("List All Registered Providers"), FoldoutGroup("Debug")]
    private void DebugListProviders()
    {
        DebugManager.Log($"[IncrementalGame] [StatManager] === REGISTERED STAT PROVIDERS ({_statProviders.Count}) ===");
        foreach (var provider in _statProviders)
        {
            DebugManager.Log($"[IncrementalGame] [StatManager]   - {provider.GetType().Name}");
        }
    }

    #endregion
}