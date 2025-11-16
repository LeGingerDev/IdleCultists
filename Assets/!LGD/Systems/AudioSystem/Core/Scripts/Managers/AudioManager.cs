using Audio.Models;
using LGD.Core.Singleton;
using UnityEngine;

namespace Audio.Managers
{
    public partial class AudioManager : MonoSingleton<AudioManager>
    {
        [SerializeField]
        private AudioClipsSOCollection _audioClipsCollection;


    }
}

