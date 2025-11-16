using DG.Tweening;
using LGD.Core;
using LGD.Core.Events;
using System.Collections;
using TMPro;
using UnityEngine;

public class RoomUnlockHeaderDisplay : BaseBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _displayText;
    [SerializeField]
    private Transform _content;
    private RoomBlueprint _currentRoom;

    private float _startYPos;
    private float _hiddenYPos;
    private void Start()
    {
        _startYPos = transform.position.y;
        _hiddenYPos = _startYPos + 400f;
        transform.position = new Vector3(transform.position.x, _hiddenYPos, transform.position.z);
    }

    [Topic(RoomEventIds.ON_ROOM_FINISHED_UNLOCKING)]
    public void OnRoomUnlocked(object sender, RoomController roomController)
    {
        RoomBlueprint roomBlueprint = roomController.Blueprint;
        _currentRoom = roomBlueprint;
        _displayText.text = $"{roomBlueprint.displayName} Unlocked!";
        StartCoroutine(RoomUnlockDisplay());
    }

    public IEnumerator RoomUnlockDisplay()
    {
        _content.DOMoveY(_startYPos, 0.25f).From(_hiddenYPos).SetEase(Ease.OutCubic);
        yield return new WaitForSeconds(3f);
        _content.DOMoveY(_hiddenYPos, 0.25f).SetEase(Ease.InCubic);
        _currentRoom = null;
    }

}
