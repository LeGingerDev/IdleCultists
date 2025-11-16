using System;
using UnityEngine;

namespace LGD.Utilities.General
{
    public class SubscribableMouseEvents : MonoBehaviour
    {
        public event Action OnMouseEnterEvent;
        public event Action OnMouseExitEvent;
        public event Action OnMouseOverEvent;
        public event Action OnMouseDownEvent;
        public event Action OnMouseUpEvent;
        private void OnMouseEnter()
        {
            OnMouseEnterEvent?.Invoke();
        }
        private void OnMouseExit()
        {
            OnMouseExitEvent?.Invoke();
        }
        private void OnMouseOver()
        {
            OnMouseOverEvent?.Invoke();
        }
        private void OnMouseDown()
        {
            OnMouseDownEvent?.Invoke();
        }
        private void OnMouseUp()
        {
            OnMouseUpEvent?.Invoke();
        }
    }
}