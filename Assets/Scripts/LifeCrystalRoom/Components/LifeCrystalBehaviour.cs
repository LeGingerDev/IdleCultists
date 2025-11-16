using DG.Tweening;
using LargeNumbers;
using LGD.Core.Events;
using LGD.PickupSystem;
using LGD.ResourceSystem;
using LGD.ResourceSystem.Models;
using LGD.Utilities.Extensions;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Cinemachine.CinemachineFreeLookModifier;

public class LifeCrystalBehaviour : ZoneBehaviorBase
{
    [SerializeField, FoldoutGroup("Resources")]
    private Resource _resourceToProvide;
    [SerializeField, FoldoutGroup("Resources"), MinMaxSlider(-20f, 20f)]
    private Vector2 _minMaxX;
    [SerializeField, FoldoutGroup("Resources"), MinMaxSlider(0, 20f)]
    private Vector2 _minMaxY;
    [SerializeField, FoldoutGroup("Resources")]
    private Transform _spawnPosition;
    [SerializeField, FoldoutGroup("Resources")]
    private Ease _resourceSpawnEase = Ease.OutQuad;
    [SerializeField, FoldoutGroup("Resources")]
    private float _resourceMoveDuration = 1f;

    [SerializeField, FoldoutGroup("References")]
    private List<EntityDockedPosition> _dockedPositions;
    [SerializeField, FoldoutGroup("Jumping")]
    private float _jumpPower = 2f;
    [SerializeField, FoldoutGroup("Jumping")]
    private float _jumpDuration = 0.5f;
    [SerializeField, FoldoutGroup("Jumping")]
    
    private Coroutine _chargeLoopCoroutine;

    [SerializeField, ReadOnly]
    private AlphabeticNotation _internalTimer;
    [SerializeField, ReadOnly]
    private AlphabeticNotation _currentChargeTimePerSecond;
    [SerializeField, ReadOnly]
    private AlphabeticNotation _maxChargeTimeSeconds;

    private List<PhysicalResource> _physicalSpawned = new List<PhysicalResource>();
    
    [Topic(PickupEventIds.ON_ENTITY_ASSIGNED_TO_ZONE)]
    public void OnEntityAssignToZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        if (_zoneId != zoneId)
            return;

        EntityController controller = runtimeData.GetController();
        HandleEntityMovement(controller);

