using System;
using System.Collections;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public enum ThrusterSize
    {
        Small,
        Medium,
        Large
    }
    
    public class ThrusterView : MonoBehaviour
    {
        [SerializeField]
        private ThrusterSize _size;
        
        // sounds
        [SerializeField]
        private AudioClip[] _thrustLoops;
        [SerializeField]
        private AudioClip[] _thrustStarts;
        [SerializeField]
        private AudioClip[] _thrustStops;
        [SerializeField]
        private AudioSource _audioSource;
        
        
        private ParticleSystem.MinMaxCurve _defaultSmokeRate = 100;
        private ParticleSystem.MinMaxCurve _defaultFireRate = 30;
        
        private bool _isThrusting;
        
        float _loopFadeTime = 0.1f;
        private float _thrustVolume = 1f;
        private float _currentThrusterVolume = 0f;
        
        private ObjectPoolManager _objectPoolManager;
        
        private PooledParticleSystem _smokeParticles;
        private PooledParticleSystem _fireParticles;
        
        private void Awake()
        {
            _thrustVolume = _audioSource.volume;
        }
        
        public void Initialize(ObjectPoolManager objectPoolManager)
        {
            _objectPoolManager = objectPoolManager;
        }
        
        private void GetPooledParticles()
        {
            var smokeParticlesName = "smallThrusterSmoke";
            var fireParticlesName = "smallThrusterFire";
            switch (_size)
            {
                case ThrusterSize.Medium:
                    smokeParticlesName = "mediumThrusterSmoke";
                    fireParticlesName = "mediumThrusterFire";
                    break;
                case ThrusterSize.Large:
                    smokeParticlesName = "largeThrusterSmoke";
                    fireParticlesName = "largeThrusterFire";
                    break;
            }
            if (!_smokeParticles)
                _smokeParticles = _objectPoolManager.GetObject<PooledParticleSystem>(smokeParticlesName);
            if (!_fireParticles)
                _fireParticles = _objectPoolManager.GetObject<PooledParticleSystem>(fireParticlesName);
            
            _smokeParticles.SpawnAttached(transform);
            _fireParticles.SpawnAttached(transform);
        }
        
        private void ReturnPooledParticles()
        {
            if (_smokeParticles)
                _smokeParticles.Stop();
            if (_fireParticles)
                _fireParticles.Stop();
            
            _smokeParticles = null;
            _fireParticles = null;
        }

        private IEnumerator FadeOutLoop()
        {
            float startVolume = _audioSource.volume;
            float startTime = Time.time;
            while (Time.time < startTime + _loopFadeTime)
            {
                _audioSource.volume = startVolume * (1 - (Time.time - startTime) / _loopFadeTime);
                yield return new WaitForEndOfFrame();
            }
            _audioSource.Stop();
        }
        
        public IEnumerator FadeInLoop()
        {
            float startVolume = _audioSource.volume;
            float startTime = Time.time;
            while (Time.time < startTime + _loopFadeTime)
            {
                _audioSource.volume = startVolume * ((Time.time - startTime) / _loopFadeTime);
                yield return new WaitForEndOfFrame();
            }
        }

        public void SetThrust(float thrustPercent)
        {
            if (_objectPoolManager == null)
                return;
            
            // stop the particles if thrust is 0
            if (thrustPercent < 0.01f)
            {
                // return pooled particles
                ReturnPooledParticles();
            }
            else
            {
                // get pooled particles
                GetPooledParticles();
                
                // Adjust particle emissions based on thrust percentage
                var smokeEmission = _smokeParticles.ParticleSystem.emission;
                var fireEmission = _fireParticles.ParticleSystem.emission;

                smokeEmission.rateOverTime = _defaultSmokeRate.constant * thrustPercent;
                fireEmission.rateOverTime = _defaultFireRate.constant * thrustPercent;
            }
            
            // Adjust audio based on thrust percentage
            if (_isThrusting)
                _audioSource.volume = Mathf.Lerp(_currentThrusterVolume, _thrustVolume * thrustPercent, 0.1f);
            
            if (thrustPercent > 0.01f)
            {
                if (!_isThrusting)
                {
                    // Transition to thrusting state
                    _isThrusting = true;

                    // Play thrust start sound
                    _audioSource.volume = 0;
                    _currentThrusterVolume = 0;
                    _audioSource.loop = false;
                    _audioSource.clip = _thrustStarts.GetRandomElement();
                    // _audioSource.Play();

                    // Schedule loop to play after the start sound ends
                    Invoke(nameof(PlayThrustLoop), 0);
                }
            }
            else
            {
                if (_isThrusting)
                {
                    // Transition to non-thrusting state
                    _isThrusting = false;

                    // Stop the loop with a fade out
                    StartCoroutine(FadeOutLoop());
                }
            }
            
            _currentThrusterVolume = _audioSource.volume;
        }

        private void PlayThrustLoop()
        {
            if (_isThrusting) // Check if still thrusting
            {
                _audioSource.volume = 0;
                _audioSource.loop = true;
                _audioSource.clip = _thrustLoops.GetRandomElement();
                _audioSource.Play();
                
                // fade in the loop volume
                // StartCoroutine(FadeInLoop());
            }
        }

    }
}