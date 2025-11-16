using LGD.InteractionSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;

public class SpawnOnClick2D : ClickBase
{
    [FoldoutGroup("Spawn Settings"), SerializeField]
    private GameObject _prefabToSpawn;

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
    private bool _autoDestroy = true;

    [FoldoutGroup("Lifetime Settings"), SerializeField, ShowIf("_autoDestroy")]
    private float _destroyDelay = 2f;

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

        if (_prefabToSpawn == null)
        {
            Debug.LogWarning($"[SpawnOnClick2D] No prefab assigned on {gameObject.name}");
            return;
        }

        SpawnObject(clickData);
    }

    private void SpawnObject(InteractionData clickData)
    {
        Vector3 spawnPosition = CalculateSpawnPosition(clickData);
        Quaternion spawnRotation = CalculateSpawnRotation();

        GameObject spawnedObject = Instantiate(_prefabToSpawn, spawnPosition, spawnRotation);

        if (_setParent)
        {
            Transform parent = _parentTransform != null ? _parentTransform : transform;
            spawnedObject.transform.SetParent(parent);
        }

        if (_autoDestroy)
        {
            StartCoroutine(DestroyAfterDelay(spawnedObject));
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(_destroyDelay);

        if (obj != null)
        {
            Destroy(obj);
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

    public GameObject GetPrefab()
    {
        return _prefabToSpawn;
    }

    public SpawnLocation GetSpawnLocation()
    {
        return _spawnLocation;
    }

    public float GetDestroyDelay()
    {
        return _destroyDelay;
    }
}
