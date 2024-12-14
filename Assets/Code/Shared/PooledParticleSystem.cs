using System;
using UnityEngine;

namespace Code.Shared
{
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticleSystem : SpawnableObject
    {
        private ParticleSystem _particleSystem;
        
        public ParticleSystem ParticleSystem => _particleSystem;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            // set the action stop mode to callback
            var main = _particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        public void Spawn(Vector2 position, float rotation)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0, 0, rotation);
            gameObject.SetActive(true);
        }
        
        public void SpawnAttached(Transform parent, Vector2 localPosition = default, float rotation = 0)
        {
            transform.SetParent(parent);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(0, 0, rotation);
            gameObject.SetActive(true);
        }
        
        public void Stop()
        {
            _particleSystem.Stop();
        }
        
        private void OnParticleSystemStopped()
        {
            ReturnToPool();
        }
    }
}