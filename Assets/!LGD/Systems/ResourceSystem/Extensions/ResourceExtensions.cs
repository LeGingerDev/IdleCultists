using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LGD.ResourceSystem.Extensions
{
    public static class ResourceExtensions
    {
        #region Resource Direct Access Extensions

        /// <summary>
        /// Get the total amount of this resource the player currently has
        /// </summary>
        public static AlphabeticNotation GetTotalAmount(this Resource resource)
        {
            return ResourceManager.Instance.GetResourceAmount(resource);
        }

        /// <summary>
        /// Check if the player has any of this resource
        /// </summary>
        public static bool HasAny(this Resource resource)
        {
            return ResourceManager.Instance.HasAnyOfResource(resource);
        }

        /// <summary>
        /// Check if the player can afford to spend the specified amount of this resource
        /// </summary>
        public static bool CanSpend(this Resource resource, AlphabeticNotation amount)
        {
            return ResourceManager.Instance.CanSpend(resource, amount);
        }

        /// <summary>
        /// Check if the player can afford to spend the specified amount of this resource
        /// </summary>
        public static bool CanSpend(this Resource resource, double amount)
        {
            return ResourceManager.Instance.CanSpend(resource, amount);
        }

        /// <summary>
        /// Add the specified amount of this resource to the player
        /// </summary>
        public static void Add(this Resource resource, AlphabeticNotation amount)
        {
            ResourceManager.Instance.AddResource(resource, amount);
        }

        /// <summary>
        /// Add the specified amount of this resource to the player
        /// </summary>
        public static void Add(this Resource resource, double amount)
        {
            ResourceManager.Instance.AddResource(resource, amount);
        }

        /// <summary>
        /// Remove the specified amount of this resource from the player. Returns false if player can't afford it.
        /// </summary>
        public static bool Remove(this Resource resource, AlphabeticNotation amount)
        {
            return ResourceManager.Instance.RemoveResource(resource, amount);
        }

        /// <summary>
        /// Remove the specified amount of this resource from the player. Returns false if player can't afford it.
        /// </summary>
        public static bool Remove(this Resource resource, double amount)
        {
            return ResourceManager.Instance.RemoveResource(resource, amount);
        }

        #endregion

        #region ResourceRuntimeData Extensions

        /// <summary>
        /// Get the Resource ScriptableObject for this runtime data
        /// </summary>
        public static Resource GetResource(this ResourceRuntimeData runtimeData)
        {
            RegistryProviderBase<Resource> registry = RegistryManager.Instance.GetRegistry<Resource>();
            if (registry == null)
            {
                DebugManager.Error("[Resource] ResourceRegistry not found!");
                return null;
            }
            return registry.GetItemById(runtimeData.id);
        }

        /// <summary>
        /// Convert runtime data to ResourceAmountPair
        /// </summary>
        public static ResourceAmountPair ToResourceAmountPair(this ResourceRuntimeData runtimeData)
        {
            Resource resource = runtimeData.GetResource();
            return new ResourceAmountPair(resource, runtimeData.amount);
        }

        #endregion

        #region List Conversion Extensions

        /// <summary>
        /// Convert List of ResourceRuntimeData to List of ResourceAmountPairs
        /// </summary>
        public static List<ResourceAmountPair> ToResourceAmountPairs(this List<ResourceRuntimeData> runtimeDataList)
        {
            return runtimeDataList.Select(r => r.ToResourceAmountPair()).ToList();
        }

        #endregion

        #region Dictionary Extensions

        public static List<(Resource resource, int amount)> ToResourceList(this Dictionary<Resource, int> dictionary)
        {
            return dictionary.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }

        #endregion

        #region List<ResourceAmountPair> String Formatting

        // Extension for List<ResourceAmountPair>
        public static string ToStringNewLined(this List<ResourceAmountPair> resources)
        {
            if (resources == null || resources.Count == 0)
                return "No resources required";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < resources.Count; i++)
            {
                var r = resources[i];
                sb.Append($"{r.resource.displayName}: {r.amount:0.##}");
                if (i < resources.Count - 1)
                    sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string ToStringCommaSeparated(this List<ResourceAmountPair> resources)
        {
            if (resources == null || resources.Count == 0)
                return "No resources required";

            return string.Join(", ", resources.Select(r => $"{r.resource.displayName}: {r.amount:0.##}"));
        }

        public static string ToStringBulleted(this List<ResourceAmountPair> resources)
        {
            if (resources == null || resources.Count == 0)
                return "No resources required";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < resources.Count; i++)
            {
                var r = resources[i];
                sb.Append($"� {r.resource.displayName}: {r.amount:0.##}");
                if (i < resources.Count - 1)
                    sb.AppendLine();
            }
            return sb.ToString();
        }

        #endregion

        #region List<(Resource, float)> String Formatting

        // Extension for List<(Resource, float)> tuples
        public static string ToStringNewLined(this List<(Resource resource, float amount)> resources)
        {
            if (resources == null || resources.Count == 0)
                return "No resources required";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < resources.Count; i++)
            {
                var r = resources[i];
                sb.Append($"{r.resource.displayName}: {r.amount:0.##}");
                if (i < resources.Count - 1)
                    sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string ToStringCommaSeparated(this List<(Resource resource, float amount)> resources)
        {
            if (resources == null || resources.Count == 0)
                return "No resources required";

            return string.Join(", ", resources.Select(r => $"{r.resource.displayName}: {r.amount:0.##}"));
        }

        public static string ToStringBulleted(this List<(Resource resource, float amount)> resources)
        {
            if (resources == null || resources.Count == 0)
                return "No resources required";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < resources.Count; i++)
            {
                var r = resources[i];
                sb.Append($"� {r.resource.displayName}: {r.amount:0.##}");
                if (i < resources.Count - 1)
                    sb.AppendLine();
            }
            return sb.ToString();
        }

        #endregion

        #region Single Resource String Formatting

        // Single resource amount pair
        public static string ToStringSingle(this ResourceAmountPair resource)
        {
            return $"{resource.resource.displayName}: {resource.amount:0.##}";
        }

        #endregion
    }
}