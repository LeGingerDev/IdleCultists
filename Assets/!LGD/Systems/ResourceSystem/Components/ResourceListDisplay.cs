using LGD.ResourceSystem.Models;
using System.Collections.Generic;
using UnityEngine;

public class ResourceListDisplay : MonoBehaviour
{
    [SerializeField]
    private Transform _contentParent;
    [SerializeField]
    private ResourceSimpleDisplay _resourceDisplayPrefab;
    
    private List<ResourceSimpleDisplay> _activeDisplays = new List<ResourceSimpleDisplay>();

    //Add a method for when it's freeeeeeeeeeeeee

    public void Initialise(ResourceAmountPair resource)
    {
        Clear();
        CreateDisplays(new List<ResourceAmountPair> { resource });
    }

    public void Initialise(List<ResourceAmountPair> resources)
    {
        Clear();
        CreateDisplays(resources);
    }

    public void CreateDisplays(List<ResourceAmountPair> resources)
    {
        resources.ForEach(r =>
        {
            var display = Instantiate(_resourceDisplayPrefab, _contentParent);
            display.Initialise(r);
            _activeDisplays.Add(display);
        });
    }

    public void Clear()
    {
        _activeDisplays.ForEach(d => Destroy(d.gameObject));
        _activeDisplays.Clear();
    }
}
