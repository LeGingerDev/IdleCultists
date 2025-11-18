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
        
        [SerializeField]
        [Tooltip("{0} = current value, {1} = max value")]
        private string _textFormat = "{0:F1} / {1:F1}";

        protected override void UpdateBarInternal(ValueChange valueChange)
        {
            _fillImage.fillAmount = valueChange.GetPercentage();
            _valueText.text = string.Format(_textFormat, valueChange.value, valueChange.maxValue);
        }
    }
}