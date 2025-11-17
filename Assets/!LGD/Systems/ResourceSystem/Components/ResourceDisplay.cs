using DG.Tweening;
using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.Extensions;
using LGD.ResourceSystem.Extensions;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LGD.ResourceSystem.Components
{
    public class ResourceDisplay : BaseBehaviour
    {
        [SerializeField, FoldoutGroup("Display Settings")] private bool _hideUntilObtained;
        [SerializeField, FoldoutGroup("Display Settings")] private CanvasGroup _fadeGroup;
        [SerializeField, FoldoutGroup("Display Settings")] private LayoutElement _layoutElement;
        [SerializeField, FoldoutGroup("UI References")] private Resource _resource;
        [SerializeField, FoldoutGroup("UI References")] private Image _resourceIcon;
        [SerializeField, FoldoutGroup("UI References")] private TextMeshProUGUI _resourceAmount;

        private AlphabeticNotation _currentAmount;
        private Tween _amountTween;

        private void Awake()
        {
            _resourceAmount = GetComponentInChildren<TextMeshProUGUI>();
            if (_resourceIcon)
                _resourceIcon.sprite = _resource.icon;
        }

        private void Start()
        {
            AlphabeticNotation amount = _resource.GetTotalAmount(); // Use extension method
            ChangeToNewAmount(amount);
            CheckToShow();
        }

        [Topic(ResourceEventIds.ON_RESOURCES_UPDATED)]
        public void OnResourcesUpdated(object sender, Dictionary<Resource, AlphabeticNotation> resources)
        {
            //TODO: Maybe add in a scale effect when this gets triggered.
            //TODO: Maybe add in a UI particle effect when it happens.
            if (resources.TryGetValue(_resource, out AlphabeticNotation newAmount))
            {
                ChangeToNewAmount(newAmount);
            }
            CheckToShow();
        }

        public void ChangeToNewAmount(AlphabeticNotation amount)
        {
            if (_amountTween != null && _amountTween.IsActive())
            {
                _amountTween.Kill();
            }

            _currentAmount = amount;

            // Use automatic formatting: 2 decimals under 1K, 1 decimal at K+
            _resourceAmount.text = amount.FormatWithDecimals();
        }

        public void CheckToShow()
        {
            if (!_hideUntilObtained)
            {
                _layoutElement.ignoreLayout = false;
                _fadeGroup.alpha = 1;
                return;
            }

            if (_resource.HasAny()) // Use extension method
                _fadeGroup.DOFade(1, 0.5f).OnStart(() => _layoutElement.ignoreLayout = false);
            else
                _fadeGroup.DOFade(0, 0).OnComplete(() => _layoutElement.ignoreLayout = true);
        }

        private void OnValidate()
        {
            if (_resource == null)
                return;

            gameObject.name = $"ResourceDisplay_{_resource.displayName}";

            if (_resourceIcon)
                _resourceIcon.sprite = _resource.icon;
        }
    }
}