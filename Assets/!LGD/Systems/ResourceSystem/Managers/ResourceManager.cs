using LargeNumbers;
using LGD.Core.Singleton;
using LGD.ResourceSystem.Models;
using LGD.ResourceSystem.SaveLoad;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LGD.ResourceSystem.Managers
{
    public class ResourceManager : MonoSingleton<ResourceManager>
    {
        [SerializeField, FoldoutGroup("Resource Definitions")]
        private List<Resource> _resourceDefinitions = new List<Resource>();

        [SerializeField, FoldoutGroup("Debug/Resources")]
        private Dictionary<Resource, AlphabeticNotation> _resources = new Dictionary<Resource, AlphabeticNotation>();

        [SerializeField, FoldoutGroup("Settings")]
        private bool _autoSave = true;

        private SaveLoadProviderBase<ResourceRuntimeData> _saveProvider;
        private bool _isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            StartCoroutine(InitializeResources());
        }

        #region Initialization

        private IEnumerator InitializeResources()
        {
            _saveProvider = SaveLoadProviderManager.Instance.GetProvider<ResourceRuntimeData>();

            if (_saveProvider != null)
            {
                yield return _saveProvider.Load();

                // Sync with registry to add any new resources that were added after the save file was created
                ResourceSaveProvider resourceProvider = _saveProvider as ResourceSaveProvider;
                if (resourceProvider != null)
                {
                    yield return resourceProvider.SyncWithRegistry();
                }

                ConvertRuntimeDataToDictionary(_saveProvider.GetData());
                DebugManager.Log($"[Resource] <color=cyan>Resource Manager initialized:</color> {_resources.Count} resources loaded");

                // ADD THIS - Notify listeners that resources have been loaded
                Publish(ResourceEventIds.ON_RESOURCES_UPDATED, _resources);
            }
            else
            {
                DebugManager.Warning("[Resource] Resource save provider not found! Initializing with empty resources. Make sure ResourceSaveProvider is in the scene.");
                InitializeResourcesFromDefinitions();

                // ADD THIS - Notify listeners that resources have been initialized
                Publish(ResourceEventIds.ON_RESOURCES_UPDATED, _resources);
            }

            _isInitialized = true;
        }

        [Button, FoldoutGroup("Debug")]
        private void InitializeResourcesFromDefinitions()
        {
            // Initialize all resources from the definitions list
            foreach (var resource in _resourceDefinitions)
            {
                if (resource != null && !_resources.ContainsKey(resource))
                {
                    _resources[resource] = AlphabeticNotation.zero;
                }
            }
        }

        private void ConvertRuntimeDataToDictionary(List<ResourceRuntimeData> runtimeData)
        {
            _resources.Clear();

            RegistryProviderBase<Resource> registry = RegistryManager.Instance.GetRegistry<Resource>();
            if (registry == null)
            {
                DebugManager.Error("[Resource] ResourceRegistry not found! Cannot convert runtime data");
                return;
            }

            foreach (ResourceRuntimeData data in runtimeData)
            {
                Resource resource = registry.GetItemById(data.id);
                if (resource != null)
                {
                    _resources[resource] = data.amount;
                }
                else
                {
                    DebugManager.Warning($"[Resource] Resource with ID '{data.id}' not found in registry. Skipping.");
                }
            }
        }

        private List<ResourceRuntimeData> ConvertDictionaryToRuntimeData()
        {
            List<ResourceRuntimeData> runtimeData = new List<ResourceRuntimeData>();

            foreach (var kvp in _resources)
            {
                if (kvp.Key != null)
                {
                    runtimeData.Add(new ResourceRuntimeData(kvp.Key.id, kvp.Value));
                }
            }

            return runtimeData;
        }

        #endregion

        #region Add Resources

        public void AddResource(Resource resource, AlphabeticNotation amount)
        {
            if (resource == null) return;

            if (!_resources.ContainsKey(resource))
                _resources[resource] = AlphabeticNotation.zero;

            _resources[resource] += amount;

            SyncDataToProvider();

            // Publish the delta for cumulative tracking
            Publish(ResourceEventIds.ON_RESOURCE_ADDED, resource, amount);

            // Publish the full dictionary for current/max tracking
            Publish(ResourceEventIds.ON_RESOURCES_UPDATED, _resources);
        }

        public void AddResource(ResourceAmountPair pair)
        {
            AddResource(pair.resource, pair.amount);
        }

        public void AddResource(Resource resource, double amount)
        {
            AddResource(resource, new AlphabeticNotation(amount));
        }

        #endregion

        #region Remove Resources

        public bool RemoveResource(Resource resource, AlphabeticNotation amount)
        {
            if (resource == null) return false;

            if (!CanSpend(resource, amount))
            {
                DebugManager.Warning($"[Resource] Cannot afford {amount} {resource.displayName}");
                return false;
            }

            _resources[resource] -= amount;

            SyncDataToProvider();

            Publish(ResourceEventIds.ON_RESOURCES_UPDATED, _resources);

            DebugManager.Log($"[Resource] Removed {amount} {resource.displayName}. New total: {_resources[resource]}");
            return true;
        }

        public bool RemoveResource(ResourceAmountPair pair)
        {
            return RemoveResource(pair.resource, pair.amount);
        }

        public bool RemoveResource(Resource resource, double amount)
        {
            return RemoveResource(resource, new AlphabeticNotation(amount));
        }

        #endregion

        #region Getters

        public AlphabeticNotation GetResourceAmount(Resource resource)
        {
            if (resource == null)
                return AlphabeticNotation.zero;

            if (_resources.TryGetValue(resource, out AlphabeticNotation amount))
                return amount;

            return AlphabeticNotation.zero;
        }

        public AlphabeticNotation GetResourceAmount(string resourceId)
        {
            // Find the Resource by its ID
            foreach (var kvp in _resources)
            {
                if (kvp.Key != null && kvp.Key.id == resourceId)
                    return kvp.Value;
            }

            return AlphabeticNotation.zero;
        }

        public bool HasAnyOfResource(Resource resource)
        {
            if (resource == null) return false;

            AlphabeticNotation amount = GetResourceAmount(resource);
            return !amount.isZero;
        }

        public bool IsInitialized()
        {
            return _isInitialized;
        }

        #endregion

        #region Affordability Checks

        public bool CanSpend(Resource resource, AlphabeticNotation amount)
        {
            if (resource == null) return false;

            AlphabeticNotation current = GetResourceAmount(resource);
            return current >= amount;
        }

        public bool CanSpend(ResourceAmountPair pair)
        {
            return CanSpend(pair.resource, pair.amount);
        }

        public bool CanSpend(Resource resource, double amount)
        {
            return CanSpend(resource, new AlphabeticNotation(amount));
        }

        public bool CanSpendMultiple(List<ResourceAmountPair> costs)
        {
            foreach (var cost in costs)
            {
                if (!CanSpend(cost))
                    return false;
            }
            return true;
        }

        #endregion

        #region Save Methods

        private void MarkDirty()
        {
            if (_saveProvider != null)
            {
                _saveProvider.MarkDirty();
            }
        }

        /// <summary>
        /// Syncs the current dictionary state to the save provider's data list.
        /// This ensures the provider has the latest data when SaveLoop runs.
        /// Call this after every dictionary modification.
        /// </summary>
        private void SyncDataToProvider()
        {
            if (_saveProvider != null)
            {
                List<ResourceRuntimeData> runtimeData = ConvertDictionaryToRuntimeData();
                StartCoroutine(_saveProvider.SetData(runtimeData));
                // SetData already calls MarkDirty() internally
            }
        }

        private IEnumerator SaveResources()
        {
            if (_saveProvider != null)
            {
                List<ResourceRuntimeData> runtimeData = ConvertDictionaryToRuntimeData();
                yield return _saveProvider.SetData(runtimeData);

                if (_autoSave)
                {
                    yield return _saveProvider.Save();
                }
            }
        }

        public IEnumerator ManualSave()
        {
            if (_saveProvider != null)
            {
                List<ResourceRuntimeData> runtimeData = ConvertDictionaryToRuntimeData();
                yield return _saveProvider.SetData(runtimeData);
                yield return _saveProvider.Save();
            }
        }

        public IEnumerator SaveIfDirty()
        {
            if (_saveProvider != null)
            {
                List<ResourceRuntimeData> runtimeData = ConvertDictionaryToRuntimeData();
                yield return _saveProvider.SetData(runtimeData);
                yield return _saveProvider.SaveIfDirty();
            }
        }

        #endregion

        #region Set Resources (For Debug/Testing)

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                DebugAddTestResources(1000);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                DebugAddTestResources(10000);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                DebugAddTestResources(100000);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad3))
            {
                DebugAddTestResources(1000000);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                DebugClearAllResources();
            }
        }

        [Button("Add Test Resources"), FoldoutGroup("Debug")]
        private void DebugAddTestResources(double value)
        {
            // Add test resources for debugging
            foreach (var resource in _resourceDefinitions)
            {
                if (resource != null)
                {
                    _resources[resource] += new AlphabeticNotation(value);
                }
            }

            SyncDataToProvider();
            Publish(ResourceEventIds.ON_RESOURCES_UPDATED, _resources);
            DebugManager.Log($"[Resource] Added {value} to all resources");
        }

        [Button("Clear All Resources"), FoldoutGroup("Debug")]
        private void DebugClearAllResources()
        {
            foreach (var resource in _resourceDefinitions)
            {
                if (resource != null)
                {
                    _resources[resource] = AlphabeticNotation.zero;
                }
            }

            SyncDataToProvider();
            Publish(ResourceEventIds.ON_RESOURCES_UPDATED, _resources);
            DebugManager.Log("[Resource] Cleared all resources");
        }

        [Button("Manual Save"), FoldoutGroup("Debug")]
        private void DebugManualSave()
        {
            StartCoroutine(ManualSave());
        }

        [Button]
        public void SetResource(Resource resource, AlphabeticNotation amount)
        {
            if (resource == null) return;
            _resources[resource] = amount;

            SyncDataToProvider();

            // TODO: Replace with Topic System
            Publish(ResourceEventIds.ON_RESOURCES_UPDATED, _resources);
        }

        public void SetResource(Resource resource, double amount)
        {
            SetResource(resource, new AlphabeticNotation(amount));
        }

        #endregion
    }
}