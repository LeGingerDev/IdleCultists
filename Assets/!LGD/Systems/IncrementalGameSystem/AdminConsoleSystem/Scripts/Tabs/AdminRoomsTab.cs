using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Admin tab for managing rooms (lock/unlock)
/// </summary>
public class AdminRoomsTab : AdminTabBase
{
    private int _selectedRoomIndex = 0;
    private List<RoomRuntimeData> _allRooms = new List<RoomRuntimeData>();
    private string[] _roomNames = new string[0];

    public override void RefreshData()
    {
        if (RoomManager.Instance != null)
        {
            _allRooms = RoomManager.Instance.GetAllRooms()
                .Where(r => r.roomId != "room-of-devotion") // Exclude room-of-devotion
                .ToList();
            _roomNames = _allRooms.Select(r =>
            {
                string status = r.isUnlocked ? "[UNLOCKED]" : "[LOCKED]";
                return $"{status} {r.roomId}";
            }).ToArray();

            if (_selectedRoomIndex >= _roomNames.Length)
                _selectedRoomIndex = 0;
        }
    }

    public override void DrawTab()
    {
        GUILayout.Label("Room Management", HeaderStyle);
        GUILayout.Space(5);

        if (_roomNames.Length == 0)
        {
            GUILayout.Label("No rooms found. Make sure RoomManager is initialized.");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Select Room:", GUILayout.Width(100));
        _selectedRoomIndex = GUILayout.SelectionGrid(_selectedRoomIndex, _roomNames, 1, GUILayout.Height(200));
        GUILayout.EndHorizontal();

        if (_selectedRoomIndex < _allRooms.Count)
        {
            RoomRuntimeData selectedRoom = _allRooms[_selectedRoomIndex];

            GUILayout.Space(10);
            GUILayout.Label($"Room ID: {selectedRoom.roomId}");
            GUILayout.Label($"Status: {(selectedRoom.isUnlocked ? "UNLOCKED" : "LOCKED")}");

            if (selectedRoom.isUnlocked)
            {
                GUILayout.Label($"Unlocked At: {selectedRoom.timeUnlocked}");
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (selectedRoom.isUnlocked)
            {
                if (GUILayout.Button("Lock Room", ButtonStyle, GUILayout.Height(40)))
                {
                    LockRoom(selectedRoom);
                }
            }
            else
            {
                if (GUILayout.Button("Unlock Room", ButtonStyle, GUILayout.Height(40)))
                {
                    UnlockRoom(selectedRoom);
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    private void UnlockRoom(RoomRuntimeData room)
    {
        room.Unlock();

        // Handle doors connected to this room
        HandleDoorsForRoom(room.roomId, true);

        Console.StartCoroutine(RoomManager.Instance.ManualSave());
        Console.StartCoroutine(RoomManager.Instance.RestoreUnlockedRooms());
        RefreshData();
        DebugManager.Log($"[Admin] Unlocked room: {room.roomId}");
    }

    private void LockRoom(RoomRuntimeData room)
    {
        room.isUnlocked = false;

        // Hide the room visually
        RoomController controller = RoomManager.Instance.GetRoomController(room.roomId);
        if (controller != null)
        {
            controller.HideRoom();
        }

        // Handle doors connected to this room
        HandleDoorsForRoom(room.roomId, false);

        Console.StartCoroutine(RoomManager.Instance.ManualSave());
        RefreshData();
        DebugManager.Log($"[Admin] Locked room: {room.roomId}");
    }

    private void HandleDoorsForRoom(string roomId, bool isUnlocking)
    {
        // Find all doors in the scene
        Door[] allDoors = Object.FindObjectsOfType<Door>();

        foreach (Door door in allDoors)
        {
            if (door.ConnectedRoom != null && door.ConnectedRoom.roomId == roomId)
            {
                if (isUnlocking)
                {
                    // Open the door (disable collider)
                    door.OpenDoor(0f, false);
                }
                else
                {
                    door.CloseDoor(0, false);
                }
            }
        }
    }
}
