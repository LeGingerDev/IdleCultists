using System.Linq;

public class AchievementManagerRegistry : RegistryProviderBase<AchievementData>
{
    public override AchievementData GetItemById(string id) => GetAllItems().FirstOrDefault(item => item.id == id);
}