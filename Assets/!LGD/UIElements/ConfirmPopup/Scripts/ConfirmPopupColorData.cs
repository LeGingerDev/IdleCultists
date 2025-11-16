using System;
using UnityEngine;
namespace LGD.UIElements.ConfirmPopup
{
    [Serializable]
    public class ConfirmPopupColorData
    {
        public ConfirmPopup.ConfirmType type;
        public Color popupBackground = Color.white;
        public Color headerFooterBackground = Color.white;
        public Color headerFooterStripes = Color.white;
    }
}