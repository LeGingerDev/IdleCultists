using Audio.Core;
using LGD.Utilities.Attributes;
using System;
using UnityEngine;
namespace Audio.Models
{
    [Serializable]
    public class AudioPlayerData
    {
        [SerializeField, ConstDropdown(typeof(AudioConstIds))]
        public string clipId;
        [SerializeField]
        public bool isOneShot = true;
        [SerializeField]
        public bool positionBased;
    }
}