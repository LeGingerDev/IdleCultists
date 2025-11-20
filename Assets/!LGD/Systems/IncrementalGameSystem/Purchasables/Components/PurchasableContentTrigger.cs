using LGD.Core;
using LGD.Core.Events;
using UnityEngine;

public class PurchasableContentTrigger : BaseBehaviour
{
    [SerializeField, Tooltip("The Purchasable you want to listen to when it was purchased.")]
    private BasePurchasable _listeningToPurchasable;

    [SerializeField]
    private GameObject _contentToToggle;

    void Start()
    {
        _contentToToggle.SetActive(_listeningToPurchasable.GetPurchaseCount() > 0);
    }

    [Topic(PurchasableEventIds.ON_PURCHASABLE_PURCHASED)]
    public void OnPurchasablePurchased(object sender, BasePurchasable blueprint, BasePurchasableRuntimeData runtimeData)
    {
        if (blueprint.purchasableId != _listeningToPurchasable.purchasableId)
            return;

        _contentToToggle.SetActive(_listeningToPurchasable.GetPurchaseCount() > 0);
    }

    public void ToggleContent(bool isActive)
    {
        if (_contentToToggle != null)
        {
            _contentToToggle.SetActive(isActive);
        }
    }
}
