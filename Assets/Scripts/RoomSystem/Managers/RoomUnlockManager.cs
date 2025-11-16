using LGD.Core;
using LGD.Core.Events;
using LGD.Core.Singleton;
using LGD.Tasks;
using NUnit.Framework;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomUnlockManager : MonoSingleton<RoomUnlockManager>
{
    [SerializeField]
    private TaskManager _taskManager;

    private RoomRuntimeData _currentRoomData;
    public RoomRuntimeData CurrentRoomData => _currentRoomData;

    [SerializeField, ReadOnly]
    private List<Door> _doorList = new();
    
    protected override void Awake()
    {
        base.Awake();
        _doorList.AddRange(FindObjectsByType<Door>(FindObjectsInactive.Include,sortMode: FindObjectsSortMode.None));
    }

    [Topic(RoomEventIds.ON_ROOM_UNLOCKED)]
    public void OnRoomUnlocked(object sender, RoomRuntimeData data)
    {
        _currentRoomData = data;
        Publish(PanelEventIds.ON_PANEL_FORCE_CLOSE_ALL);
        StartCoroutine(RoomUnlock());
    }

    public IEnumerator RoomUnlock()
    {
        yield return _taskManager.Execute();
        _currentRoomData = null;
    }

    public Door GetCurrentUnlockedDoor() => GetDoorById(_currentRoomData.roomId);

    public Door GetDoorById(string roomId)
    {
        return _doorList.FirstOrDefault(door => door.ConnectedRoom.roomId == roomId && door.enabled);
    }

    public RoomController GetCurrentUnlockedRoom() => RoomManager.Instance.GetRoomController(_currentRoomData.roomId);
}
