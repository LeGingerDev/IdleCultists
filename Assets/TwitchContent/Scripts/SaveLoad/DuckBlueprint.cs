using LGD.Gameplay.Polish;
using UnityEngine;

[CreateAssetMenu(fileName = "DuckBlueprint", menuName = "LGD/TwitchContent/DuckBlueprint")]
public class DuckBlueprint : ScriptableObject
{
    public string id;
    public RubberDuckController duckPrefab;
}

