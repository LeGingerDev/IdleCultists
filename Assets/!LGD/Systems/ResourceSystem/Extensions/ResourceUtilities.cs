using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LGD.ResourceSystem.Utilities
{
    public static class ResourceUtilities
    {
        private static RegistryProviderBase<Resource> _registry;

        private static RegistryProviderBase<Resource> GetRegistry()
        {
            if (_registry == null)
            {
                _registry = RegistryManager.Instance.GetRegistry<Resource>();
            }
            return _registry;
        }

        /// <summary>
        /// Get a Resource by its ID string
        /// </summary>
        public static Resource GetResourceById(string resourceId)
        {
            var registry = GetRegistry();
            if (registry == null)
            {
                DebugManager.Error("[Resource] Resource registry not found!");
                return null;
            }

            return registry.GetItemById(resourceId);
        }

        /// <summary>
        /// Get all registered resources
        /// </summary>
        public static List<Resource> GetAllResources()
        {
            var registry = GetRegistry();
            if (registry == null)
            {
                DebugManager.Error("[Resource] Resource registry not found!");
                return new List<Resource>();
            }

            return registry.GetAllItems();
        }

        /// <summary>
        /// Check if a resource with the given ID exists in the registry
        /// </summary>
        public static bool ResourceExists(string resourceId)
        {
            return GetResourceById(resourceId) != null;
        }

        /// <summary>
        /// Get all resources that the player currently has (amount > 0)
        /// </summary>
        public static List<Resource> GetOwnedResources()
        {
            return GetAllResources()
                .Where(r => ResourceManager.Instance.HasAnyOfResource(r))
                .ToList();
        }

        /// <summary>
        /// Get all resources with their current amounts
        /// </summary>
        public static List<ResourceAmountPair> GetAllResourcesWithAmounts()
        {
            List<Resource> allResources = GetAllResources();
            List<ResourceAmountPair> pairs = new List<ResourceAmountPair>();

            foreach (Resource resource in allResources)
            {
                AlphabeticNotation amount = ResourceManager.Instance.GetResourceAmount(resource);
                pairs.Add(new ResourceAmountPair(resource, amount));
            }

            return pairs;
        }

        /// <summary>
        /// Get only resources that have non-zero amounts
        /// </summary>
        public static List<ResourceAmountPair> GetOwnedResourcesWithAmounts()
        {
            return GetAllResourcesWithAmounts()
                .Where(pair => !pair.amount.isZero)
                .ToList();
        }

        #region Resource Extensions

        /// <summary>
        /// Get a resource by its ID (convenience extension on string)
        /// </summary>
        public static Resource ToResource(this string resourceId)
        {
            return GetResourceById(resourceId);
        }

        #endregion
    }
}