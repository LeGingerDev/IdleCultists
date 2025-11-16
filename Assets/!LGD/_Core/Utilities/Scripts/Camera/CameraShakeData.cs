using System;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class CameraShakeData
{
    [FoldoutGroup("Shake Settings")]
    [SerializeField] private float _duration = 0.3f;

    [FoldoutGroup("Shake Settings")]
    [SerializeField] private float _strength = 0.5f;

    [FoldoutGroup("Shake Settings")]
    [Tooltip("Number of shakes within the duration")]
    [SerializeField] private int _vibrato = 10;

    [FoldoutGroup("Shake Settings")]
    [Range(0f, 90f)]
    [Tooltip("How random the shake direction is (0 = structured, 90 = completely random)")]
    [SerializeField] private float _randomness = 90f;

    [FoldoutGroup("Shake Settings")]
    [Tooltip("Whether the shake should fade out over time")]
    [SerializeField] private bool _fadeOut = true;

    public float GetDuration() => _duration;
    public float GetStrength() => _strength;
    public int GetVibrato() => _vibrato;
    public float GetRandomness() => _randomness;
    public bool GetFadeOut() => _fadeOut;

    // Constructor for creating shake data in code
    public CameraShakeData(float duration, float strength, int vibrato = 10, float randomness = 90f, bool fadeOut = true)
    {
        _duration = duration;
        _strength = strength;
        _vibrato = vibrato;
        _randomness = randomness;
        _fadeOut = fadeOut;
    }
}