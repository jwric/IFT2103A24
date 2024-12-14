using System;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EjectedShell : SpawnableObject
    {
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        
        private float _timeToLive;
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            
            // collision starts disabled, and is enabled when the shell leaves the player
            _collider.isTrigger = true;
        }
        
        public void Spawn(Vector2 position, Vector2 velocity, float rotation, float angularVelocity, float timeToLive)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0, 0, rotation);
            _timeToLive = timeToLive;
            gameObject.SetActive(true);
            _rigidbody.velocity = velocity;
            _rigidbody.angularVelocity = angularVelocity;
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            _collider.isTrigger = false;
        }

        private void Update()
        {
            _timeToLive -= Time.deltaTime;
            if (_timeToLive <= 0)
            {
                ReturnToPool();
            }
        }
    }
}