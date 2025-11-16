using System.Collections.Generic;
using UnityEngine;

namespace PoolSystem
{
    [System.Serializable]
    public class Pool
    {
        [SerializeField] private string _key;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSpawnCount;

        private List<GameObject> _spawnedObjects;
        private Transform _containerTransform;

        public string GetKey() => _key;
        public GameObject GetPrefab() => _prefab;
        public int GetInitialSpawnCount() => _initialSpawnCount;

        public void Initialize(Transform parentTransform)
        {
            _spawnedObjects = new List<GameObject>();

            // Create container GameObject
            GameObject container = new GameObject(_key);
            container.transform.SetParent(parentTransform);
            _containerTransform = container.transform;

            // Spawn initial objects
            for (int i = 0; i < _initialSpawnCount; i++)
            {
                SpawnNewObject();
            }
        }

        public GameObject GetObject(Transform newParent = null)
        {
            // Find first inactive object
            GameObject obj = FindInactiveObject();

            // If none available, spawn a new one
            if (obj == null)
            {
                obj = SpawnNewObject();
            }

            // Set parent if provided
            if (newParent != null)
            {
                obj.transform.SetParent(newParent);
            }
            else
            {
                // Unparent from container
                obj.transform.SetParent(null);
            }

            return obj;
        }

        public void ReturnObject(GameObject obj, bool deactivate)
        {
            if (!_spawnedObjects.Contains(obj))
            {
                DebugManager.Error($"[Polling] [Pool] Trying to return an object that doesn't belong to pool '{_key}'");
                return;
            }

            // Re-parent to container
            obj.transform.SetParent(_containerTransform);

            // Deactivate if requested
            if (deactivate)
            {
                obj.SetActive(false);
            }
        }

        private GameObject FindInactiveObject()
        {
            foreach (GameObject obj in _spawnedObjects)
            {
                if (!obj.activeInHierarchy)
                {
                    return obj;
                }
            }
            return null;
        }

        private GameObject SpawnNewObject()
        {
            GameObject obj = Object.Instantiate(_prefab, _containerTransform);
            obj.SetActive(false);
            _spawnedObjects.Add(obj);
            return obj;
        }
    }
}