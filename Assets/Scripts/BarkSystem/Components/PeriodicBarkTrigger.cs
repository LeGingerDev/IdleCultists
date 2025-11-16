using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

/// <summary>
/// Triggers periodic ambient barks for a cultist based on their current zone.
/// Attach to each cultist prefab.
/// Waits 20-40 seconds (random) then triggers a bark based on currentZoneId.
/// </summary>
[RequireComponent(typeof(EntityController))]
public class PeriodicBarkTrigger : BaseBehaviour
{
    [SerializeField, FoldoutGroup("Settings")]
    private float _minBarkInterval = 20f;

    [SerializeField, FoldoutGroup("Settings")]
    private float _maxBarkInterval = 40f;

    [SerializeField, FoldoutGroup("Settings")]
    private bool _enablePeriodicBarks = true;

    [SerializeField, FoldoutGroup("Settings")]
    [Tooltip("Only bark when in these states")]
    private EntityState[] _validStatesForBarking = new EntityState[]
    {
        EntityState.Assigned
    };

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private bool _isActive;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private float _nextBarkTime;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private string _currentZoneId;

    private EntityController _entityController;
    private Coroutine _periodicBarkCoroutine;

    private void Awake()
    {
        _entityController = GetComponent<EntityController>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartPeriodicBarks();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopPeriodicBarks();
    }

    private void StartPeriodicBarks()
    {
        if (!_enablePeriodicBarks)
            return;

        if (_periodicBarkCoroutine != null)
            StopCoroutine(_periodicBarkCoroutine);

        _periodicBarkCoroutine = StartCoroutine(PeriodicBarkCoroutine());
        _isActive = true;
    }

    private void StopPeriodicBarks()
    {
        if (_periodicBarkCoroutine != null)
        {
            StopCoroutine(_periodicBarkCoroutine);
            _periodicBarkCoroutine = null;
        }
        _isActive = false;
    }

    private IEnumerator PeriodicBarkCoroutine()
    {
        // Wait a bit after spawn before first bark
        yield return new WaitForSeconds(Random.Range(_minBarkInterval * 0.5f, _minBarkInterval));

        while (true)
        {
            // Wait random interval
            float interval = Random.Range(_minBarkInterval, _maxBarkInterval);
            _nextBarkTime = Time.time + interval;
            yield return new WaitForSeconds(interval);

            // Trigger bark based on current state
            TriggerAmbientBark();
        }
    }

    private void TriggerAmbientBark()
    {
        if (_entityController?.RuntimeData == null)
            return;

        EntityRuntimeData runtimeData = _entityController.RuntimeData;

        // Cache for debug
        _currentZoneId = runtimeData.currentZoneId;

        // Only bark when in valid states
        bool isValidState = System.Array.Exists(_validStatesForBarking, state => state == runtimeData.currentState);
        if (!isValidState)
        {
            Debug.Log($"[PeriodicBarkTrigger] Entity {runtimeData.entityName} not in valid state for barking: {runtimeData.currentState}");
            return;
        }

        // Get appropriate ambient bark context for current zone
        BarkContext context = BarkManager.Instance.GetAmbientBarkContextForZone(runtimeData.currentZoneId);

        // Request the bark
        BarkManager.Instance.RequestBark(runtimeData, context);

        Debug.Log($"[PeriodicBarkTrigger] Triggered ambient bark for {runtimeData.entityName} in zone {runtimeData.currentZoneId} (context: {context})");
    }

    #region Debug Helpers

    [Button("Force Ambient Bark Now"), FoldoutGroup("Debug")]
    private void DebugForceAmbientBark()
    {
        TriggerAmbientBark();
    }

    [Button("Toggle Periodic Barks"), FoldoutGroup("Debug")]
    private void DebugTogglePeriodicBarks()
    {
        _enablePeriodicBarks = !_enablePeriodicBarks;

        if (_enablePeriodicBarks)
            StartPeriodicBarks();
        else
            StopPeriodicBarks();

        Debug.Log($"[PeriodicBarkTrigger] Periodic barks {(_enablePeriodicBarks ? "enabled" : "disabled")}");
    }

    #endregion
}