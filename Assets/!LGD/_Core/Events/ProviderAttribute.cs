using System;

namespace LGD.Core.Events
{
    /// <summary>
    /// Mark a method as a data provider for RequestBus
    /// The method MUST return a value (not void) and take at least one parameter (the requester)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ProviderAttribute : Attribute
    {
        public string RequestType { get; }
        public int Priority { get; }

        public ProviderAttribute(string requestType, int priority = 1)
        {
            RequestType = requestType;
            Priority = priority;
        }
    }
}