using LGD.InteractionSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public class ParticleSystemClickEffect : ClickBase
{
    [SerializeField]
    private ParticleSystem _particleSystem;

    public override void OnMouseUpEvent(InteractionData clickData)
    {
        base.OnMouseUpEvent(clickData);

        if (_particleSystem != null)
        {
            _particleSystem.Play();
        }
    }

    [Button]
    public void SetBurstAmount(int amount)
    {
        if (_particleSystem == null) return;

        var emission = _particleSystem.emission;
        var burst = emission.GetBurst(0); // Gets the first burst
        burst.count = amount; // Set the particle count
        emission.SetBurst(0, burst); // Apply it back
    }
}