using System;
using System.Collections;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public class ThrusterView : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem _smokeEffect;
        [SerializeField]
        private ParticleSystem _fireEffect;

        // sounds
        [SerializeField]
        private AudioClip[] _thrustLoops;
        [SerializeField]
        private AudioClip[] _thrustStarts;
        [SerializeField]
        private AudioClip[] _thrustStops;
        [SerializeField]
        private AudioSource _audioSource;
        
        
        private ParticleSystem.MinMaxCurve _defaultSmokeRate;
        private ParticleSystem.MinMaxCurve _defaultFireRate;
        
        private bool _isThrusting;
        
        float _loopFadeTime = 0.1f;
        private float _thrustVolume = 1f;
        private float _currentThrusterVolume = 0f;
        
        private void Awake()
        {
            _defaultSmokeRate = _smokeEffect.emission.rateOverTime;
            _defaultFireRate = _fireEffect.emission.rateOverTime;
            _thrustVolume = _audioSource.volume;
        }
        
        public IEnumerator FadeOutLoop()
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
            // Adjust particle emissions based on thrust percentage
            var smokeEmission = _smokeEffect.emission;
            var fireEmission = _fireEffect.emission;

            smokeEmission.rateOverTime = _defaultSmokeRate.constant * thrustPercent;
            fireEmission.rateOverTime = _defaultFireRate.constant * thrustPercent;
            
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