using Sirenix.OdinInspector;
using UnityEngine;

public class ParticleEffectTrigger : MonoBehaviour
{
    [SerializeField, FoldoutGroup("Settings")]
    private ParticleSystem _particleSystem;

    [Button]
    public void TriggerEffect()
    {
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
        var burst = emission.GetBurst(0);
        burst.count = amount;
        emission.SetBurst(0, burst);
    }
}