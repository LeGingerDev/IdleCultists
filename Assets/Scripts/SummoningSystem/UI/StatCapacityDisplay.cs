using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatCapacityDisplay : BaseBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _capacityText;

    [SerializeField]
    private StatType capacityType;

    [SerializeField]
    private StatType maxCapacityType;

    [SerializeField]
    private Sprite _iconSprite;

    [SerializeField]
    private Image _iconImage;

    private void Start()
    {
        _iconImage.sprite = _iconSprite;
        OnStatsRecalculated(this);
    }

    [Topic(StatEventIds.ON_STATS_RECALCULATED)]
    public void OnStatsRecalculated(object sender)
    {
        AlphabeticNotation currentCapacityAmount = StatManager.Instance.QueryStat(capacityType);
        AlphabeticNotation maxCapacityAmount = StatManager.Instance.QueryStat(maxCapacityType);

        // ToString() automatically formats as "1.5K", "23.4M", etc.
        _capacityText.text = $"{currentCapacityAmount}/{maxCapacityAmount}";
    }
}