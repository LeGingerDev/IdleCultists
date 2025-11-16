using LGD.Core;
using LGD.Core.Events;
using LGD.InteractionSystem;
using LGD.PickupSystem;
using UnityEngine;

public class CultistAnimator : BaseBehaviour
{
    private const string IDLE_ANIM = "Idle";
    private const string DEVOTION_ANIM = "Devoting";
    private const string FALLING_ANIM = "Falling";
    private const string FLAILING_ANIM = "Pickup";
    private const string CLIMBING_ANIM = "Climbing";
    private const string OOGA_BOOGA_ANIM = "OogaBooga";

    private EntityController _entityController;
    private Animator _animator;

    private void Awake()
    {
        _entityController = GetComponent<EntityController>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        PlayAnimation(IDLE_ANIM);
    }

    // Listen to BOTH assignment and reconnection events
    [Topic(PickupEventIds.ON_ENTITY_ASSIGNED_TO_ZONE)]
    [Topic(PickupEventIds.ON_ENTITY_RECONNECTED_TO_ZONE)]
    public void OnAssignedToZone(object sender, EntityRuntimeData data, string zoneId)
    {
        Debug.Log($"CultistAnimator: OnAssignedToZone called for entity {data.uniqueId} to zone {zoneId}");
        if (data.uniqueId != _entityController.RuntimeData.uniqueId) return;
        if (zoneId.ToLower().Contains("devotion"))
            PlayAnimation(DEVOTION_ANIM);
        if (zoneId.ToLower().Contains("life"))
            PlayAnimation(OOGA_BOOGA_ANIM);
    }

    [Topic(PickupEventIds.ON_ENTITY_PICKED_UP)]
    public void OnPickedUp(object sender, EntityPickup entity)
    {
        if (entity.GetRuntimeData().uniqueId != _entityController.RuntimeData.uniqueId) return;
        PlayAnimation(FLAILING_ANIM);
    }

    [Topic(PickupEventIds.ON_ENTITY_INVALID_DROP)]
    public void OnInvalidDrop(object sender, EntityPickup entity)
    {
        if (entity.GetRuntimeData().uniqueId != _entityController.RuntimeData.uniqueId) return;
        PlayAnimation(IDLE_ANIM);
    }

    [Topic(PitOfLifeEventIds.ON_ENTITY_CLIMBING)]
    public void OnClimbing(object sender, EntityRuntimeData data)
    {
        if (data.uniqueId != _entityController.RuntimeData.uniqueId) return;
        PlayAnimation(CLIMBING_ANIM);
    }

    [Topic(PitOfLifeEventIds.ON_ENTITY_EXITED)]
    [Topic(PitOfLifeEventIds.ON_ENTITY_START_EXIT)]
    public void OnExitedPit(object sender, EntityRuntimeData data)
    {
        if (data.uniqueId != _entityController.RuntimeData.uniqueId) return;
        PlayAnimation(IDLE_ANIM);
    }

    public void PlayAnimation(string animationName)
    {
        if (_animator == null) return;
        _animator.Play(animationName);
    }
}