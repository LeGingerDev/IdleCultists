using Audio.Core;
using Audio.Managers;
using DG.Tweening;
using UnityEngine;

public class WallDoor : Door
{

    [SerializeField]
    private Transform _movePoint;

    public override void OpenDoor(float duration)
    {
        base.OpenDoor(duration);
        AudioManager.Instance.PlaySFX(AudioConstIds.DOOR_OPENING);
        _movePoint.DOLocalMoveY(2.1f, duration).SetEase(Ease.Linear);
    }
}
