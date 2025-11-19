using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Admin tab for spawning and managing entities
/// </summary>
public class AdminEntitiesTab : AdminTabBase
{
    private int _selectedEntityIndex = 0;
    private List<EntityBlueprint> _allEntityBlueprints = new List<EntityBlueprint>();
    private string[] _entityNames = new string[0];
    private Vector3 _spawnPosition = Vector3.zero;

    public override void RefreshData()
    {
        var entityRegistry = RegistryManager.Instance?.GetRegistry<EntityBlueprint>() as EntityRegistry;
        if (entityRegistry != null)
        {
            _allEntityBlueprints = entityRegistry.GetAllItems();
            _entityNames = _allEntityBlueprints.Select(e => e.displayName).ToArray();

            if (_selectedEntityIndex >= _entityNames.Length)
                _selectedEntityIndex = 0;
        }
    }

    public override void DrawTab()
    {
        GUILayout.Label("Entity Spawning", HeaderStyle);
        GUILayout.Space(5);

        if (_entityNames.Length == 0)
        {
            GUILayout.Label("No entity blueprints found.");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Select Entity:", GUILayout.Width(120));
        _selectedEntityIndex = GUILayout.SelectionGrid(_selectedEntityIndex, _entityNames, 1, GUILayout.Height(200));
        GUILayout.EndHorizontal();

        if (_selectedEntityIndex < _allEntityBlueprints.Count)
        {
            EntityBlueprint selectedEntity = _allEntityBlueprints[_selectedEntityIndex];

            GUILayout.Space(10);
            GUILayout.Label($"Entity: {selectedEntity.displayName}");
            GUILayout.Label($"ID: {selectedEntity.id}");

            GUILayout.Space(10);
            GUILayout.Label("Spawn Position:");

            GUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(20));
            string xStr = GUILayout.TextField(_spawnPosition.x.ToString("F2"), GUILayout.Width(80));
            GUILayout.Label("Y:", GUILayout.Width(20));
            string yStr = GUILayout.TextField(_spawnPosition.y.ToString("F2"), GUILayout.Width(80));
            GUILayout.Label("Z:", GUILayout.Width(20));
            string zStr = GUILayout.TextField(_spawnPosition.z.ToString("F2"), GUILayout.Width(80));
            GUILayout.EndHorizontal();

            // Parse position inputs
            if (float.TryParse(xStr, out float x)) _spawnPosition.x = x;
            if (float.TryParse(yStr, out float y)) _spawnPosition.y = y;
            if (float.TryParse(zStr, out float z)) _spawnPosition.z = z;

            GUILayout.Space(5);

            if (GUILayout.Button("Set to Camera Position", ButtonStyle))
            {
                if (Camera.main != null)
                {
                    _spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 5f;
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button($"Spawn {selectedEntity.displayName}", ButtonStyle, GUILayout.Height(40)))
            {
                SpawnEntity(selectedEntity);
            }

            GUILayout.Space(10);
            GUILayout.Label($"Total Entities: {EntityManager.Instance?.GetEntityCount() ?? 0}");

            GUILayout.Space(10);

            // Bulk actions
            DrawSection("Bulk Actions", () =>
            {
                if (GUILayout.Button("Destroy All Entities", ButtonStyle, GUILayout.Height(30)))
                {
                    DestroyAllEntities();
                }
            });
        }
    }

    private void SpawnEntity(EntityBlueprint blueprint)
    {
        if (blueprint.prefab == null)
        {
            DebugManager.Error($"[Admin] Entity blueprint {blueprint.id} has no prefab!");
            return;
        }

        // Create new runtime data
        EntityRuntimeData runtimeData = new EntityRuntimeData();
        runtimeData.Initialise(blueprint);
        runtimeData.worldPosition = new SerializableVector3(_spawnPosition);

        // Spawn the GameObject
        EntityController controller = Object.Instantiate(blueprint.prefab, _spawnPosition, Quaternion.identity);
        controller.Initialise(runtimeData, blueprint);

        DebugManager.Log($"[Admin] Spawned entity: {blueprint.displayName} at {_spawnPosition}");
    }

    private void DestroyAllEntities()
    {
        EntityController[] allEntities = Object.FindObjectsOfType<EntityController>();
        int destroyedCount = allEntities.Length;

        foreach (EntityController entity in allEntities)
        {
            Object.Destroy(entity.gameObject);
        }

        DebugManager.Log($"[Admin] Destroyed {destroyedCount} entities");
    }
}
