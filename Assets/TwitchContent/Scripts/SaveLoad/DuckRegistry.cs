public class DuckRegistry : RegistryProviderBase<DuckBlueprint>
{
    public override DuckBlueprint GetItemById(string id)
    {
        return GetAllItems().Find(item => item.id == id);
    }
}

