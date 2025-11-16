using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRuntimeDataStartup : MonoSingleton<PlayerRuntimeDataStartup>, IStatProvider
{
    [SerializeField, FoldoutGroup("Starting Stats")]
    private List<RuntimeStat> _startRuntimeStats = new();

    [SerializeField, FoldoutGroup("Settings")]
    private bool _forceReinitialize = false;

    private EntityRuntimeData _playerData;

    private void Start()
    {
        InitializePlayerIfNeeded();
        RegisterWithStatManager();
    }

    private void OnDestroy()
    {
        UnregisterFromStatManager();
    }

    public void InitializePlayerIfNeeded()
    {
        // Check if player runtime data already exists (would be loaded from save)
        EntityRuntimeData existingData = EntityExtensions.GetEntityById("player");

        if (existingData != null && !_forceReinitialize)
        {
            _playerData = existingData;
            Debug.Log("Player runtime data already exists. Skipping initialization.");
            return;
        }

        // Create new player runtime data
        _playerData = new EntityRuntimeData
        {
            uniqueId = "player",
            blueprintId = "player",
            runtimeStats = new List<RuntimeStat>(_startRuntimeStats), // Copy the list
            currentState = EntityState.Assigned
        };

        EntityManager.Instance.RegisterEntityRuntimeData(_playerData);

        Debug.Log("Initialized player runtime data with starting stats.");
    }

    #region IStatProvider Implementation

    public List<StatModifier> GetModifiersForStat(StatType statType)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        if (_playerData == null)
            return modifiers;

        // Find the stat in player's runtime stats
        RuntimeStat playerStat = _playerData.runtimeStats.Find(s => s.statType == statType);

        if (playerStat != null && !playerStat.currentValue.isZero)
        {
            // Return player's stat as an additive modifier
            modifiers.Add(new StatModifier(
                statType,
                playerStat.currentValue,
                "player_base"
            ));
        }

        return modifiers;
    }

    #endregion

    #region StatManager Registration

    private void RegisterWithStatManager()
    {
        if (StatManager.Instance != null)
        {
            StatManager.Instance.RegisterStatProvider(this);
            Debug.Log("[PlayerRuntimeDataStartup] Registered with StatManager");
        }
    }

    private void UnregisterFromStatManager()
    {
        if (StatManager.Instance != null)
        {
            StatManager.Instance.UnregisterStatProvider(this);
        }
    }

    #endregion

    #region Debug Helpers

    [Button("Reset Player Data"), FoldoutGroup("Debug")]
    private void DebugResetPlayerData()
    {
        // Remove existing if present
        EntityRuntimeData existing = EntityExtensions.GetEntityById("player");
        if (existing != null)
        {
            EntityManager.Instance.UnregisterEntityRuntimeData(existing);
        }

        // Re-initialize
        _forceReinitialize = true;
        InitializePlayerIfNeeded();
        _forceReinitialize = false;

        // Trigger recalculation
        StatManager.Instance.RecalculateAllStats();
    }

    [Button("Log Player Stats"), FoldoutGroup("Debug")]
    private void DebugLogPlayerStats()
    {
        if (_playerData == null)
        {
            Debug.LogWarning("Player data not initialized");
            return;
        }

        Debug.Log($"=== PLAYER STATS ===");
        foreach (var stat in _playerData.runtimeStats)
        {
            Debug.Log($"{stat.statType}: {stat.currentValue}");
        }
    }

    #endregion
}