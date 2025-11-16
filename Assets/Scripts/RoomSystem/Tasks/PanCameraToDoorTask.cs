using LGD.Tasks;
using System.Collections;
using UnityEngine;

public class PanCameraToDoorTask : TaskBase
{
    [SerializeField]
    private float _taskDuration;

    public override IEnumerator ExecuteInternal()
    {
        Door door = RoomUnlockManager.Instance.GetCurrentUnlockedDoor();
        CameraController2D.Instance.ToggleIsLocked(true);
        CameraController2D.Instance.ZoomToSizeAnimated(2.5f, 1f);
        CameraController2D.Instance.PanToTarget(door.DoorCameraAimTarget);
        yield return new WaitForSeconds(_taskDuration);
    }

}
