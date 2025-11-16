using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySaveProvider : SaveLoadProviderBase<EntityRuntimeData>
{
    protected override string GetSaveFileName()
    {
        return "entities.json";
    }

    protected override IEnumerator CreateDefault()
    {
        _data.Clear();

        // Entities are dynamic - start with empty list
        // No registry to sync with, entities are spawned at runtime
        Debug.Log($"<color=green>Created default entity data:</color> 0 entities");

        yield return null;
    }
}