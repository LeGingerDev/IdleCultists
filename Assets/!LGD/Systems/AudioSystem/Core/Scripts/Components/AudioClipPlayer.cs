using Audio.Managers;
using Audio.Models;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Audio.Components
{
    public class AudioClipPlayer : MonoBehaviour
    {
        [SerializeField]
        private bool _playOnStart = false;

        [SerializeField]
        private AudioPlayerData _audioPlayerData;

        [Header("Spawn Delay Settings")]
        [SerializeField]
        private bool _useSpawnDelay = false;

        [SerializeField]
        private float _spawnDelayDuration = 0.5f;

        private float _firstEnableTime = -1f;

        private void OnEnable()
        {
            if (_firstEnableTime < 0f)
            {
                _firstEnableTime = Time.time;
            }

            if (_playOnStart)
            {
                PlaySFX();
            }
        }
        [Button("Test Audio")]
        public void PlaySFX()
        {
            if (_useSpawnDelay && !HasSpawnDelayPassed())
            {
                return;
            }

            AudioManager.Instance.PlaySFX(_audioPlayerData.clipId, _audioPlayerData.isOneShot, _audioPlayerData.positionBased ? transform.position : null);
        }

        private bool HasSpawnDelayPassed()
        {
            return Time.time >= _firstEnableTime + _spawnDelayDuration;
        }
    }
}