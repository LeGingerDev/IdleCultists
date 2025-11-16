using LGD.Core;
using Sirenix.OdinInspector;
using UnityEngine;

public class RoomController : BaseBehaviour
{
    [SerializeField]
    private Transform _cameraAimTarget;
    public Transform CameraAimTarget => _cameraAimTarget;

    [SerializeField]
    private TilemapRippleEffect _tilemapRippleEffect;
    [SerializeField]
    private GameObject _roomContent;

    [SerializeField, FoldoutGroup("Setup")]
    private RoomBlueprint _roomBlueprint;
    public RoomBlueprint Blueprint => _roomBlueprint;

    [SerializeField, ReadOnly, FoldoutGroup("Runtime")]
    private RoomRuntimeData _runtimeData;
    public RoomRuntimeData RuntimeData => _runtimeData;

    #region Initialization

    /// <summary>
    /// Called by RoomManager after initialization is complete.
    /// This is when we grab our runtime data from the manager.
    /// </summary>
    public void OnManagerInitialized()
    {
        if (_roomBlueprint == null)
        {
            Debug.LogError($"RoomController on {gameObject.name} has no blueprint assigned!", this);
            return;
        }

        // Get runtime data from RoomManager (already created and loaded)
        _runtimeData = RoomManager.Instance.GetRoomRuntimeData(_roomBlueprint.roomId);

        if (_runtimeData == null)
        {
            Debug.LogError($"RoomController on {gameObject.name} could not find runtime data for {_roomBlueprint.roomId}!", this);
            return;
        }

        // Publish room initialized event
        Publish(RoomEventIds.ON_ROOM_INITIALIZED, _runtimeData);

        // Set initial tilemap state based on unlock status
        SetInitialTilemapState();

        Debug.Log($"Room initialized: {_roomBlueprint.displayName} (Unlocked: {_runtimeData.isUnlocked})");
    }

    private void SetInitialTilemapState()
    {
        if (_tilemapRippleEffect == null)
            return;

        if (_runtimeData != null && _runtimeData.isUnlocked)
        {
            // Room is already unlocked (from save) - set to unlocked state instantly
            _tilemapRippleEffect.SetUnlockedState();
        }
        else
        {
            // Room is locked - set to locked state (default)
            _tilemapRippleEffect.SetLockedState();
        }
    }

    #endregion

    #region Unlock Management

    public bool CanUnlock()
    {
        if (_runtimeData == null || _runtimeData.isUnlocked)
            return false;

        return _roomBlueprint.CheckPrerequisites();
    }

    public void UnlockRoom()
    {
        if (_runtimeData == null)
        {
            Debug.LogError($"Cannot unlock room - runtime data is null!");
            return;
        }

        if (_runtimeData.isUnlocked)
        {
            Debug.LogWarning($"Room {_roomBlueprint.displayName} is already unlocked!");
            return;
        }

        _runtimeData.Unlock();

        Publish(RoomEventIds.ON_ROOM_UNLOCKED, _runtimeData);

        Debug.Log($"Room unlocked: {_roomBlueprint.displayName}");
    }

    #endregion

    #region Show/Hide Methods

    /// <summary>
    /// Show room with ripple effect animation (used during fresh unlock)
    /// </summary>
    public void ShowRoom()
    {
        _roomContent?.SetActive(true);

        if (_tilemapRippleEffect != null)
        {
            _tilemapRippleEffect.StartRippleEffect();
            // Ripple effect handles hiding cover automatically when complete
        }
    }

    /// <summary>
    /// Show room instantly without animation (used during save/load restoration)
    /// </summary>
    public void ShowRoomInstant()
    {
        _roomContent?.SetActive(true);

        if (_tilemapRippleEffect != null)
        {
            _tilemapRippleEffect.SetUnlockedState();
            // Sets tiles to finished rotation and hides cover instantly
        }
    }

    #endregion

    #region Debug Helpers

#if UNITY_EDITOR
    [Button("Force Unlock Room"), FoldoutGroup("Debug")]
    private void DebugUnlockRoom()
    {
        if (_runtimeData == null)
        {
            _runtimeData = RoomManager.Instance.GetRoomRuntimeData(_roomBlueprint.roomId);
        }

        UnlockRoom();
    }

    [Button("Force Lock Room"), FoldoutGroup("Debug")]
    private void DebugLockRoom()
    {
        if (_runtimeData != null)
        {
            _runtimeData.isUnlocked = false;
            Debug.Log($"Room locked: {_roomBlueprint.displayName}");
        }
    }
#endif

    #endregion
}