using System;
using UnityEngine;

namespace Code.Shared
{
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticleSystem : SpawnableObject
    {
        private ParticleSystem _particleSystem;

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
        
        private void OnParticleSystemStopped()
        {
            ReturnToPool();
        }
    }
}