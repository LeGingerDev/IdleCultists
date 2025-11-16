using LGD.InteractionSystem;
using LGD.Utilities.Attributes;
using PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;

public class SpawnPoolItemOnClick2D : ClickBase
{
    [FoldoutGroup("Spawn Settings"), SerializeField, ConstDropdown(typeof(PoolKeys))]
    private string _poolKey;

    [FoldoutGroup("Spawn Settings"), SerializeField]
    private SpawnLocation _spawnLocation = SpawnLocation.ClickWorldPosition;

    [FoldoutGroup("Spawn Settings"), SerializeField]
    private Vector3 _localOffset = Vector3.zero;

    [FoldoutGroup("Spawn Settings"), SerializeField]
    private bool _matchRotation = false;

    [FoldoutGroup("Parent Settings"), SerializeField]
    private bool _setParent = false;

    [FoldoutGroup("Parent Settings"), SerializeField, ShowIf("_setParent")]
    private Transform _parentTransform;

    [FoldoutGroup("Lifetime Settings"), SerializeField]
    private bool _autoReturn = true;

    [FoldoutGroup("Lifetime Settings"), SerializeField, ShowIf("_autoReturn")]
    private float _returnDelay = 2f;

    public enum SpawnLocation
    {
        ClickWorldPosition,
        ObjectPosition,
        CustomTransform
    }

    [FoldoutGroup("Parent Settings"), SerializeField, ShowIf("_spawnLocation", SpawnLocation.CustomTransform)]
    private Transform _customSpawnTransform;

    public override void OnMouseUpEvent(InteractionData clickData)
    {
        base.OnMouseUpEvent(clickData);

        if (string.IsNullOrEmpty(_poolKey))
        {
            Debug.LogWarning($"[SpawnPoolItemOnClick2D] No pool key assigned on {gameObject.name}");
            return;
        }

        SpawnObject(clickData);
    }

    private void SpawnObject(InteractionData clickData)
    {
        Vector3 spawnPosition = CalculateSpawnPosition(clickData);
        Quaternion spawnRotation = CalculateSpawnRotation();

        GameObject pooledItem = PoolingManager.Get(_poolKey);

        if (pooledItem == null)
        {
            Debug.LogWarning($"[SpawnPoolItemOnClick2D] Failed to get pooled item with key: {_poolKey}");
            return;
        }

        pooledItem.transform.position = spawnPosition;
        pooledItem.transform.rotation = spawnRotation;
        pooledItem.SetActive(true);

        if (_setParent)
        {
            Transform parent = _parentTransform != null ? _parentTransform : transform;
            pooledItem.transform.SetParent(parent);
        }

        if (_autoReturn)
        {
            StartCoroutine(ReturnToPoolAfterDelay(pooledItem));
        }
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(_returnDelay);

        if (obj != null && obj.activeInHierarchy)
        {
            PoolingManager.Return(obj, _poolKey, true);
        }
    }

    private Vector3 CalculateSpawnPosition(InteractionData clickData)
    {
        Vector3 basePosition = Vector3.zero;

        switch (_spawnLocation)
        {
            case SpawnLocation.ClickWorldPosition:
                basePosition = GetClickWorldPosition(clickData);
                break;

            case SpawnLocation.ObjectPosition:
                basePosition = transform.position;
                break;

            case SpawnLocation.CustomTransform:
                basePosition = _customSpawnTransform != null
                    ? _customSpawnTransform.position
                    : transform.position;
                break;
        }

        return basePosition + _localOffset;
    }

    private Vector3 GetClickWorldPosition(InteractionData clickData)
    {
        if (clickData.Is2DHit && clickData.Hit.HasValue)
        {
            return clickData.Hit.Value.Point;
        }

        Vector3 screenPos = clickData.ScreenPosition;
        screenPos.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    private Quaternion CalculateSpawnRotation()
    {
        return _matchRotation ? transform.rotation : Quaternion.identity;
    }

    public string GetPoolKey()
    {
        return _poolKey;
    }

    public SpawnLocation GetSpawnLocation()
    {
        return _spawnLocation;
    }

    public float GetReturnDelay()
    {
        return _returnDelay;
    }
}