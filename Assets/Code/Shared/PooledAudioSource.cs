using System;
using UnityEngine;

namespace Code.Shared
{
    
    [RequireComponent(typeof(AudioSource))]
    public class PooledAudioSource : SpawnableObject
    {
        private AudioSource _audioSource;
        
        public AudioSource AudioSource => _audioSource;
        

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        public void Spawn(Vector2 position)
        {
            transform.position = position;
            gameObject.SetActive(true);
        }
        
        public void SpawnAttached(Transform parent, Vector2 localPosition = default)
        {
            transform.SetParent(parent);
            transform.localPosition = localPosition;
            gameObject.SetActive(true);
        }
        
        public void Destroy()
        {
            ReturnToPool();
        }
    }
}