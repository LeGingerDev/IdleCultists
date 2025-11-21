using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Admin tab for development tools and utilities
/// </summary>
public class AdminToolsTab : AdminTabBase
{
    private float _timeScale = 1f;

    public override void DrawTab()
    {
        GUILayout.Label("Development Tools", HeaderStyle);
        GUILayout.Space(10);

        // Time Scale
        DrawSection("Time Scale", () =>
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current: {Time.timeScale:F2}x", GUILayout.Width(150));
            _timeScale = GUILayout.HorizontalSlider(_timeScale, 0.1f, 10f, GUILayout.Width(300));
            if (GUILayout.Button("Apply", GUILayout.Width(80)))
            {
                Time.timeScale = _timeScale;
            }
            if (GUILayout.Button("Reset", GUILayout.Width(80)))
            {
                Time.timeScale = 1f;
                _timeScale = 1f;
            }
            GUILayout.EndHorizontal();
        });

        // Save/Load Tools
        DrawSection("Save/Load", () =>
        {
            if (GUILayout.Button("Force Save All", ButtonStyle, GUILayout.Height(40)))
            {
                ForceSaveAll();
            }
            if (GUILayout.Button("Delete All Saves (Dangerous!)", ButtonStyle, GUILayout.Height(40)))
            {
                DeleteAllSaves();
            }
        });

        // Room Tools
        DrawSection("Room Tools", () =>
        {
            if (GUILayout.Button("Unlock All Rooms", ButtonStyle, GUILayout.Height(40)))
            {
                UnlockAllRooms();
            }
            if (GUILayout.Button("Lock All Rooms", ButtonStyle, GUILayout.Height(40)))
            {
                LockAllRooms();
            }
        });

        // General Info
        DrawSection("Game Info", () =>
        {
            GUILayout.Label($"FPS: {(1f / Time.smoothDeltaTime):F0}");
            GUILayout.Label($"Time Scale: {Time.timeScale:F2}x");
            GUILayout.Label($"Total Entities: {EntityManager.Instance?.GetEntityCount() ?? 0}");
            GUILayout.Label($"Unlocked Rooms: {RoomManager.Instance?.GetUnlockedRoomCount() ?? 0}");
        });
    }

    private void ForceSaveAll()
    {
        Console.StartCoroutine(SaveLoadProviderManager.Instance.SaveAll());
        DebugManager.Log("[Admin] Forced save all");
    }

    private void DeleteAllSaves()
    {
        SaveLoadProviderManager.Instance.DeleteAllSaves();
        DebugManager.Warning("[Admin] Deleted all saves! Reloading scene...");
        Console.StartCoroutine(ReloadSceneAfterDelay());
    }

    private IEnumerator ReloadSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void UnlockAllRooms()
    {
        var allRooms = RoomManager.Instance.GetAllRooms();
        foreach (var room in allRooms)
        {
            // Skip room-of-devotion as it's always unlocked
            if (room.roomId == "room-of-devotion")
                continue;

            if (!room.isUnlocked)
            {
                room.Unlock();
                HandleDoorsForRoom(room.roomId, true);
            }
        }
        Console.StartCoroutine(RoomManager.Instance.ManualSave());
        Console.StartCoroutine(RoomManager.Instance.RestoreUnlockedRooms());
        DebugManager.Log("[Admin] Unlocked all rooms");
    }

    private void LockAllRooms()
    {
        var allRooms = RoomManager.Instance.GetAllRooms();
        foreach (var room in allRooms)
        {
            // Skip room-of-devotion as it's always unlocked
            if (room.roomId == "room-of-devotion")
                continue;

            room.isUnlocked = false;

            RoomController controller = RoomManager.Instance.GetRoomController(room.roomId);
            if (controller != null)
            {
                controller.HideRoom();
            }

            HandleDoorsForRoom(room.roomId, false);
        }
        Console.StartCoroutine(RoomManager.Instance.ManualSave());
        DebugManager.Log("[Admin] Locked all rooms");
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
