using LGD.Core.Events;
using LGD.Core.Singleton;
using LGD.PickupSystem;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager for the bark system.
/// - Listens to game events (pickup, drop, spawn)
/// - Maps zone IDs to bark contexts
/// - Requests barks from CentralizedBarkProvider
/// - Publishes bark events that CultistBarkDisplay responds to
/// </summary>
public class BarkManager : MonoSingleton<BarkManager>
{
    [SerializeField, FoldoutGroup("Settings")]
    private bool _enableDebugLogs = true;

    [SerializeField, FoldoutGroup("References")]
    private CentralizedBarkProvider _barkProvider;

    [SerializeField, FoldoutGroup("Zone Mapping")]
    [InfoBox("Map zone ID keywords to bark contexts. Used for both reactive and ambient barks.")]
    private List<ZoneBarkMapping> _zoneMappings = new List<ZoneBarkMapping>()
    {
        new ZoneBarkMapping("devotion", BarkContext.AssignedToDevotion, BarkContext.DevotingAmbient),
        new ZoneBarkMapping("life", BarkContext.AssignedToLifeCrystal, BarkContext.LifeCrystalAmbient),
        new ZoneBarkMapping("work", BarkContext.AssignedToWork, BarkContext.WorkAmbient),
    };

    protected override void Awake()
    {
        base.Awake();

        if (_barkProvider == null)
        {
            _barkProvider = GetComponent<CentralizedBarkProvider>();
        }
    }

    #region Bark Retrieval

    /// <summary>
    /// Get a random bark for the given context.
    /// </summary>
    public string GetRandomBark(BarkContext context)
    {
        if (_barkProvider == null)
        {
            return "";
        }

        List<string> barks = _barkProvider.GetBarksForContext(context);

        if (barks.Count == 0)
        {
            return "";
        }

        return barks[Random.Range(0, barks.Count)];
    }

    /// <summary>
    /// Request a bark for a specific entity and context.
    /// Publishes ON_BARK_REQUESTED event with entity ID.
    /// </summary>
    public void RequestBark(EntityRuntimeData entity, BarkContext context)
    {
        if (entity == null)
            return;

        string bark = GetRandomBark(context);

        if (string.IsNullOrEmpty(bark))
            return;

        // Publish event - CultistBarkDisplay will catch this
        Publish(BarkEventIds.ON_BARK_REQUESTED, entity, bark);
    }

    /// <summary>
    /// Overload that takes entity ID directly.
    /// </summary>
    public void RequestBark(string entityId, BarkContext context)
    {
        EntityRuntimeData entity = EntityManager.Instance.GetEntityRuntimeDataById(entityId);
        if (entity != null)
        {
            RequestBark(entity, context);
        }
    }

    #endregion

    #region Zone ID Mapping

    /// <summary>
    /// Map a zone ID to a REACTIVE bark context (happens once on assignment).
    /// Uses keyword matching from _zoneMappings list.
    /// </summary>
    public BarkContext GetReactiveBarkContextForZone(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId))
            return BarkContext.IdleAmbient;

        // Check mappings for keyword match
        foreach (var mapping in _zoneMappings)
        {
            if (zoneId.ToLower().Contains(mapping.zoneKeyword.ToLower()))
            {
                DebugLog($"Mapped zone '{zoneId}' to reactive context: {mapping.reactiveBarkContext}");
                return mapping.reactiveBarkContext;
            }
        }

        // Default fallback
        DebugLog($"No mapping found for zone '{zoneId}', using IdleAmbient");
        return BarkContext.IdleAmbient;
    }

    /// <summary>
    /// Map a zone ID to an AMBIENT bark context (happens periodically while assigned).
    /// Uses keyword matching from _zoneMappings list.
    /// </summary>
    public BarkContext GetAmbientBarkContextForZone(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId))
            return BarkContext.IdleAmbient;

        // Check mappings for keyword match
        foreach (var mapping in _zoneMappings)
        {
            if (zoneId.ToLower().Contains(mapping.zoneKeyword.ToLower()))
            {
                DebugLog($"Mapped zone '{zoneId}' to ambient context: {mapping.ambientBarkContext}");
                return mapping.ambientBarkContext;
            }
        }

        // Default fallback
        return BarkContext.IdleAmbient;
    }

    #endregion

    #region Event Listeners - AUTO-TRIGGER REACTIVE BARKS

    [Topic(PickupEventIds.ON_ENTITY_PICKED_UP)]
    public void OnEntityPickedUp(object sender, EntityPickup pickup)
    {
        RequestBark(pickup.GetRuntimeData(), BarkContext.PickedUp);
    }

    // Listen to BOTH assignment and reconnection events
    [Topic(PickupEventIds.ON_ENTITY_ASSIGNED_TO_ZONE)]
    [Topic(PickupEventIds.ON_ENTITY_RECONNECTED_TO_ZONE)]
    public void OnEntityAssignedToZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        // Map zone ID to appropriate reactive bark
        BarkContext context = GetReactiveBarkContextForZone(zoneId);
        RequestBark(runtimeData, context);
    }

    [Topic(EntityEventIds.ON_ENTITY_SPAWNED)]
    public void OnEntitySpawned(object sender, EntityRuntimeData runtimeData, bool fromLoading)
    {
        RequestBark(runtimeData, BarkContext.Spawned);
    }

    #endregion

    #region Helpers

    private void DebugLog(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[BarkManager] {message}");
        }
    }

    #endregion

    #region Debug Helpers

    [Button("Test Random Bark"), FoldoutGroup("Debug")]
    private void DebugTestRandomBark(BarkContext context)
    {
        string bark = GetRandomBark(context);
        Debug.Log($"Random bark for {context}: \"{bark}\"");
    }

    [Button("Test Zone Mapping"), FoldoutGroup("Debug")]
    private void DebugTestZoneMapping(string testZoneId)
    {
        BarkContext reactive = GetReactiveBarkContextForZone(testZoneId);
        BarkContext ambient = GetAmbientBarkContextForZone(testZoneId);

        Debug.Log($"=== ZONE MAPPING TEST ===");
        Debug.Log($"Zone ID: {testZoneId}");
        Debug.Log($"Reactive Context: {reactive}");
        Debug.Log($"Ambient Context: {ambient}");
    }

    #endregion
}