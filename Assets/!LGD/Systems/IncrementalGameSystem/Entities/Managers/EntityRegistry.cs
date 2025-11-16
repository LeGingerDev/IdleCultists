using UnityEngine;

public class EntityRegistry : RegistryProviderBase<EntityBlueprint>
{
    public override EntityBlueprint GetItemById(string id)
    {
        return _items.Find(item => item.id == id);
    }
}
