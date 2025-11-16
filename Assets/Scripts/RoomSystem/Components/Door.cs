using LGD.Core;
using LGD.Core.Events;
using LGD.InteractionSystem;
using LGD.ResourceSystem.Extensions;
using LGD.UIElements.ConfirmPopup;
using Sirenix.OdinInspector;
using ToolTipSystem.Components;
using UnityEngine;

public class Door : WorldToolTipBase<RoomBlueprint>
{
    [SerializeField]
    private Transform _doorCameraAimTarget;
    public Transform DoorCameraAimTarget => _doorCameraAimTarget;

    [SerializeField]
    private RoomBlueprint _connectedRoom;
    public RoomBlueprint ConnectedRoom => _connectedRoom;

    public override RoomBlueprint Data => ConnectedRoom;
    private bool isOpen = false;
    private Collider2D col;
    
    private void Awake()
    {
        col = GetComponentInChildren<Collider2D>();
    }

    protected override void OnHoverEnter()
    {
        base.OnHoverEnter();
    }

    [Topic(InteractionEventIds.ON_MOUSE_UP)]
    public void OnMouseUpEvent(object sender, InteractionData data)
    {
        if (data == null || data.Target != this.transform)
            return;

        if (!_connectedRoom.CanAfford())
            return;

        HandleDoorPress();
        
        //Handle door interaction logic here (e.g., open door, transition to another room, etc.)
    }

    public void HandleDoorPress()
    {
        if (_connectedRoom == null)
        {
            Debug.LogError("Connected room is not assigned on door: " + gameObject.name);
            return;
        }

        ConfirmPopupData confirmData = new ConfirmPopupData()
        {
            cancelButtonText = "Cancel",
            confirmButtonText = "Purchase",
            message = $"Do you want to unlock the {_connectedRoom.displayName} for \n {_connectedRoom.GetUnlockCost().ToStringSingle()}?",
            title = $"{_connectedRoom.displayName}",
            confirmType = ConfirmPopup.ConfirmType.Normal,
            onConfirm = OnRoomPurchased,
            onCancel = OnCancel
        };

        ConfirmPopup.Instance.Open(confirmData);
    }
    public void OnRoomPurchased()
    {
        RoomManager.Instance.UnlockRoom(_connectedRoom);
    }

    public void OnCancel()
    {
        //For now do nothing. Probably won't ever do anything :D
    }

    [Topic(RoomEventIds.ON_ROOM_UNLOCKED)]
    public void OnRoomUnlocked(object sender, RoomRuntimeData data)
    {
        if (_connectedRoom == null)
            return; 

        if (data == null || data.roomId != _connectedRoom.roomId)
            return;

        col.enabled = false;
        //Do something when the connected room is unlocked, e.g., change door appearance
    }

    [Button]
    public virtual void OpenDoor(float duration)
    {
        isOpen = true;
        col.enabled = false;
    }
}
