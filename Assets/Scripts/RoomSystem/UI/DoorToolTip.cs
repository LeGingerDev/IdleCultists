using LGD.ResourceSystem.Components;
using System.Collections;
using TMPro;
using ToolTipSystem.Components;
using UnityEngine;

public class DoorToolTip : ToolTip<RoomBlueprint>
{
    [SerializeField]
    private ResourceListDisplay _resourcesDisplay;
    [SerializeField]
    private TextMeshProUGUI _roomNameText;
    [SerializeField]
    private TextMeshProUGUI _descriptionText;
    [SerializeField]
    private GameObject _cantAffordText;

    private RoomBlueprint _currentRoomData;
    private Coroutine _checkLoopCoroutine;

    public override void Show(RoomBlueprint data)
    {
        _currentRoomData = data;

        _resourcesDisplay.Initialise(data.GetUnlockCost());
        _roomNameText.text = data.displayName;
        _descriptionText.text = data.description;

        _checkLoopCoroutine = StartCoroutine(CheckLoop());
    }

    public override void HideInternal()
    {
        base.HideInternal();

        if(_checkLoopCoroutine != null)
        {
            StopCoroutine(_checkLoopCoroutine);
            _checkLoopCoroutine = null;
        }

        _currentRoomData = null;
    }

    private IEnumerator CheckLoop()
    {
        while(true)
        {
            _cantAffordText.SetActive(!_currentRoomData.CanAfford());
            yield return new WaitForSeconds(0.2f);
            yield return null;
        }

    }




}
