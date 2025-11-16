using LGD.Utilities.Data;
using UnityEngine;
using UnityEngine.UI;
namespace LGD.Utilities.UI.UIComponents
{
    public class FillBar : ValueBar
    {
        [SerializeField]
        private Image _fillImage;

        protected override void UpdateBarInternal(ValueChange valueChange)
        {
            _fillImage.fillAmount = valueChange.GetPercentage();
        }
    }
}