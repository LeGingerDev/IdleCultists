using LGD.Tasks;
using System.Collections;
using UnityEngine;

public class ShowRoomTask : TaskBase
{
    public override IEnumerator ExecuteInternal()
    {
        RoomController unlockedRoom = RoomUnlockManager.Instance.GetCurrentUnlockedRoom();
        unlockedRoom.ShowRoom();
        CameraController2D.Instance.PanToTarget(unlockedRoom.CameraAimTarget, 1.5f);
        CameraController2D.Instance.ZoomToSizeAnimated(5f, 1.5f);
        yield return new WaitForSeconds(1.6f);
        CameraController2D.Instance.ToggleIsLocked(false);
        Publish(RoomEventIds.ON_ROOM_FINISHED_UNLOCKING, unlockedRoom);
    }

    
}
