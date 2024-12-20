using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Client
{
    [CreateAssetMenu(menuName = "AudioManagerResources")]
    public class AudioManagerResources : ScriptableObject
    {
        public List<AudioClip> MusicClips;
        public List<AudioClip> AmbienceClips;
        
        public AudioClip GetMusicClip(string key)
        {
            return MusicClips.Find(clip => clip.name == key);
        }

        public AudioClip GetAmbienceClip(string key)
        {
            return AmbienceClips.Find(clip => clip.name == key);
        }
    }
}