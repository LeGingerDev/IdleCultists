using System.Collections;
using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class PurchasableContentTrigger : BaseBehaviour
{
    public UnityEvent OnContentActivated;
    public UnityEvent OnContentDeactivated;

    [SerializeField, Tooltip("The Purchasable you want to listen to when it was purchased.")]
    private BasePurchasable _listeningToPurchasable;

    [SerializeField]
    private GameObject _contentToToggle;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f);
        ToggleContent(_listeningToPurchasable.GetPurchaseCount() > 0);
    }

    [Topic(PurchasableEventIds.ON_PURCHASABLE_PURCHASED)]
    public void OnPurchasablePurchased(object sender, BasePurchasable blueprint, BasePurchasableRuntimeData runtimeData)
    {
        if (blueprint.purchasableId != _listeningToPurchasable.purchasableId)
            return;

        ToggleContent(_listeningToPurchasable.GetPurchaseCount() > 0);
    }

[Button]
    public void DebugTest() => ToggleContent(_listeningToPurchasable.GetPurchaseCount() > 0);

    public void ToggleContent(bool isActive)
    {
        if (_contentToToggle != null)
            _contentToToggle.SetActive(isActive);

        if (isActive)
            OnContentActivated?.Invoke();
        else
            OnContentDeactivated?.Invoke();
        
    }
}
