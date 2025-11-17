using DG.Tweening;
using LargeNumbers;
using LGD.ResourceSystem;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using PoolSystem;
using UnityEngine;

public class PhysicalResource : HoverBase
{
    [SerializeField]
    private Resource resourceToProvide;
    [SerializeField]
    private AlphabeticNotation amountToProvide;

    // Optional: If set, the resource will be returned to the pool instead of destroyed
    private string _poolKey;

    public void Initialise(AlphabeticNotation amountToProvide, string poolKey = null)
    {
        this.amountToProvide = amountToProvide;
        this._poolKey = poolKey;
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

        // Return to pool if poolKey is set, otherwise destroy normally
        if (!string.IsNullOrEmpty(_poolKey))
        {
            PoolingManager.Return(gameObject, _poolKey, deactivate: true);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void MoveToPosition(Vector2 targetPosition, float duration, Ease ease)
    {
        transform.DOMove(targetPosition, duration).SetEase(ease);
    }
}
