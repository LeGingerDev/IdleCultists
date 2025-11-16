using LargeNumbers;
using LGD.Core.Events;
using LGD.PickupSystem;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Devotion zone behavior - actively calculates devotion contribution from assigned entities.
/// Queries EntityManager for assigned entities and sums their DevotionPerSecond stats.
/// Implements IStatProvider to feed into StatManager's calculation.
/// </summary>
public class DevotionZoneBehavior : ZoneBehaviorBase
{
    [SerializeField, FoldoutGroup("Visual Effects")]
    private ParticleSystem[] _prayerEffects;

    [SerializeField, FoldoutGroup("Audio")]
    private AudioClip _prayerAmbience;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private AlphabeticNotation _currentDevotionContribution;

    #region IStatProvider Implementation

    public override List<StatModifier> GetModifiersForStat(StatType statType)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        // Only contribute to DevotionPerSecond
        if (statType != StatType.DevotionPerSecond)
            return modifiers;

        // Get all entities assigned to this zone
        List<EntityRuntimeData> assignedEntities = GetAssignedEntities();

        if (assignedEntities.Count == 0)
            return modifiers;

        // Sum up devotion from all assigned entities
        AlphabeticNotation totalDevotion = AlphabeticNotation.zero;

        foreach (var entity in assignedEntities)
        {
            AlphabeticNotation entityDevotion = entity.GetStatValue(StatType.DevotionPerSecond);
            totalDevotion += entityDevotion;
        }

        // Cache for debug display
        _currentDevotionContribution = totalDevotion;

        // Return as an additive modifier
        if (!totalDevotion.isZero)
        {
            modifiers.Add(new StatModifier(
                StatType.DevotionPerSecond,
                totalDevotion,
                $"devotion_zone_{_zoneId}"
            ));
        }

        return modifiers;
    }

    #endregion

    #region Topic Listeners

    [Topic(PickupEventIds.ON_ENTITY_ASSIGNED_TO_ZONE)]
    public void OnEntityAssignedToZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        // Only respond to assignments to THIS zone
        if (zoneId != _zoneId)
            return;

        Debug.Log($"[DevotionZone:{_zoneId}] Entity {runtimeData.entityName} started praying");

        // Start visual effects when first entity assigned
        if (GetAssignedEntityCount() == 1)
        {
            ActivatePrayerEffects();
        }
    }

    [Topic(PickupEventIds.ON_ENTITY_REMOVED_FROM_ZONE)]
    public void OnEntityRemovedFromZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        // Only respond to removals from THIS zone
        if (zoneId != _zoneId)
            return;

        Debug.Log($"[DevotionZone:{_zoneId}] Entity {runtimeData.entityName} stopped praying");

        // Stop visual effects when zone becomes empty
        if (GetAssignedEntityCount() == 0)
        {
            DeactivatePrayerEffects();
        }
    }

    #endregion

    #region Visual Effects

    private void ActivatePrayerEffects()
    {
        if (_prayerEffects == null || _prayerEffects.Length == 0)
            return;

        foreach (ParticleSystem effect in _prayerEffects)
        {
            if (effect != null && !effect.isPlaying)
            {
                effect.Play();
            }
        }

        Debug.Log($"[DevotionZone:{_zoneId}] Prayer effects activated");
    }

    private void DeactivatePrayerEffects()
    {
        if (_prayerEffects == null || _prayerEffects.Length == 0)
            return;

        foreach (ParticleSystem effect in _prayerEffects)
        {
            if (effect != null && effect.isPlaying)
            {
                effect.Stop();
            }
        }

        Debug.Log($"[DevotionZone:{_zoneId}] Prayer effects deactivated");
    }

    #endregion

    #region Debug Helpers

    [Button("Calculate Current Contribution"), FoldoutGroup("Debug")]
    private void DebugCalculateContribution()
    {
        var modifiers = GetModifiersForStat(StatType.DevotionPerSecond);

        Debug.Log($"=== DEVOTION ZONE {_zoneId} CONTRIBUTION ===");
        Debug.Log($"Assigned Entities: {GetAssignedEntityCount()}");

        if (modifiers.Count > 0)
        {
            Debug.Log($"Total Devotion Per Second: {modifiers[0].additiveValue}");
        }
        else
        {
            Debug.Log("No contribution (no entities assigned)");
        }

        var entities = GetAssignedEntities();
        foreach (var entity in entities)
        {
            var entityDevotion = entity.GetStatValue(StatType.DevotionPerSecond);
            Debug.Log($"  - {entity.entityName}: {entityDevotion}");
        }
    }

    #endregion
}