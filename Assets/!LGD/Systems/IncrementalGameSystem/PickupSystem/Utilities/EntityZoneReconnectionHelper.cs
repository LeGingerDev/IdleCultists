using LGD.PickupSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper for reconnecting entities to zones during save/load restoration.
/// Called after entities are spawned but before gameplay resumes.
/// </summary>
public static class EntityZoneReconnectionHelper
{
    /// <summary>
    /// Reconnect all assigned entities to their zones.
    /// Call this after EntityManager.RestoreEntities() completes.
    /// </summary>
    public static IEnumerator ReconnectEntitiesToZones()
    {
        DebugManager.Log("[IncrementalGame] <color=yellow>Starting zone reconnection...</color>");

        // Get all entities that were assigned to zones when saved
        List<EntityRuntimeData> assignedEntities = EntityManager.Instance.GetEntitiesByState(EntityState.Assigned);

        if (assignedEntities.Count == 0)
        {
            DebugManager.Log("[IncrementalGame] <color=cyan>No entities to reconnect</color>");
            yield break;
        }

        int reconnectedCount = 0;
        int failedCount = 0;

        foreach (EntityRuntimeData runtimeData in assignedEntities)
        {
            // Find the zone this entity was assigned to
            IDropZone zone = FindZoneById(runtimeData.currentZoneId);

                if (zone == null)
                {
                    DebugManager.Warning($"[IncrementalGame] Zone '{runtimeData.currentZoneId}' not found for entity {runtimeData.entityName} ({runtimeData.uniqueId})");
                    failedCount++;
                    continue;
                }

            // Find the EntityPickup component for this entity
            EntityPickup pickup = FindEntityPickupById(runtimeData.uniqueId);

                if (pickup == null)
                {
                    DebugManager.Warning($"[IncrementalGame] EntityPickup not found for entity {runtimeData.entityName} ({runtimeData.uniqueId})");
                    failedCount++;
                    continue;
                }

            // Reconnect: update internal state without firing events
            pickup.ReconnectToZone(zone);
            zone.OnEntityReconnected(pickup);

            reconnectedCount++;

            // Small yield every 10 entities to prevent frame hitches
            if (reconnectedCount % 10 == 0)
            {
                yield return null;
            }
        }

        // Trigger a single stat recalculation after all reconnections
        if (reconnectedCount > 0 && StatManager.Instance != null)
        {
            StatManager.Instance.RecalculateAllStats();
        }

        DebugManager.Log($"[IncrementalGame] <color=green>Zone reconnection complete:</color> {reconnectedCount} entities reconnected, {failedCount} failed");
    }

    /// <summary>
    /// Find a zone by its ID in the scene.
    /// </summary>
    private static IDropZone FindZoneById(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId))
            return null;

        // Find all IDropZone components in the scene
        DropZoneBase[] allZones = Object.FindObjectsByType<DropZoneBase>(FindObjectsSortMode.None);

        foreach (DropZoneBase zone in allZones)
        {
            if (zone.GetZoneId() == zoneId)
            {
                return zone;
            }
        }

        return null;
    }

    /// <summary>
    /// Find an EntityPickup by its runtime data unique ID.
    /// </summary>
    private static EntityPickup FindEntityPickupById(string entityId)
    {
        if (string.IsNullOrEmpty(entityId))
            return null;

        EntityPickup[] allPickups = Object.FindObjectsByType<EntityPickup>(FindObjectsSortMode.None);

        foreach (EntityPickup pickup in allPickups)
        {
            if (pickup.GetIdOfEntity() == entityId)
            {
                return pickup;
            }
        }

        return null;
    }
}