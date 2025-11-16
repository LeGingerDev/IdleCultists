using System;
using UnityEngine;

namespace LGD.Utilities.General
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SubscribableParticles : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        public event Action<GameObject> OnParticleCollisionEvent;


        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void OnParticleCollision(GameObject other)
        {
            print($"Detected {other.name}");
            OnParticleCollisionEvent(other);
        }
    }
}