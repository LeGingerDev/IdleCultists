using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LGD.Core.Events
{
    /// <summary>
    /// Request-Response system where you ask for data and providers respond
    /// Similar to ServiceBus but reversed - you REQUEST information instead of PUBLISH events
    /// </summary>
    public static class RequestBus
    {
        private static readonly List<(object instance, string requestType, int priority, MethodInfo method)> _providers =
            new List<(object instance, string requestType, int priority, MethodInfo method)>();

        [RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void RegisterDefaultReferences()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                    {
                        var attributes = method.GetCustomAttributes<ProviderAttribute>();
                        if (!attributes.Any())
                            continue;

                        foreach (var attribute in attributes)
                        {
                            if (attribute == null || string.IsNullOrEmpty(attribute.RequestType))
                                continue;

                            _providers.Add((null, attribute.RequestType, attribute.Priority, method));
                        }
                    }
                }
            }
        }

        public static void RegisterInstance(object instance)
        {
            foreach (var methodProvider in ParseObject(instance))
            {
                _providers.Add((instance, methodProvider.requestType, methodProvider.priority, methodProvider.method));
            }
        }

        public static void UnregisterInstance(object instance)
        {
            _providers.RemoveAll(x => x.instance == instance);
        }

        /// <summary>
        /// Check if there's at least one provider for a request type
        /// </summary>
        public static bool HasProvider(string requestType)
        {
            return _providers.Any(x => x.requestType == requestType);
        }

        /// <summary>
        /// Request data of a specific type. Returns the first response found.
        /// </summary>
        public static T Request<T>(string requestType, object requester, params object[] args)
        {
            var responses = RequestAll<T>(requestType, requester, args);
            return responses.FirstOrDefault();
        }

        /// <summary>
        /// Request data from ALL providers. Returns all responses.
        /// Useful when you want to aggregate data from multiple sources.
        /// </summary>
        public static List<T> RequestAll<T>(string requestType, object requester, params object[] args)
        {
            List<T> responses = new List<T>();

            foreach (var (instance, priority, method) in GetProvidersFor(requestType)
                         .OrderByDescending(provider => provider.priority))
            {
                var parameters = method.GetParameters();
                List<object> arguments = new List<object> { requester };

                // Skip if the method doesn't have at least one parameter (the requester)
                if (parameters.Length < 1)
                {
                    DebugManager.Warning($"[Core] Invalid provider for request {requestType}: {method.Name}");
                    continue;
                }

                // Handle params array
                if (parameters.Length == 2 &&
                    parameters[1].GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                {
                    arguments.Add(args);
                }
                else
                {
                    // Match additional parameters
                    foreach (var param in parameters.Skip(1))
                    {
                        object val = args.FirstOrDefault(v => v != null && v.GetType() == param.ParameterType);
                        if (val == null)
                            val = args.FirstOrDefault(v => v != null && param.ParameterType.IsAssignableFrom(v.GetType()));

                        if (val == null)
                        {
                            DebugManager.Warning($"[Core] Could not resolve value for {param} whilst requesting {requestType} at {method.Name}");
                            continue;
                        }

                        arguments.Add(val);
                    }
                }

                try
                {
                    var result = method.Invoke(instance, arguments.ToArray());

                    if (result is T typedResult)
                    {
                        responses.Add(typedResult);
                    }
                    else if (result != null)
                    {
                        DebugManager.Warning($"[Core] Provider {method.Name} returned {result.GetType()} but expected {typeof(T)}");
                    }
                }
                catch (Exception ex)
                {
                    DebugManager.Error($"[Core] {ex.Message} - {requestType} - {method} | {instance}");
                }
            }

            return responses;
        }

        private static IEnumerable<(object instance, int priority, MethodInfo method)> GetProvidersFor(string requestType)
        {
            return _providers
                .Where(x => x.requestType == requestType)
                .Select(x => (x.instance, x.priority, x.method));
        }

        private static IEnumerable<(string requestType, int priority, MethodInfo method)> ParseObject(object obj)
        {
            var methods = obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                // Providers must return something (not void)
                if (method.ReturnType == typeof(void))
                    continue;

                var attributes = method.GetCustomAttributes<ProviderAttribute>();
                if (!attributes.Any())
                    continue;

                foreach (var attribute in attributes)
                {
                    if (attribute == null || string.IsNullOrEmpty(attribute.RequestType))
                        continue;

                    yield return (attribute.RequestType, attribute.Priority, method);
                }
            }
        }
    }
}
