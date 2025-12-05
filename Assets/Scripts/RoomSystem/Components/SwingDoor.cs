using Audio.Core;
using Audio.Managers;
using UnityEngine;

public class SwingDoor : Door
{
    private const string ON_DOOR_OPEN_ANIM = "Opening";

    [SerializeField]
    private Transform _matchingDoor;

    [SerializeField]
    private Animator _doorAnim;

    [SerializeField]
    private string _idleAnimName = "Idle";

    public override void OpenDoor(float duration, bool playWithAudio = true)
    {
        base.OpenDoor(duration, playWithAudio);
        if (playWithAudio)
            AudioManager.Instance.PlaySFX(AudioConstIds.SLDING_DOOR_OPEN, true, transform.position);
        _doorAnim.Play("Opening");
    }

    public override void CloseDoor(float duration, bool playWithAudio = true)
    {
        base.CloseDoor(duration, playWithAudio);
        _doorAnim.Play(_idleAnimName);
    }

    public Transform MatchingDoor => _matchingDoor;
}
