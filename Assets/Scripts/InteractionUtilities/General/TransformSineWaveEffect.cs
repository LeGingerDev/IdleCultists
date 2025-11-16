using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;

public class TransformSineWaveEffect : MonoBehaviour
{
    [FoldoutGroup("Target Settings")]
    [SerializeField] private Transform _target;

    [FoldoutGroup("Wave Parameters")]
    [SerializeField] private float _amplitude = 1f;
    [FoldoutGroup("Wave Parameters")]
    [SerializeField] private float _frequency = 1f;
    [FoldoutGroup("Wave Parameters")]
    [SerializeField] private Vector3 _direction = Vector3.up;
    [FoldoutGroup("Wave Parameters")]
    [SerializeField] private float _phaseOffset = 0f;

    [FoldoutGroup("Control")]
    [SerializeField] private bool _useLocalSpace = true;
    [FoldoutGroup("Control")]
    [SerializeField] private bool _playOnStart = true;

    private Vector3 _startPosition;
    private Coroutine _waveCoroutine;
    private bool _isPlaying;

    private void Start()
    {
        if (_target == null)
        {
            _target = transform;
        }

        _startPosition = _useLocalSpace ? _target.localPosition : _target.position;

        if (_playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (_isPlaying) return;

        _startPosition = _useLocalSpace ? _target.localPosition : _target.position;
        _waveCoroutine = StartCoroutine(AnimateSineWave());
    }

    public void Stop()
    {
        if (!_isPlaying) return;

        if (_waveCoroutine != null)
        {
            StopCoroutine(_waveCoroutine);
            _waveCoroutine = null;
        }

        _isPlaying = false;
    }

    public void ResetToStart()
    {
        Stop();

        if (_useLocalSpace)
        {
            _target.localPosition = _startPosition;
        }
        else
        {
            _target.position = _startPosition;
        }
    }

    private IEnumerator AnimateSineWave()
    {
        _isPlaying = true;
        float time = _phaseOffset;

        while (_isPlaying)
        {
            float offset = Mathf.Sin(time * _frequency) * _amplitude;
            Vector3 newPosition = _startPosition + (_direction.normalized * offset);

            if (_useLocalSpace)
            {
                _target.localPosition = newPosition;
            }
            else
            {
                _target.position = newPosition;
            }

            time += Time.deltaTime;
            yield return null;
        }
    }

    public bool GetIsPlaying()
    {
        return _isPlaying;
    }
}