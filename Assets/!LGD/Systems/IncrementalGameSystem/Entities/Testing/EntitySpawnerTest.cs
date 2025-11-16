using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;

public class EntitySpawnerTest : BaseBehaviour
{
    [SerializeField, FoldoutGroup("Settings")]
    private Transform _spawnPoint;

    [Button("Spawn Entity"), FoldoutGroup("Test")]
    private void SpawnEntity(EntityBlueprint blueprint)
    {
        if (blueprint == null)
        {
            Debug.LogError("No blueprint assigned!");
            return;
        }

        if (blueprint.prefab == null)
        {
            Debug.LogError("Blueprint has no prefab assigned!");
            return;
        }

        Vector3 spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;

        EntityController entity = Instantiate(blueprint.prefab, spawnPosition, Quaternion.identity);
        entity.Initialise(blueprint);

        Debug.Log($"Spawned {blueprint.displayName} at {spawnPosition}");
    }

    [Topic(SummoningEventIds.ON_SUMMONING_STARTED)]
    public void OnSummonEvent(object sender, EntityBlueprint blueprint)
    {
        SpawnEntity(blueprint);
    }
}