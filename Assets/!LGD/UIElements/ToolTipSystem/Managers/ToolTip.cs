using LGD.Core;
using System;
using ToolTipSystem.Interfaces;
using ToolTipSystem.Managers;
using UnityEngine;

namespace ToolTipSystem.Components
{
    /// <summary>
    /// base class for a tooltip. All tool tips will inherit from this defining the data type they are focusing on.
    /// It'll also be on each individual tooltip object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ToolTip<T> : BaseBehaviour, IToolTip<T>
    {
        private void Start()
        {
            ToolTipController.Instance.RegisterToolTip(this);
            _content.SetActive(false);
        }

        private void OnDestroy()
        {
            ToolTipController.Instance.UnregisterToolTip(this);
        }

        [SerializeField] private GameObject _content;
        public Type DataType => typeof(T);

        public abstract void Show(T data);
        public virtual void HideInternal()
        {

        }
        public void Show(object data)
        {
            Show((T)data);
            _content.SetActive(true);
        }

        public void Hide()
        {
            HideInternal();
            _content.SetActive(false);
        }

        public void Move(Vector2 position)
        {
            transform.position = position;
        }
    }
}