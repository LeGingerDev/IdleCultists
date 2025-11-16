using LGD.Core.Singleton;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RegistryManager : MonoSingleton<RegistryManager>
{
    private Dictionary<Type, object> _registries = new Dictionary<Type, object>();

    
    public void RegisterRegistry<T>(RegistryProviderBase<T> registry) where T : ScriptableObject
    {
        Type type = typeof(T);
        if (_registries.ContainsKey(type))
        {
            DebugManager.Warning($"[IncrementalGame] Registry already exists for {type.Name}");
            return;
        }
        _registries.Add(type, registry);
        DebugManager.Log($"[IncrementalGame] Registered {type.Name} registry");
    }

    public RegistryProviderBase<T> GetRegistry<T>() where T : ScriptableObject
    {
        Type type = typeof(T);
        if (_registries.TryGetValue(type, out object registry))
        {
            return registry as RegistryProviderBase<T>;
        }

        DebugManager.Error($"[IncrementalGame] No registry found for {type.Name}");
        return null;
    }
}
