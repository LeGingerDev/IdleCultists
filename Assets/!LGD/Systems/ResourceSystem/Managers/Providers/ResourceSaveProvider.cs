using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LGD.ResourceSystem.SaveLoad
{
    public class ResourceSaveProvider : SaveLoadProviderBase<ResourceRuntimeData>
    {
        protected override string GetSaveFileName()
        {
            return "resources.json";
        }

        protected override IEnumerator CreateDefault()
        {
            _data.Clear();

            RegistryProviderBase<Resource> registry = RegistryManager.Instance.GetRegistry<Resource>();

            if (registry != null)
            {
                List<Resource> allResources = registry.GetAllItems();

                foreach (Resource resource in allResources)
                {
                    if (resource != null)
                    {
                        ResourceRuntimeData runtimeData = new ResourceRuntimeData(resource.id, AlphabeticNotation.zero);
                        _data.Add(runtimeData);
                    }
                }

                DebugManager.Log($"[Resource] <color=green>Created default resource data:</color> {_data.Count} resources");
            }
            else
            {
                DebugManager.Error("[Resource] ResourceRegistry not found! Cannot create default resource data");
            }

            yield return null;
        }

        /// <summary>
        /// Syncs new resources from the registry into the existing save data.
        /// Call this after loading to ensure any newly added resources are included.
        /// </summary>
        public IEnumerator SyncWithRegistry()
        {
            RegistryProviderBase<Resource> registry = RegistryManager.Instance.GetRegistry<Resource>();

            if (registry != null)
            {
                List<Resource> allResources = registry.GetAllItems();
                int addedCount = 0;

                foreach (Resource resource in allResources)
                {
                    if (resource == null) continue;

                    // Check if this resource already exists in save data
                    bool exists = _data.Exists(runtime => runtime.id == resource.id);

                    if (!exists)
                    {
                        // New resource detected - add it with zero amount
                        ResourceRuntimeData runtimeData = new ResourceRuntimeData(resource.id, AlphabeticNotation.zero);
                        _data.Add(runtimeData);
                        addedCount++;
                        DebugManager.Log($"[Resource] <color=yellow>New resource detected:</color> {resource.id}");
                    }
                }

                if (addedCount > 0)
                {
                    MarkDirty();
                    yield return Save(); // Save immediately to persist new resources
                    DebugManager.Log($"[Resource] <color=green>Synced {addedCount} new resource(s) from registry</color>");
                }
                else
                {
                    DebugManager.Log($"[Resource] <color=cyan>Registry sync complete:</color> No new resources to add");
                }
            }
            else
            {
                DebugManager.Error("[Resource] ResourceRegistry not found! Cannot sync with registry");
            }

            yield return null;
        }
    }
}