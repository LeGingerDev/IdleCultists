using LGD.Core.Singleton;
using LGD.Utilities.Extensions;
using System.Collections.Generic;

public class NameRegistry : MonoSingleton<NameRegistry>
{
    public List<string> cultistNames = new List<string>()
    {
    "Bob",
    "Pepsi",
    "Keith",
    "Margarine",
    "Steve",
    "Dorito",
    "Gary",
    "Spatula",
    "Linda",
    "Wifi",
    "Derek",
    "Crouton",
    "Barbara",
    "Tupperware",
    "Greg",
    "Placeholder",
    "Susan",
    "Bluetooth",
    "Jeff",
    "Napkin"
    };

    public string GetRandomName()
    {
        return cultistNames.Random().ToString();
    }
}