using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSaveProvider : SaveLoadProviderBase<RoomRuntimeData>
{
    protected override string GetSaveFileName()
    {
        return "rooms.json";
    }

    protected override IEnumerator CreateDefault()
    {
        _data.Clear();

        // Get all rooms from the registry
        RoomRegistry roomRegistry = RegistryManager.Instance.GetRegistry<RoomBlueprint>() as RoomRegistry;
        List<RoomBlueprint> allRoomBlueprints = roomRegistry.GetAllItems();

        // Create locked runtime data for each room blueprint
        foreach (RoomBlueprint blueprint in allRoomBlueprints)
        {
            RoomRuntimeData runtimeData = new RoomRuntimeData(blueprint.roomId);
            _data.Add(runtimeData);
        }

        Debug.Log($"Created default room data: {_data.Count} room(s) (all locked)");

        yield return null;
    }
}