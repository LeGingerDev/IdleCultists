using UnityEngine;

public class SwingDoor : Door
{
    private const string ON_DOOR_OPEN_ANIM = "Opening";

    [SerializeField]
    private Transform _matchingDoor;

    [SerializeField]
    private Animator _doorAnim;

    public override void OpenDoor(float duration)
    {
        base.OpenDoor(duration);
        _doorAnim.Play("Opening");
    }

    public Transform MatchingDoor => _matchingDoor;
}
