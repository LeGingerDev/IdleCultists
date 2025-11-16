using LargeNumbers;
using System;

namespace LGD.ResourceSystem.Models
{
    /// <summary>
    /// Runtime data for saving/loading resource amounts
    /// Stores resource by ID instead of reference for proper serialization
    /// </summary>
    [Serializable]
    public class ResourceRuntimeData
    {
        public string id;
        public AlphabeticNotation amount;

        // Parameterless constructor for JSON deserialization
        public ResourceRuntimeData() { }

        public ResourceRuntimeData(string id, AlphabeticNotation amount)
        {
            this.id = id;
            this.amount = amount;
        }

        public ResourceRuntimeData(Resource resource, AlphabeticNotation amount)
        {
            this.id = resource != null ? resource.id : string.Empty;
            this.amount = amount;
        }
    }
}