using LGD.Tasks;
using System.Collections;
using UnityEngine;

/// <summary>
/// Task for restoring unlocked rooms during game load.
/// Instantly shows room content and opens doors without animations.
/// Call this after entities are restored but before gameplay begins.
/// </summary>
public class RestoreUnlockedRoomsTask : TaskBase
{
    public override IEnumerator ExecuteInternal()
    {
        RoomManager.Instance.CacheAllRoomControllers();

        Debug.Log("<color=yellow>[RestoreUnlockedRoomsTask] Starting room restoration...</color>");

        // Wait for RoomManager to finish silent load
        yield return new WaitUntil(() => RoomManager.Instance.IsInitialized());

        // Restore all unlocked rooms (instant visual changes, no animations)
        yield return RoomManager.Instance.RestoreUnlockedRooms();

        Debug.Log("<color=green>[RestoreUnlockedRoomsTask] Room restoration complete!</color>");
    }
}