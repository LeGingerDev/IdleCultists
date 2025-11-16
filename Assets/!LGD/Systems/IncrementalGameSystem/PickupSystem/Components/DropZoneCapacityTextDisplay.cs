using TMPro;
using UnityEngine;

public class DropZoneCapacityTextDisplay : MonoBehaviour, IDropZoneCapacityDisplayer
{
    [SerializeField]
    private TextMeshProUGUI _capacityText;
    public void UpdateCapacityDisplay(int currentCapacity, int maxCapacity)
    {
        if(_capacityText != null)
        {
            _capacityText.text = $"{currentCapacity}/{maxCapacity}";
        }
    }
}
