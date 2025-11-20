using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Admin tab for viewing zones and their assigned entities
/// </summary>
public class AdminZonesTab : AdminTabBase
{
    private List<ZoneBehaviorBase> _allZones = new List<ZoneBehaviorBase>();

    public override void RefreshData()
    {
        // Find all zone behaviors in the scene
        _allZones = new List<ZoneBehaviorBase>(Object.FindObjectsOfType<ZoneBehaviorBase>());
    }

    public override void DrawTab()
    {
        GUILayout.Label("Zone Management", HeaderStyle);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", ButtonStyle))
        {
            RefreshData();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (_allZones.Count == 0)
        {
            GUILayout.Label("No zones found in scene.");
            return;
        }

        GUILayout.Label($"Total Zones: {_allZones.Count}");

        GUILayout.Space(10);

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(450));

        foreach (var zone in _allZones)
        {
            if (zone == null) continue;

            GUILayout.BeginVertical(BoxStyle);

            // Zone info
            GUILayout.Label($"{zone.GetType().Name}", HeaderStyle);

            // Use reflection to get zone ID and type
            var zoneIdField = zone.GetType().GetField("_zoneId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var zoneTypeField = zone.GetType().GetField("_zoneType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (zoneIdField != null)
            {
                string zoneId = (string)zoneIdField.GetValue(zone);
                GUILayout.Label($"Zone ID: {zoneId}");

                // Get entities assigned to this zone
                if (EntityManager.Instance != null)
                {
                    var assignedEntities = EntityManager.Instance.GetEntitiesInZone(zoneId);
                    GUILayout.Label($"Assigned Entities: {assignedEntities.Count}");

                    if (assignedEntities.Count > 0)
                    {
                        GUILayout.Label("Entities:", SubHeaderStyle);
                        foreach (var entity in assignedEntities)
                        {
                            GUILayout.Label($"  - {entity.entityName} ({entity.blueprintId})");
                        }
                    }
                }
            }

            if (zoneTypeField != null)
            {
                var zoneType = zoneTypeField.GetValue(zone);
                GUILayout.Label($"Zone Type: {zoneType}");
            }

            GUILayout.Label($"GameObject: {zone.gameObject.name}");

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }
}
