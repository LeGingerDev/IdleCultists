using LGD.Core.Events;
using LGD.Core.Singleton;
using LGD.ResourceSystem.Managers;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomManager : MonoSingleton<RoomManager>, IStatProvider
{
    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private Dictionary<string, RoomRuntimeData> _rooms = new Dictionary<string, RoomRuntimeData>();

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private Dictionary<string, RoomController> _roomControllers = new Dictionary<string, RoomController>();

    private RoomRegistry _roomRegistry;
    private SaveLoadProviderBase<RoomRuntimeData> _saveProvider;
    private bool _isInitialized = false;

    protected override void Awake()
    {
        base.Awake();
        CacheAllRoomControllers();
    }

    private void Start()
    {
        StartCoroutine(InitializeRooms());
    }

    #region Initialization

    /// <summary>
    /// Cache all RoomControllers in the scene and store by roomId.
    /// </summary>
    public void CacheAllRoomControllers()
    {
        RoomController[] allRooms = FindObjectsByType<RoomController>(FindObjectsSortMode.None);

        foreach (RoomController roomController in allRooms)
        {
            if (roomController.Blueprint != null)
            {
                string roomId = roomController.Blueprint.roomId;
                _roomControllers[roomId] = roomController;
            }
            else
            {
                Debug.LogError($"RoomController on {roomController.gameObject.name} has no blueprint assigned!", roomController);
            }
        }

        Debug.Log($"<color=cyan>Cached {_roomControllers.Count} RoomController(s)</color>");
    }

    /// <summary>
    /// Phase 1: Silent load - loads room data from save file, creates missing data for scene rooms
    /// </summary>
    private IEnumerator InitializeRooms()
    {
        _roomRegistry = RegistryManager.Instance.GetRegistry<RoomBlueprint>() as RoomRegistry;
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<RoomRuntimeData>();

        if (_saveProvider == null)
        {
            Debug.LogError("Room save provider not found! Rooms will not be saved. Make sure RoomSaveProvider is in the scene.");
            yield break;
        }

        // Load saved room data
        yield return _saveProvider.Load();
        List<RoomRuntimeData> loadedRooms = _saveProvider.GetData();

        // Populate _rooms dictionary from loaded data
        foreach (RoomRuntimeData roomData in loadedRooms)
        {
            _rooms[roomData.roomId] = roomData;
        }

        // Ensure ALL rooms in the scene have runtime data (create missing ones)
        bool needsSave = false;
        foreach (var kvp in _roomControllers)
        {
            string roomId = kvp.Key;

            if (!_rooms.ContainsKey(roomId))
            {
                // Room exists in scene but not in save - create new data
                RoomRuntimeData newData = new RoomRuntimeData(roomId);
                _rooms[roomId] = newData;
                needsSave = true;
                Debug.Log($"<color=yellow>Created new runtime data for room:</color> {roomId}");
            }
        }

        // Auto-save if we created new room data
        if (needsSave)
        {
            SaveImmediately();
        }

        Debug.Log($"<color=cyan>Room Manager initialized:</color> {_rooms.Count} room(s) loaded (silent mode - no visual changes)");

        _isInitialized = true;

        // Notify all RoomControllers that initialization is complete
        foreach (RoomController controller in _roomControllers.Values)
        {
            controller.OnManagerInitialized();
        }
    }

    /// <summary>
    /// Phase 2: Visual restoration - instantly shows unlocked rooms and opens doors
    /// Call this AFTER game scene is ready via RestoreUnlockedRoomsTask
    /// </summary>
    public IEnumerator RestoreUnlockedRooms()
    {
        List<RoomRuntimeData> unlockedRooms = GetAllUnlockedRooms();

        if (unlockedRooms.Count == 0)
        {
            Debug.Log("<color=cyan>No unlocked rooms to restore</color>");
            yield break;
        }

        Debug.Log($"<color=yellow>Restoring {unlockedRooms.Count} unlocked room(s)...</color>");

        int restoredCount = 0;

        foreach (RoomRuntimeData roomData in unlockedRooms)
        {
            // Get the RoomController (already cached)
            if (!_roomControllers.TryGetValue(roomData.roomId, out RoomController roomController))
            {
                Debug.LogWarning($"RoomController not found for room: {roomData.roomId}");
                continue;
            }

            // Instantly show the room (no animation)
            roomController.ShowRoomInstant();

            // Find and instantly open the door
            Door door = FindDoorForRoom(roomData.roomId);
            if (door != null)
            {
                door.OpenDoor(0f); // Instant snap
            }

            // Publish restoration event (visual-only, no gameplay logic)
            Publish(RoomEventIds.ON_ROOM_RESTORED, roomData);

            restoredCount++;

            yield return null; // Small yield to prevent hitches
        }

        Debug.Log($"<color=green>Room restoration complete:</color> {restoredCount} room(s) restored");
    }

    public bool IsInitialized() => _isInitialized;

    #endregion

    #region Runtime Data Access

    /// <summary>
    /// Get runtime data for a room. Returns null if not found.
    /// </summary>
    public RoomRuntimeData GetRoomRuntimeData(string roomId)
    {
        _rooms.TryGetValue(roomId, out RoomRuntimeData data);
        return data;
    }

    /// <summary>
    /// Get the RoomController for a room. Returns null if not found.
    /// </summary>
    public RoomController GetRoomController(string roomId)
    {
        _roomControllers.TryGetValue(roomId, out RoomController controller);
        return controller;
    }

    public RoomController GetRoomController(RoomBlueprint blueprint)
    {
        return GetRoomController(blueprint.roomId);
    }

    #endregion

    #region Unlock Management

    public void UnlockRoom(RoomBlueprint blueprint) => UnlockRoom(blueprint.roomId);

    public bool UnlockRoom(string roomId)
    {
        RoomBlueprint blueprint = _roomRegistry.GetItemById(roomId);
        if (blueprint == null)
        {
            Debug.LogError($"Room blueprint not found: {roomId}");
            return false;
        }

        if (!_rooms.TryGetValue(roomId, out RoomRuntimeData runtimeData))
        {
            Debug.LogError($"Room runtime data not found: {roomId}");
            return false;
        }

        if (runtimeData.isUnlocked)
        {
            Debug.LogWarning($"Room {roomId} is already unlocked!");
            return false;
        }

        // Check prerequisites
        if (!blueprint.CheckPrerequisites())
        {
            Debug.LogWarning($"Prerequisites not met for room: {roomId}");
            return false;
        }

        // Check cost
        var cost = blueprint.GetUnlockCost();
        if (!ResourceManager.Instance.CanSpend(cost))
        {
            Debug.LogWarning($"Cannot afford to unlock room: {roomId}");
            return false;
        }

        // Deduct cost
        ResourceManager.Instance.RemoveResource(cost);

        // Unlock the room
        runtimeData.Unlock();

        // Save immediately (rooms don't unlock often)
        SaveImmediately();

        // Publish unlock event (triggers fancy animation sequence)
        Publish(RoomEventIds.ON_ROOM_UNLOCKED, runtimeData);

        Debug.Log($"Unlocked room: {blueprint.displayName}");
        return true;
    }

    #endregion

    #region Queries

    public bool IsRoomUnlocked(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out RoomRuntimeData data))
            return data.isUnlocked;
        return false;
    }

    public List<RoomRuntimeData> GetAllUnlockedRooms()
    {
        return _rooms.Values.Where(r => r.isUnlocked).ToList();
    }

    public int GetUnlockedRoomCount()
    {
        return _rooms.Values.Count(r => r.isUnlocked);
    }

    public List<RoomRuntimeData> GetAllRooms()
    {
        return new List<RoomRuntimeData>(_rooms.Values);
    }

    #endregion

    #region Helpers

    private Door FindDoorForRoom(string roomId)
    {
        Door[] allDoors = FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return allDoors.FirstOrDefault(door => door.ConnectedRoom != null && door.ConnectedRoom.roomId == roomId);
    }

    #endregion

    #region Save Methods

    /// <summary>
    /// Set the room data in the save provider and mark dirty.
    /// Call this whenever room data changes.
    /// </summary>
    private void SetDataAndMarkDirty()
    {
        if (_saveProvider != null)
        {
            // Convert dictionary values to list for saving
            List<RoomRuntimeData> roomList = new List<RoomRuntimeData>(_rooms.Values);
            StartCoroutine(_saveProvider.SetData(roomList));
            _saveProvider.MarkDirty();
        }
    }

    /// <summary>
    /// Save immediately (rooms don't change often, so immediate save is fine).
    /// </summary>
    private void SaveImmediately()
    {
        if (_saveProvider != null)
        {
            StartCoroutine(SaveRoomDataCoroutine());
        }
    }

    private IEnumerator SaveRoomDataCoroutine()
    {
        // Convert dictionary values to list for saving
        List<RoomRuntimeData> roomList = new List<RoomRuntimeData>(_rooms.Values);
        yield return _saveProvider.SetData(roomList);
        yield return _saveProvider.Save();
    }

    public IEnumerator ManualSave()
    {
        yield return SaveRoomDataCoroutine();
    }

    #endregion

    #region IStatProvider Implementation

    public List<StatModifier> GetModifiersForStat(StatType statType)
    {
        // Rooms themselves don't provide modifiers directly
        // They contain upgrades that provide modifiers
        return new List<StatModifier>();
    }

    #endregion

    #region Debug Helpers

    [Button("Log All Rooms"), FoldoutGroup("Debug")]
    private void DebugLogAllRooms()
    {
        Debug.Log($"=== ALL ROOMS ({_rooms.Count}) ===");
        foreach (var kvp in _rooms)
        {
            Debug.Log($"Room: {kvp.Key} | Unlocked: {kvp.Value.isUnlocked} | Time: {kvp.Value.timeUnlocked}");
        }
    }

    [Button("Unlock All Rooms"), FoldoutGroup("Debug")]
    private void DebugUnlockAllRooms()
    {
        foreach (var kvp in _rooms)
        {
            if (!kvp.Value.isUnlocked)
            {
                kvp.Value.Unlock();
                Debug.Log($"Force unlocked: {kvp.Key}");
            }
        }
        SaveImmediately();
    }

    [Button("Manual Save"), FoldoutGroup("Debug")]
    private void DebugManualSave()
    {
        StartCoroutine(ManualSave());
    }

    [Button("Force Restore Rooms"), FoldoutGroup("Debug")]
    private void DebugForceRestore()
    {
        StartCoroutine(RestoreUnlockedRooms());
    }

    #endregion
}