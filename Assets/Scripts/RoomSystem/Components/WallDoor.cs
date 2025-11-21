using Audio.Core;
using Audio.Managers;
using DG.Tweening;
using UnityEngine;

public class WallDoor : Door
{

    [SerializeField]
    private Transform _movePoint;
    private Vector2 _originalPos;
    void Start()
    {
        _originalPos = _movePoint.localPosition;
    }

    public override void OpenDoor(float duration, bool playWithAudio = true)
    {
        base.OpenDoor(duration, playWithAudio);
        if(playWithAudio)
            AudioManager.Instance.PlaySFX(AudioConstIds.DOOR_OPENING);
        _movePoint.DOLocalMoveY(2.1f, duration).SetEase(Ease.Linear);
    }

    public override void CloseDoor(float duration, bool playWithAudio = true)
    {
        base.CloseDoor(duration, playWithAudio);
        _movePoint.DOLocalMove(_originalPos, duration).SetEase(Ease.Linear);
    }
}
