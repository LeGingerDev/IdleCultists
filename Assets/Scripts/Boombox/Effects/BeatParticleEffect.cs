using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Beat-synced particle effect that emits particles in sync with music beats
/// Fires particle bursts on each beat from the Boombox
/// </summary>
public class BeatParticleEffect : BaseBehaviour
{
    [FoldoutGroup("Particle Systems"), Required]
    [Tooltip("List of particle systems to emit on each beat")]
    [SerializeField] private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

    [FoldoutGroup("Emission Settings")]
    [Tooltip("Number of particles to emit per beat")]
    [Range(1, 100)]
    [SerializeField] private int emitCount = 10;

    [FoldoutGroup("Emission Settings")]
    [Tooltip("If true, uses particle system's emission settings instead of emitCount")]
    [SerializeField] private bool useParticleSystemEmission = false;

    [FoldoutGroup("Settings")]
    [Tooltip("If true, effect is active. If false, no particles emit")]
    [SerializeField] private bool isActive = true;

    [FoldoutGroup("Settings")]
    [Tooltip("Only emit particles if at least one particle system is assigned")]
    [SerializeField] private bool requireParticleSystems = true;

    private void OnValidate()
    {
        // If particle systems list is empty, try to find one on this GameObject
        if (particleSystems == null || particleSystems.Count == 0)
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                particleSystems = new List<ParticleSystem> { ps };
            }
        }
    }

    #region Event Listeners

    [Topic(BoomboxEventIds.ON_BEAT)]
    public void OnBeat(object sender, float bpm)
    {
        if (!isActive)
            return;

        if (requireParticleSystems && (particleSystems == null || particleSystems.Count == 0))
            return;

        // Emit particles from all systems
        EmitParticles();
    }

    [Topic(BoomboxEventIds.ON_TRACK_STOPPED)]
    public void OnTrackStopped(object sender)
    {
        // Optionally stop all particle systems when music stops
        StopAllParticleSystems();
    }

    #endregion

    #region Particle Emission

    /// <summary>
    /// Emit particles from all particle systems
    /// </summary>
    private void EmitParticles()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                if (useParticleSystemEmission)
                {
                    // Play the particle system (uses its own emission settings)
                    if (!ps.isPlaying)
                    {
                        ps.Play();
                    }
                    else
                    {
                        // Trigger a burst
                        ps.Play();
                    }
                }
                else
                {
                    // Emit specific number of particles
                    ps.Emit(emitCount);
                }
            }
        }
    }

    /// <summary>
    /// Stop all particle systems
    /// </summary>
    private void StopAllParticleSystems()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    /// <summary>
    /// Clear all particles immediately
    /// </summary>
    private void ClearAllParticles()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Clear();
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Enable or disable the effect
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;

        if (!active)
        {
            StopAllParticleSystems();
        }
    }

    /// <summary>
    /// Add a particle system to the emission list
    /// </summary>
    public void AddParticleSystem(ParticleSystem ps)
    {
        if (ps != null && !particleSystems.Contains(ps))
        {
            particleSystems.Add(ps);
        }
    }

    /// <summary>
    /// Remove a particle system from the emission list
    /// </summary>
    public void RemoveParticleSystem(ParticleSystem ps)
    {
        if (ps != null)
        {
            particleSystems.Remove(ps);
        }
    }

    /// <summary>
    /// Set the emit count for all particle systems
    /// </summary>
    public void SetEmitCount(int count)
    {
        emitCount = Mathf.Clamp(count, 1, 100);
    }

    #endregion

    #region Cleanup

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllParticleSystems();
    }

    private void OnDestroy()
    {
        ClearAllParticles();
    }

    #endregion

    #region Editor

#if UNITY_EDITOR
    [Button("Test Emit Particles"), FoldoutGroup("Testing")]
    private void TestEmit()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter play mode to test");
            return;
        }

        EmitParticles();
    }

    [Button("Stop All Particles"), FoldoutGroup("Testing")]
    private void TestStop()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter play mode to test");
            return;
        }

        StopAllParticleSystems();
    }

    [Button("Clear All Particles"), FoldoutGroup("Testing")]
    private void TestClear()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter play mode to test");
            return;
        }

        ClearAllParticles();
    }

    [Button("Add Particle System from This GameObject"), FoldoutGroup("Setup")]
    private void AddParticleSystemFromSelf()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            if (!particleSystems.Contains(ps))
            {
                particleSystems.Add(ps);
                Debug.Log($"Added ParticleSystem from {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("ParticleSystem already in list");
            }
        }
        else
        {
            Debug.LogWarning("No ParticleSystem found on this GameObject");
        }
    }

    [Button("Add Particle Systems from Children"), FoldoutGroup("Setup")]
    private void AddParticleSystemsFromChildren()
    {
        ParticleSystem[] childSystems = GetComponentsInChildren<ParticleSystem>();
        int addedCount = 0;

        foreach (var ps in childSystems)
        {
            if (ps != null && !particleSystems.Contains(ps))
            {
                particleSystems.Add(ps);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            Debug.Log($"Added {addedCount} ParticleSystem(s) from children");
        }
        else
        {
            Debug.LogWarning("No new ParticleSystems found in children");
        }
    }
#endif

    #endregion
}
