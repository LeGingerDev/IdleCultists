using LGD.ResourceSystem.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceSimpleDisplay : MonoBehaviour
{
    [SerializeField]
    private Image _icon;
    [SerializeField]
    private TextMeshProUGUI _amountText;

    public void Initialise(ResourceAmountPair resourcePair)
    {
        _icon.gameObject.SetActive(false);
        _icon.sprite = resourcePair.resource.icon;
        _amountText.text = $"{resourcePair.resource.GetResourceSpriteText()} {resourcePair.amount}";
    }
}