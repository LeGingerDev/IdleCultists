using System.Collections.Generic;
using UnityEngine;

namespace LGD.PickupSystem
{
    public static class PickupExtensions
    {
        /// <summary>
        /// Checks if a zone type flag contains a specific zone type
        /// </summary>
        public static bool HasZoneType(this DropZoneType flags, DropZoneType zoneType)
        {
            return (flags & zoneType) == zoneType;
        }

        /// <summary>
        /// Gets all EntityPickup components within a zone
        /// </summary>
        public static List<EntityPickup> GetEntitiesInZone(this IDropZone zone)
        {
            List<EntityPickup> entities = new List<EntityPickup>();
            List<string> entityIds = zone.GetAssignedEntityIds();

            foreach (string entityId in entityIds)
            {
                EntityRuntimeData runtimeData = EntityManager.Instance.GetEntityRuntimeDataById(entityId);
                if (runtimeData != null)
                {
                    EntityPickup pickup = FindEntityPickupById(entityId);
                    if (pickup != null)
                    {
                        entities.Add(pickup);
                    }
                }
            }

            return entities;
        }

        /// <summary>
        /// Checks if a zone is at capacity
        /// </summary>
        public static bool IsAtCapacity(this IDropZone zone)
        {
            return zone.GetCurrentCapacity() >= zone.GetMaxCapacity();
        }

        /// <summary>
        /// Gets the remaining capacity of a zone
        /// </summary>
        public static int GetRemainingCapacity(this IDropZone zone)
        {
            return zone.GetMaxCapacity() - zone.GetCurrentCapacity();
        }

        /// <summary>
        /// Gets the remaining capacity of a zone (alias for GetRemainingCapacity)
        /// </summary>
        public static int GetAvailableCapacity(this IDropZone zone)
        {
            return zone.GetMaxCapacity() - zone.GetCurrentCapacity();
        }

        /// <summary>
        /// Tries to find an EntityPickup by runtime data ID
        /// </summary>
        private static EntityPickup FindEntityPickupById(string entityId)
        {
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

        /// <summary>
        /// Checks if an entity's state allows stat contribution
        /// </summary>
        public static bool IsContributingStats(this EntityPickup entity)
        {
            EntityRuntimeData runtimeData = entity.GetRuntimeData();
            return runtimeData != null && runtimeData.CanContributeStats();
        }

        /// <summary>
        /// Gets the zone an entity is currently assigned to
        /// </summary>
        public static IDropZone GetAssignedZone(this EntityPickup entity)
        {
            return entity.GetCurrentZone();
        }
    }
}