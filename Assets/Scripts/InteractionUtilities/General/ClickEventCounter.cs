using LGD.InteractionSystem;
using UnityEngine;

public class ClickEventCounter : ClickBase
{
    [EventIdDropdown, SerializeField]
    private string clickEventId;

    public override void OnMouseUpEvent(InteractionData clickData)
    {
        base.OnMouseUpEvent(clickData);

        Publish(clickEventId);
    }
}
