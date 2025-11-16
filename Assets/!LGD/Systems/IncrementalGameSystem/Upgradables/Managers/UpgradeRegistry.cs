using UnityEngine;

public class UpgradeRegistry : RegistryProviderBase<UpgradeBlueprint>
{
    public override UpgradeBlueprint GetItemById(string id)
    {
        return _items.Find(upgrade => upgrade.upgradeId == id);
    }
}
