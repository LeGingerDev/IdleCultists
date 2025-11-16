using LGD.Utilities.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SummonLocationPair
{
    public EntityBlueprint entity;
    public List<Transform> spawnLocations;

    public Transform GetRandomSpawnLocation() => spawnLocations.Random();
}