using LGD.Utilities.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LGD.Utilities.UI.UIComponents
{
    public class TimerFillValueBar : ValueBar
    {
        [SerializeField]
        private Image _fillImage;
        [SerializeField]
        private TextMeshProUGUI _timerText;
        [SerializeField]
        private string _displayFormat = "Time Remaining\n{0}m {1}s";
        [SerializeField]
        private bool _fillBarInReverse = true;

        public void UpdateFillImage(ValueChange valueChange)
        {
            //I need to reverse it. So the value change has a value and a max value assuming 0 is bottom and max is lets say 90 for 1m 30s.
            //The bar needs to move upwards so as time goes down then the bar is going up. the _fillbarInReverse needs to respect this.

            float percentage = valueChange.GetPercentage();
            float finalPercentage = _fillBarInReverse ? 1 - percentage : percentage;
            _fillImage.fillAmount = finalPercentage;

            float maxSeconds = valueChange.maxValue;
            float currentSeconds = valueChange.value;

            string timeString = string.Format(_displayFormat,
                Mathf.FloorToInt(currentSeconds / 60), // Minutes
                Mathf.FloorToInt(currentSeconds % 60)  // Seconds
            );

            _timerText.text = timeString;

        }

        protected override void UpdateBarInternal(ValueChange valueChange)
        {
            UpdateFillImage(valueChange);
        }

        protected override void OnBarClearUpdate()
        {
            _fillImage.fillAmount = 0;
        }
    }
}