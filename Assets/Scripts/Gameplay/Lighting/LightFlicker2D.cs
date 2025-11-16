using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using Sirenix.OdinInspector;

public class LightFlicker2D : MonoBehaviour
{
    [System.Serializable]
    public class Light2DData
    {
        [Required]
        [SerializeField] private Light2D _light;

        [MinMaxSlider(0f, 50f, true)]
        [SerializeField] private Vector2 _intensityRange = new Vector2(0.8f, 1.2f);

        private float _targetIntensity;

        public Light2D GetLight() => _light;
        public Vector2 GetIntensityRange() => _intensityRange;
        public float GetTargetIntensity() => _targetIntensity;
        public void SetTargetIntensity(float value) => _targetIntensity = value;
    }

    [FoldoutGroup("Light Settings")]
    [SerializeField] private Light2DData[] _lightData;

    [FoldoutGroup("Flicker Settings")]
    [Range(0.1f, 5f)]
    [SerializeField] private float _flickerFrequency = 0.5f;

    [FoldoutGroup("Flicker Settings")]
    [Range(0.1f, 2f)]
    [SerializeField] private float _transitionDuration = 0.3f;

    [FoldoutGroup("Flicker Settings")]
    [SerializeField] private Ease _flickerEase = Ease.InOutSine;

    [FoldoutGroup("Flicker Settings")]
    [SerializeField] private bool _playOnStart = true;

    private Coroutine _flickerCoroutine;

    private void Start()
    {
        if (_playOnStart)
        {
            StartFlicker();
        }
    }

    [FoldoutGroup("Controls")]
    [Button(ButtonSizes.Medium)]
    public void StartFlicker()
    {
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
        }

        _flickerCoroutine = StartCoroutine(FlickerRoutine());
    }

    [FoldoutGroup("Controls")]
    [Button(ButtonSizes.Medium)]
    public void StopFlicker()
    {
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }

        ResetAllLightsToOriginalIntensity();
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            CalculateNewTargetIntensities();
            AnimateToTargetIntensities();

            yield return new WaitForSeconds(_flickerFrequency);
        }
    }

    private void CalculateNewTargetIntensities()
    {
        foreach (Light2DData data in _lightData)
        {
            if (data.GetLight() == null) continue;

            float randomIntensity = Random.Range(data.GetIntensityRange().x, data.GetIntensityRange().y);
            data.SetTargetIntensity(randomIntensity);
        }
    }

    private void AnimateToTargetIntensities()
    {
        foreach (Light2DData data in _lightData)
        {
            if (data.GetLight() == null) continue;

            Light2D light = data.GetLight();
            DOTween.Kill(light);

            DOTween.To(() => light.intensity, x => light.intensity = x, data.GetTargetIntensity(), _transitionDuration)
                .SetEase(_flickerEase)
                .SetTarget(light);
        }
    }

    private void ResetAllLightsToOriginalIntensity()
    {
        foreach (Light2DData data in _lightData)
        {
            if (data.GetLight() == null) continue;

            Light2D light = data.GetLight();
            DOTween.Kill(light);

            float midRangeIntensity = (data.GetIntensityRange().x + data.GetIntensityRange().y) / 2f;
            DOTween.To(() => light.intensity, x => light.intensity = x, midRangeIntensity, _transitionDuration)
                .SetTarget(light);
        }
    }

    private void OnDestroy()
    {
        foreach (Light2DData data in _lightData)
        {
            if (data.GetLight() != null)
            {
                DOTween.Kill(data.GetLight());
            }
        }
    }
}