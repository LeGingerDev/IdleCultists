using LGD.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace LGD.Utilities.UI.UIComponents
{
    public class PointerDownUI : BaseBehaviour, IPointerDownHandler
    {
        [SerializeField] private UnityEvent _onPointerDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDown?.Invoke();
        }
    }
}