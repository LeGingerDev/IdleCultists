using LGD.Utilities.Data;
using UnityEngine;
namespace LGD.Utilities.UI.UIComponents
{
    public class ValueBar : MonoBehaviour
    {
        [SerializeField]
        private ValueChange _currentValues;

        public void UpdateBar(ValueChange valueChange)
        {
            _currentValues = valueChange;
            UpdateBarInternal(valueChange);
        }

        protected virtual void UpdateBarInternal(ValueChange valueChange)
        {

        }

        protected virtual void OnBarClearUpdate()
        {

        }
    }
}