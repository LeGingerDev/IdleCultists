using System.Collections.Generic;
using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.Extensions;
using LGD.ResourceSystem;
using LGD.ResourceSystem.Models;
using TMPro;
using UnityEngine;

public class AchievementCounterTextDisplay : BaseBehaviour
{
    [SerializeField] Resource _resource;
    [SerializeField] TextMeshProUGUI _achievementText;


    [Topic(ResourceEventIds.ON_RESOURCES_UPDATED)]
    public void OnResourcesUpdated(object sender, Dictionary<Resource, AlphabeticNotation> resources)
    {
        if (resources.TryGetValue(_resource, out AlphabeticNotation newAmount))
        {
            ChangeToNewAmount(newAmount);
        }
    }

    public void ChangeToNewAmount(AlphabeticNotation amount)
    {
        _achievementText.text = $"{_resource.GetResourceSpriteText()} Points Available: {amount.FormatAsInteger()}";
    }
}
