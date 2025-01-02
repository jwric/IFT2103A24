using System;
using System.Collections;
using Code.Client.Managers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Client.Logic
{
    public enum AudioGroupType
    {
        Music,
        Ambience,
        SFX
    }
    
    public struct AudioSourceData
    {
        public AudioClip Clip;
        public float Volume;
        public float Pitch;
        public bool Loop;
        // Spatialised
        public bool Spatialised;
        public float SpatialBlend;
        public float MinDistance;
        public float MaxDistance;
    }
    
    [RequireComponent(typeof(AudioSource))]
    public class GameAudioSource : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioSourceData _audioSourceData;
        [FormerlySerializedAs("_audioType")] [SerializeField]
        private AudioGroupType audioGroupType;

        private Coroutine _soundTransitionCoroutine;
        
        public AudioGroupType AudioGroupType => audioGroupType;
        public float Volume => _audioSourceData.Volume;
        public float Pitch => _audioSourceData.Pitch;
        public bool Loop => _audioSourceData.Loop;
        public bool Spatialised => _audioSourceData.Spatialised;
        public float SpatialBlend => _audioSourceData.SpatialBlend;
        public float MinDistance => _audioSourceData.MinDistance;
        public float MaxDistance => _audioSourceData.MaxDistance;
        public bool IsPlaying => _audioSource.isPlaying;

        private void OnEnable()
        {
            // subscribe to the audio level setting
            GameManager.Instance.Settings.OnVolumeChanged += OnVolumeChanged;
        }
        
        private void OnDisable()
        {
            // unsubscribe from the audio level setting
            GameManager.Instance.Settings.OnVolumeChanged -= OnVolumeChanged;
        }

        private void OnVolumeChanged()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.volume = CalculateVolume();
            }
        }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSourceData = new AudioSourceData
            {
                Clip = _audioSource.clip,
                Volume = _audioSource.volume,
                Pitch = _audioSource.pitch,
                Loop = _audioSource.loop,
                Spatialised = _audioSource.spatialize,
                SpatialBlend = _audioSource.spatialBlend,
                MinDistance = _audioSource.minDistance,
                MaxDistance = _audioSource.maxDistance
            };
            _audioSource.playOnAwake = false;
        }
        
        public void SetVolume(float volume)
        {
            _audioSourceData.Volume = volume;
            
            if (_audioSource.isPlaying)
            {
                _audioSource.volume = CalculateVolume();
            }
        }
        
        public void SetPitch(float pitch)
        {
            _audioSourceData.Pitch = pitch;
            
            if (_audioSource.isPlaying)
            {
                _audioSource.pitch = pitch;
            }
        }
        
        public void SetLoop(bool loop)
        {
            _audioSourceData.Loop = loop;
            
            if (_audioSource.isPlaying)
            {
                _audioSource.loop = loop;
            }
        }
        
        public void SetSpatialised(bool spatialised, float spatialBlend = 1, float minDistance = 1, float maxDistance = 500)
        {
            _audioSourceData.Spatialised = spatialised;
            _audioSourceData.SpatialBlend = spatialised ? spatialBlend : 0;
            _audioSourceData.MinDistance = minDistance;
            _audioSourceData.MaxDistance = maxDistance;
        }
        
        public void PlayOneShot(AudioClip clip)
        {
            _audioSource.pitch = _audioSourceData.Pitch;
            _audioSource.loop = _audioSourceData.Loop;
            _audioSource.spatialize = _audioSourceData.Spatialised;
            _audioSource.spatialBlend = _audioSourceData.SpatialBlend;
            _audioSource.minDistance = _audioSourceData.MinDistance;
            _audioSource.maxDistance = _audioSourceData.MaxDistance;
            _audioSource.PlayOneShot(clip, CalculateVolume());
        }
        
        public void Play(AudioClip clip)
        {
            _audioSource.clip = clip;
            _audioSource.volume = CalculateVolume();
            _audioSource.pitch = _audioSourceData.Pitch;
            _audioSource.loop = _audioSourceData.Loop;
            _audioSource.spatialize = _audioSourceData.Spatialised;
            _audioSource.spatialBlend = _audioSourceData.SpatialBlend;
            _audioSource.minDistance = _audioSourceData.MinDistance;
            _audioSource.maxDistance = _audioSourceData.MaxDistance;
            _audioSource.Play();
        }
        
        public void PlayFadeIn(AudioClip clip, float duration, Action callback = null)
        {
            _audioSource.clip = clip;
            _audioSource.volume = 0;
            _audioSource.pitch = _audioSourceData.Pitch;
            _audioSource.loop = _audioSourceData.Loop;
            _audioSource.spatialize = _audioSourceData.Spatialised;
            _audioSource.spatialBlend = _audioSourceData.SpatialBlend;
            _audioSource.minDistance = _audioSourceData.MinDistance;
            _audioSource.maxDistance = _audioSourceData.MaxDistance;
            PlaySoundTransition(FadeInCoroutine(0, CalculateVolume(), duration, callback));
        }
        
        public void Stop()
        {
            _audioSource.Stop();
        }
        
        public void StopFadeOut(float duration, Action callback = null)
        {
            if (!_audioSource.isPlaying)
            {
                callback?.Invoke();
                return;
            }
            PlaySoundTransition(FadeOutCoroutine(_audioSource.volume, 0, duration, callback));
        }

        private void PlaySoundTransition(IEnumerator coroutine)
        {
            if (_soundTransitionCoroutine != null)
            {
                StopCoroutine(_soundTransitionCoroutine);
            }
            _soundTransitionCoroutine = StartCoroutine(coroutine);
        }
        
        public void SetClip(AudioClip clip)
        {
            _audioSource.clip = clip;
            
            if (_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
        
        public void SetType(AudioGroupType groupType)
        {
            audioGroupType = groupType;
            switch (audioGroupType)
            {
                case AudioGroupType.Music:
                    _audioSource.bypassListenerEffects = true;
                    break;
                case AudioGroupType.Ambience:
                    break;
                case AudioGroupType.SFX:
                    break;
            }
        }
        
        private IEnumerator FadeOutCoroutine(float startVolume, float endVolume, float duration, Action callback)
        {
            var startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                _audioSource.volume = Mathf.Lerp(startVolume, endVolume, (Time.time - startTime) / duration);
                yield return new WaitForEndOfFrame();
            }
            _audioSource.volume = endVolume;
            _audioSource.Stop();
            callback?.Invoke();
        }
        
        private IEnumerator FadeInCoroutine(float startVolume, float endVolume, float duration, Action callback)
        {
            _audioSource.volume = startVolume;
            _audioSource.Play();
            var startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                _audioSource.volume = Mathf.Lerp(startVolume, endVolume, (Time.time - startTime) / duration);
                yield return new WaitForEndOfFrame();
            }
            _audioSource.volume = endVolume;
            callback?.Invoke();
        }
        
        private float CalculateVolume()
        {
            var baseVolume = _audioSourceData.Volume * GameManager.Instance.Settings.MasterVolume;
            return audioGroupType switch
            {
                AudioGroupType.Music => baseVolume * GameManager.Instance.Settings.MusicVolume,
                AudioGroupType.Ambience => baseVolume * GameManager.Instance.Settings.AmbientVolume,
                AudioGroupType.SFX => baseVolume * GameManager.Instance.Settings.SFXVolume,
                _ => baseVolume
            };
        }
        
    }
}