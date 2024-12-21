using System;
using System.Collections.Generic;
using Code.Client.Logic;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Managers
{
    public class AudioClipSettings
    {
        public float Volume = 1;
        public float Pitch = 1;
        public bool Loop = true;
        public bool Spatialised = false;
        public float SpatialBlend = 1;
        public float MinDistance = 1;
        public float MaxDistance = 500;
    }
    
    public class AudioManager
    {
        GameManager _gameManager;
        
        // Actions
        private Action _onMusicEnd;
        private Action _onMusicStart;
        
        // Sources
        private GameAudioSource _musicSource;
        private GameObjectPool<GameAudioSource> _ambienceSourcePool;
        
        private List<(GameAudioSource, AudioClipSettings)> _activeAmbienceSources = new();
        
        private Queue<AudioClip> _musicQueue = new Queue<AudioClip>();
        
        private static GameAudioSource CreateAudioSource(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<GameAudioSource>();
            var source = go.AddComponent<GameAudioSource>();
            return source;
        }
        
        public AudioManager(GameManager gameManager)
        {
            _gameManager = gameManager;
            _musicSource = CreateAudioSource(null, "Music");
            _musicSource.SetType(AudioGroupType.Music);
            _musicSource.SetLoop(true);
            _musicSource.SetVolume(1);
            _musicSource.SetSpatialised(false, 0, 1, 500);
            _ambienceSourcePool = new GameObjectPool<GameAudioSource>(() => CreateAudioSource(null, "Ambience"));
        }
        
        // Play music with fade in/out and queueing
        public void PlayMusic(string name, bool endPrevious = true)
        {
            var clip = _gameManager.AudioManagerResources.GetMusicClip(name);
            if (clip == null)
            {
                Debug.LogError($"Music clip {name} not found");
                return;
            }
            
            if (endPrevious && _musicSource.IsPlaying)
            {
                _musicSource.StopFadeOut(0.5f);
            }
            
            _musicQueue.Enqueue(clip);
            if (!_musicSource.IsPlaying)
            {
                PlayNextMusic();
            }
        }
        
        private void PlayNextMusic()
        {
            if (_musicQueue.Count == 0)
            {
                return;
            }
            
            var clip = _musicQueue.Dequeue();
            _musicSource.PlayFadeIn(clip, 0.5f);
        }
        
        public void PlayAmbience(string name, Vector2 position, AudioClipSettings settings)
        {
            var clip = _gameManager.AudioManagerResources.GetAmbienceClip(name);
            if (clip == null)
            {
                Debug.LogError($"Ambience clip {name} not found");
                return;
            }
            
            var source = _ambienceSourcePool.Get();
            source.Stop();
            source.SetType(AudioGroupType.Ambience);
            source.SetVolume(settings.Volume);
            source.SetPitch(settings.Pitch);
            source.SetLoop(settings.Loop);
            source.SetSpatialised(settings.Spatialised, settings.SpatialBlend, settings.MinDistance, settings.MaxDistance);
            source.transform.position = position;
            source.Play(clip);
            _activeAmbienceSources.Add((source, settings));
        }
        
        public void StopAmbience()
        {
            foreach (var source in _activeAmbienceSources)
            {
                source.Item1.Stop();
                _ambienceSourcePool.Put(source.Item1);
            }
            _activeAmbienceSources.Clear();
        }
        
        int updateCount = 0;

        public void Update(float dt)
        {
            // update music, fades, etc
            
            // check for any ambience sources that have finished playing
            if (updateCount++ % 10 == 0)
            {
                for (int i = _activeAmbienceSources.Count - 1; i >= 0; i--)
                {
                    var (source, _) = _activeAmbienceSources[i];
                    if (!source.IsPlaying)
                    {
                        _ambienceSourcePool.Put(source);
                        _activeAmbienceSources.RemoveAt(i);
                    }
                }
            }

            if (!_musicSource.IsPlaying && _musicQueue.Count > 0)
            {
                PlayNextMusic();
            }
            
            updateCount = (updateCount + 1) % 10;
        }
    }
}