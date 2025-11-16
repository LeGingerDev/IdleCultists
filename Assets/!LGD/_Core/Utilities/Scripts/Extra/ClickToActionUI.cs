using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace LGD.Utilities.Extra
{
    public class ClickToActionUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent OnClick;

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }
    }
}