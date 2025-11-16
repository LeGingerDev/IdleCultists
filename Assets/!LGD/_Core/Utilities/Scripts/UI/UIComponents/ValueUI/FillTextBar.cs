using LGD.Utilities.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LGD.Utilities.UI.UIComponents
{
    public class FillTextBar : ValueBar
    {
        [SerializeField]
        private Image _fillImage;
        [SerializeField]
        private TextMeshProUGUI _valueText;
        protected override void UpdateBarInternal(ValueChange valueChange)
        {
            _fillImage.fillAmount = valueChange.GetPercentage();
            _valueText.text = $"{valueChange.value.ToString("F1")} / {valueChange.maxValue.ToString("F1")}";
        }
    }
}