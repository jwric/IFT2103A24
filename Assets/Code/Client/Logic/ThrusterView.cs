using System;
using System.Collections;
using Code.Client.Managers;
using Code.Shared;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Code.Client.Logic
{
    public enum ThrusterSize
    {
        Small,
        Medium,
        Large
    }

    public enum ThrusterState
    {
        Idle,
        Starting,
        Looping,
        Stopping
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
        private GameAudioSource _audioSource;

        [SerializeField] private Light2D _light;

        private ThrusterState _currentState = ThrusterState.Idle;
        private Coroutine _soundTransitionCoroutine;
        private Coroutine _flickerCoroutine;

        private float _baseVolume = 1f;
        private float _maxLightIntensity = 1f;
        private float _loopFadeTime = 0.1f;

        private ObjectPoolManager _objectPoolManager;
        private PooledParticleSystem _smokeParticles;
        private PooledParticleSystem _fireParticles;

        private ParticleSystem.MinMaxCurve _defaultSmokeRate = 100;
        private ParticleSystem.MinMaxCurve _defaultFireRate = 30;

        private void Awake()
        {
        }

        public void Initialize(ObjectPoolManager objectPoolManager)
        {
            _objectPoolManager = objectPoolManager;
        }

        private void GetPooledParticles()
        {
            if (_objectPoolManager == null)
                return;

            string smokeParticlesName = "smallThrusterSmoke";
            string fireParticlesName = "smallThrusterFire";

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

            if (_smokeParticles == null)
                _smokeParticles = _objectPoolManager.GetObject<PooledParticleSystem>(smokeParticlesName);

            if (_fireParticles == null)
                _fireParticles = _objectPoolManager.GetObject<PooledParticleSystem>(fireParticlesName);

            _smokeParticles.SpawnAttached(transform);
            _fireParticles.SpawnAttached(transform);
        }

        private void ReturnPooledParticles()
        {
            if (_smokeParticles != null)
            {
                _smokeParticles.Stop();
                _smokeParticles = null;
            }

            if (_fireParticles != null)
            {
                _fireParticles.Stop();
                _fireParticles = null;
            }
        }

        public void SetThrust(float thrustPercent)
        {
            if (thrustPercent < 0.01f)
            {
                // Stop thrusting
                if (_currentState != ThrusterState.Idle)
                {
                    ChangeState(ThrusterState.Stopping);
                }
            }
            else
            {
                // Start or continue thrusting
                if (_currentState == ThrusterState.Idle || _currentState == ThrusterState.Stopping)
                {
                    ChangeState(ThrusterState.Starting);
                }
                else if (_currentState == ThrusterState.Looping)
                {
                    AdjustLoopVolume(thrustPercent);
                }
            }

            UpdateLightAndParticles(thrustPercent);
        }

        private void ChangeState(ThrusterState newState)
        {
            if (_currentState == newState)
                return;

            // Stop any ongoing sound transition
            if (_soundTransitionCoroutine != null)
            {
                StopCoroutine(_soundTransitionCoroutine);
            }

            _currentState = newState;

            switch (newState)
            {
                case ThrusterState.Starting:
                    _soundTransitionCoroutine = StartCoroutine(PlayStartSound());
                    break;
                case ThrusterState.Looping:
                    _soundTransitionCoroutine = StartCoroutine(FadeInLoop());
                    break;
                case ThrusterState.Stopping:
                    _soundTransitionCoroutine = StartCoroutine(FadeOutLoop());
                    break;
            }
        }

        private IEnumerator PlayStartSound()
        {
            _audioSource.Stop();
            // _audioSource.loop = false;
            // _audioSource.clip = _thrustStarts.GetRandomElement();
            // _audioSource.volume = _thrustVolume;
            // _audioSource.Play();

            yield return new WaitWhile(() => _audioSource.IsPlaying);

            if (_currentState == ThrusterState.Starting)
            {
                ChangeState(ThrusterState.Looping);
            }
        }

        private IEnumerator FadeInLoop()
        {
            _audioSource.Stop();
            _audioSource.SetLoop(true);
            // _audioSource.SetClip(_thrustLoops.GetRandomElement());
            _audioSource.SetVolume(0);
            _audioSource.Play(_thrustLoops.GetRandomElement());

            float elapsedTime = 0f;
            while (elapsedTime < _loopFadeTime)
            {
                _audioSource.SetVolume(Mathf.Lerp(0, _baseVolume, elapsedTime / _loopFadeTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            _audioSource.SetVolume(_baseVolume);
        }

        private IEnumerator FadeOutLoop()
        {
            float startVolume = _audioSource.Volume;
            float elapsedTime = 0f;
            while (elapsedTime < _loopFadeTime)
            {
                _audioSource.SetVolume(Mathf.Lerp(startVolume, 0, elapsedTime / _loopFadeTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _audioSource.Stop();
            _currentState = ThrusterState.Idle;
            ReturnPooledParticles(); // Ensure particles are returned after stopping
        }

        private void AdjustLoopVolume(float thrustPercent)
        {
            _audioSource.SetVolume(_baseVolume * thrustPercent);

            if (_smokeParticles && _fireParticles)
            {
                var smokeEmission = _smokeParticles.ParticleSystem.emission;
                var fireEmission = _fireParticles.ParticleSystem.emission;
                smokeEmission.rateOverTime = _defaultSmokeRate.constant * thrustPercent;
                fireEmission.rateOverTime = _defaultFireRate.constant * thrustPercent;
            }
        }

        private void UpdateLightAndParticles(float thrustPercent)
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
            }

            if (thrustPercent > 0.01f)
            {
                GetPooledParticles(); // Ensure particles are active
                float targetIntensity = _maxLightIntensity * thrustPercent;
                _flickerCoroutine = StartCoroutine(FlickerLight(targetIntensity));
            }
            else
            {
                _flickerCoroutine = StartCoroutine(FlickerLight(0));
            }
        }

        private IEnumerator FlickerLight(float targetIntensity)
        {
            float fadeDuration = 0.1f;
            float elapsedTime = 0f;
            float initialIntensity = _light.intensity;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                _light.intensity = Mathf.Lerp(initialIntensity, targetIntensity, elapsedTime / fadeDuration);
                yield return null;
            }

            _light.intensity = targetIntensity;
        }
    }
}
