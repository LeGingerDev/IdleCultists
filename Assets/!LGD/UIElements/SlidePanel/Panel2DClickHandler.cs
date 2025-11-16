using LGD.Core;
using LGD.Core.Events;
using LGD.InteractionSystem;
using UnityEngine;

public class Panel2DClickHandler : BaseBehaviour
{
    public PanelType targetPanelType;
    public bool isOpening;

    [Topic(InteractionEventIds.ON_MOUSE_UP)]
    public void OnMouseUpEvent(object sender, InteractionData interactionData)
    {
        if (!RequestBus.Request<bool>(InputRequestIds.CAN_PROCESS_WORLD_CLICK, this))
        {
            DebugManager.Log("[UI] [Altar] Click blocked - pickup in progress");
            return;
        }

        if (interactionData == null || !interactionData.HasTarget)
            return; 

        if (transform != interactionData.Target)
            return;

        if (isOpening)
            Publish(PanelEventIds.ON_PANEL_OPEN_REQUESTED, targetPanelType);
        else
            Publish(PanelEventIds.ON_PANEL_CLOSE_REQUESTED, targetPanelType);
    }


}
