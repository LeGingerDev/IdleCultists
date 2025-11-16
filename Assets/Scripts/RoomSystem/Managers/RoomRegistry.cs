public class RoomRegistry : RegistryProviderBase<RoomBlueprint>
{
    public override RoomBlueprint GetItemById(string id)
    {
        return _items.Find(room => room.roomId == id);
    }
}