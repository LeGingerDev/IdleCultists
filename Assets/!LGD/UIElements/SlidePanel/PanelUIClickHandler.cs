using LGD.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class PanelUIClickHandler : BaseBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PanelType targetPanelType;
    public bool isOpening;

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(isOpening)
            Publish(PanelEventIds.ON_PANEL_OPEN_REQUESTED, targetPanelType);
        else
            Publish(PanelEventIds.ON_PANEL_CLOSE_REQUESTED, targetPanelType);
    }
}
