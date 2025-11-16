using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PurchasableEvent_NAME", menuName = "LGD/Idle Cultist/Purchasable/Event Announcement")]
public class PurchasableEventBlueprint : PurchasableBlueprint
{
    [SerializeField, FoldoutGroup("Event"), EventIdDropdown]
    private string _eventId;

    public override string GetContextId()
    {
        return "PurchasableEvent";
    }

    public override void HandlePurchase(PurchasableRuntimeData runtimeData)
    {
        ServiceBus.Publish(_eventId, this);
    }

}
