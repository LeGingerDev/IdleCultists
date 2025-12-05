using LGD.Core;
using Sirenix.OdinInspector;
using UnityEngine;

public class EntityController : BaseBehaviour
{
    [SerializeField, ReadOnly]
    private EntityBlueprint _blueprint;
    public EntityBlueprint Blueprint => _blueprint;

    [SerializeField, ReadOnly]
    private EntityRuntimeData _runtimeData;
    public EntityRuntimeData RuntimeData => _runtimeData;

    private float _positionUpdateInterval = 1f;
    private float _timeSinceLastPositionUpdate = 0f;

    /// <summary>
    /// Initialize with a fresh entity (spawned from blueprint)
    /// </summary>
    public void Initialise(EntityBlueprint blueprint)
    {
        _blueprint = blueprint;
        _runtimeData = new EntityRuntimeData();
        _runtimeData.Initialise(blueprint);

        // Set initial position
        _runtimeData.UpdatePositionFromTransform(transform);

        Publish(EntityEventIds.ON_ENTITY_SPAWNED, _runtimeData, false);

        Debug.Log($"Entity spawned: {blueprint.displayName} with ID: {_runtimeData.uniqueId}");
    }

    /// <summary>
    /// Initialize with loaded runtime data (restored from save)
    /// </summary>
    public void Initialise(EntityRuntimeData runtimeData, EntityBlueprint blueprint)
    {
        _runtimeData = runtimeData;
        _blueprint = blueprint;

        // Position was already set by EntityManager when spawning
        // Just register with manager
        Publish(EntityEventIds.ON_ENTITY_SPAWNED, _runtimeData, true);

        Debug.Log($"Entity loaded: {blueprint.displayName} with ID: {_runtimeData.uniqueId}");
    }

    private void Update()
    {
        // Periodically update position in runtime data
        _timeSinceLastPositionUpdate += Time.deltaTime;

        if (_timeSinceLastPositionUpdate >= _positionUpdateInterval)
        {
            UpdatePosition();
            _timeSinceLastPositionUpdate = 0f;
        }
    }

    /// <summary>
    /// Update the saved position for this entity
    /// </summary>
    public void UpdatePosition()
    {
        if (_runtimeData != null && EntityManager.Instance != null)
        {
            EntityManager.Instance.UpdateEntityPosition(_runtimeData, transform);
        }
    }

    /// <summary>
    /// Force immediate position update (call when entity is assigned to zone or moves significantly)
    /// </summary>
    public void ForcePositionUpdate()
    {
        UpdatePosition();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_runtimeData != null)
        {
            // Update position one last time before being destroyed
            UpdatePosition();
            Publish(EntityEventIds.ON_ENTITY_DESTROYED, _runtimeData);
        }
    }


}