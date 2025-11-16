using LargeNumbers;
using System;

namespace LGD.ResourceSystem.Models
{
    [Serializable]
    public class ResourceAmountPair
    {
        public Resource resource;
        public AlphabeticNotation amount;

        public ResourceAmountPair(Resource resource, AlphabeticNotation amount)
        {
            this.resource = resource;
            this.amount = amount;
        }

        // Helper constructor for convenience with doubles
        public ResourceAmountPair(Resource resource, double amount)
        {
            this.resource = resource;
            this.amount = new AlphabeticNotation(amount);
        }
    }
}