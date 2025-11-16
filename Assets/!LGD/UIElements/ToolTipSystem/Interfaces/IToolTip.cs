using System;
using UnityEngine;

namespace ToolTipSystem.Interfaces
{
    public interface IToolTip
    {
        Type DataType { get; }
        void Show(object data);
        void Hide();
        void Move(Vector2 position);
    }

    public interface IToolTip<T> : IToolTip
    {
        void Show(T data);
    }
}