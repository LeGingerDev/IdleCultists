using DG.Tweening;
using LargeNumbers;
using LGD.ResourceSystem;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using UnityEngine;

public class PhysicalResource : HoverBase
{
    [SerializeField]
    private Resource resourceToProvide;
    [SerializeField]
    private AlphabeticNotation amountToProvide;

    public void Initialise(AlphabeticNotation amountToProvide)
    {
        this.amountToProvide = amountToProvide;
    }

    public override void OnHoverStart()
    {
        base.OnHoverStart();
        ResourceManager.Instance.AddResource(resourceToProvide, amountToProvide);
        //Play sound effect.
        DestroyResource();
    }

    public void DestroyResource()
    {
        Publish(ResourceEventIds.ON_PHYSICAL_RESOURCE_DESTROYED, this);
        Destroy(this.gameObject);
    }

    public void MoveToPosition(Vector2 targetPosition, float duration, Ease ease)
    {
        transform.DOMove(targetPosition, duration).SetEase(ease);
    }
}
