using LGD.Tasks;
using System.Collections;
using UnityEngine;

public class OpenDoorTask : TaskBase
{
    public override IEnumerator ExecuteInternal()
    {
        Door door = RoomUnlockManager.Instance.GetCurrentUnlockedDoor();
        float doorDuration = HandleDoorOpening(door);
        HandleDoorOpening(door);
        yield return new WaitForSeconds(doorDuration);


    }

    public float HandleDoorOpening(Door door)
    {
        if (door is SwingDoor swingDoor)
        {
            StartCoroutine(HandleSwingDoor(swingDoor));
            return 3f;
        }
        else if (door is WallDoor wallDoor)
        {
            HandleWallDoor(wallDoor);
            return 4.2f;
        }
        return 4.2f;
    }

    public IEnumerator HandleSwingDoor(SwingDoor door)
    {
        door.OpenDoor(-1f);
        yield return new WaitForSeconds(2f);
        CameraController2D.Instance.ZoomToSize(0.08f, true, true, 0.5f);
        yield return new WaitForSeconds(0.5f);
        CameraController2D.Instance.PanToTarget(door.MatchingDoor, -1);
    }

    public void HandleWallDoor(WallDoor wallDoor)
    {
        wallDoor.OpenDoor(4f);
        CameraController2D.Instance.ShakeCamera();
    }

}