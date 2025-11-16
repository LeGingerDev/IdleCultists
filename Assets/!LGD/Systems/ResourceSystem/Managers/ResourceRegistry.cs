using LGD.ResourceSystem.Models;
using System.Linq;

namespace LGD.ResourceSystem.Managers
{
    public class ResourceRegistry : RegistryProviderBase<Resource>
    {
        public override Resource GetItemById(string id) => GetAllItems().FirstOrDefault(item => item.id == id);
    }
}