        if (IsOccupied())
            StartChargingLoop();
    }

    [Topic(PickupEventIds.ON_ENTITY_RECONNECTED_TO_ZONE)]
    public void OnEntityReconnectedOnLoad(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        if (_zoneId != zoneId)
            return;

        EntityController controller = runtimeData.GetController();
        controller.transform.position = transform.position;
        HandleEntityMovement(controller);

        if (IsOccupied())
            StartChargingLoop();
    }


    [Topic(PickupEventIds.ON_ENTITY_RETURNED_TO_ZONE)]
    public void OnEntityReturnedToZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        if (_zoneId != zoneId)
            return;

        // Just ensure they're in the right state - no jump animation
        EntityController controller = runtimeData.GetController();

        // Find their docked position if they already have one
        EntityDockedPosition dockedPosition = GetDockedPositionWithEntity(runtimeData.uniqueId);
        if (dockedPosition != null && dockedPosition.entity == controller)
        {
            // They're already docked, just ensure they're facing the right way
            controller.transform.localScale = new Vector3(GetDirectionToTarget(transform.position, controller.transform.position), 1, 1);
        }

        if (IsOccupied())
            StartChargingLoop();
    }

    [Topic(PickupEventIds.ON_ENTITY_REMOVED_FROM_ZONE)]
    public void OnEntityRemovedFromZone(object sender, EntityRuntimeData runtimeData, string zoneId)
    {
        if (_zoneId != zoneId)
            return;

        GetDockedPositionWithEntity(runtimeData.uniqueId)?.Remove();

        if (!IsOccupied())
            StopChargingLoop();
    }

    [Button]
    public void StopChargingLoop()
    {
        if (_chargeLoopCoroutine != null)
        {
            StopCoroutine(_chargeLoopCoroutine);
            _chargeLoopCoroutine = null;
        }
    }

    [Button]
    public void StartChargingLoop()
    {
        if (_chargeLoopCoroutine != null)
            return;
        _chargeLoopCoroutine = StartCoroutine(LifeEssenceChargeLoop());
    }

    public IEnumerator LifeEssenceChargeLoop()
    {
        _maxChargeTimeSeconds = GetTotalChargeTime();

        while (true)
        {
            _internalTimer += _currentChargeTimePerSecond * Time.deltaTime;
            yield return new WaitForEndOfFrame();

            if (_internalTimer >= _maxChargeTimeSeconds)
            {
                _internalTimer = new AlphabeticNotation(0);
                CreateLifeEssence();
            }
        }
    }

    [Button]
    public void CreateLifeEssence()
    {
        RemoveOldest();

        PhysicalResource resourceObject = _resourceToProvide.physicalResourceAssociation;
        //This will be changed later on to have different visuals for different quantities.
        AlphabeticNotation howMuch = StatManager.Instance.QueryStat(StatType.LifeEssenceGain);

        float x = _minMaxX.GetRandom();
        float y = _minMaxY.GetRandom();
        PhysicalResource createdResource = Instantiate(resourceObject, _spawnPosition.position,Quaternion.identity,null);
        createdResource.Initialise(new AlphabeticNotation(howMuch));
        createdResource.MoveToPosition(GetRandomSpawnToPosition(), _resourceMoveDuration, _resourceSpawnEase);
        _physicalSpawned.Add(createdResource);
    }

    [Topic(ResourceEventIds.ON_PHYSICAL_RESOURCE_DESTROYED)]
    public void OnResourceDestroyed(object sender, PhysicalResource physicalResource)
    {
        if (_physicalSpawned.Contains(physicalResource))
        {
            _physicalSpawned.Remove(physicalResource);
        }
    }

    public void RemoveOldest()
    {
        AlphabeticNotation maxSpawnCapacity = StatManager.Instance.QueryStat(StatType.LifeEssenceMaxCapacity);

        if (_physicalSpawned.Count == 0)
            return;

        if(_physicalSpawned.Count < (int)maxSpawnCapacity)
            return;

        PhysicalResource oldest = _physicalSpawned[0];
        _physicalSpawned.Remove(oldest);
        oldest.DestroyResource();
    }

    #region EntityManagement

    public void HandleEntityMovement(EntityController entity)
    {
        if (!GetFreePosition(out EntityDockedPosition dockedPosition))
        {
            Debug.LogWarning($"[{GetType().Name}] No free docked positions available for entity {entity.name}");
            return;
        }

        Rigidbody2D entityRigidbody = entity.GetComponent<Rigidbody2D>();
        entityRigidbody.bodyType = RigidbodyType2D.Kinematic;
        dockedPosition.Assign(entity);
        entity.transform.DOJump(dockedPosition.transform.position, _jumpPower, 1, _jumpDuration)
            .OnStart(() => 
            {
                entity.transform.localScale = new Vector3(GetDirectionToTarget(dockedPosition.transform.position, entity.transform.position), 1, 1);
            })
            .OnComplete(() =>
            {
                entity.transform.localScale = new Vector3(GetDirectionToTarget(transform.position, entity.transform.position), 1, 1);
                entityRigidbody.bodyType = RigidbodyType2D.Dynamic;
            })
            ;
        
        //CultistAnimator animator = entity.GetComponent<CultistAnimator>();
        //animator.PlayAnimation()
    }

    #endregion EntityManagement

    #region Helpers

    public AlphabeticNotation GetTotalChargeTime()
    {
        return StatManager.Instance.QueryStat(StatType.LifeEssenceMaxCharge);
    }

    public AlphabeticNotation GetChargeTimePerSecond()
    {
        return StatManager.Instance.QueryStat(StatType.LifeEssenceChargePerSecond);
    }

    public int GetDirectionToTarget(Vector2 targetPosition, Vector2 currentPosition)
    {
        float direction = targetPosition.x - currentPosition.x;
        if (direction > 0)
            return 1;
        else if (direction < 0)
            return -1;
        else
            return 0;
    }

    public Vector2 GetRandomSpawnToPosition()
    {
        return _spawnPosition.position + new Vector3(_minMaxX.GetRandom(), _minMaxY.GetRandom(), 0);
    }

    public bool IsOccupied() => _dropZone.GetCurrentCapacity() > 0;

    public bool GetFreePosition(out EntityDockedPosition dockedPosition)
    {
        if(AreAnyDocksFree())
        {
            dockedPosition = _dockedPositions.First(d => !d.IsOccupied());
            return true;
        }

        dockedPosition = null;
        return false;
    }

    public EntityDockedPosition GetDockedPositionWithEntity(string id) => _dockedPositions.FirstOrDefault(d => d.entity != null && d.entity.RuntimeData.uniqueId == id);

    public bool AreAnyDocksFree() => _dockedPositions.Any(d => !d.IsOccupied());

    public override List<StatModifier> GetModifiersForStat(StatType statType)
    {
        List<StatModifier> statModifiers = new List<StatModifier>();
        if (statType != StatType.LifeEssenceChargePerSecond)
            return statModifiers;

        List<EntityRuntimeData> assignedEntities = GetAssignedEntities();

        if (assignedEntities.Count == 0)
            return statModifiers;

        AlphabeticNotation totalCharge = AlphabeticNotation.zero;

        foreach (var entity in assignedEntities)
        {
            AlphabeticNotation entityDevotion = entity.GetStatValue(StatType.LifeEssenceChargePerSecond);
            totalCharge += entityDevotion;
        }

        if (!totalCharge.isZero)
        {
            statModifiers.Add(new StatModifier(
                StatType.LifeEssenceChargePerSecond,
                totalCharge,
                $"life_crystal_zone_{_zoneId}"));
        }

        return statModifiers;
    }
    #endregion Helpers

    [Topic(StatEventIds.ON_STATS_RECALCULATED)]
    public void OnStatsRecalculated(object sender)
    {
        _currentChargeTimePerSecond = GetChargeTimePerSecond();
        _maxChargeTimeSeconds = GetTotalChargeTime();
    }
}

