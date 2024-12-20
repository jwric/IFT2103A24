using System;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public class Projectile : SpawnableObject
    {
        public float Speed = 10f;         // Speed of the projectile
        public float TimeToLive = 5f;     // Time before the projectile auto-destroys

        private float _timer;             // Timer to keep track of the projectile's lifetime
        private Vector2 _direction;       // Direction of the projectile
        private Collider2D _ignoredCollider; // Collider to ignore
        private PlayerView _owner;        // Reference to the projectile's owner

        public bool IsDead => _timer <= 0;

        private void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer("Projectile");
        }

        public void Spawn(Vector2 position, Vector2 direction, PlayerView owner)
        {
            _timer = TimeToLive;
            _owner = owner;

            transform.position = position;
            SetDirection(direction);

            // Ignore collisions with the owner
            if (owner != null)
            {
                var ownerCollider = owner.GetComponent<Collider2D>();
                var projectileCollider = GetComponent<Collider2D>();
                if (ownerCollider != null && projectileCollider != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, ownerCollider);
                    _ignoredCollider = ownerCollider; // Store the ignored collider
                }
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            // Move the projectile
            transform.position += (Vector3)(_direction * (Speed * Time.deltaTime));

            // Update the timer
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                Destroy();
            }
        }

        public void SetDirection(Vector2 direction)
        {
            _direction = direction.normalized;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Ignore collision with the owner
            if (collision == _ignoredCollider) return;
            // Ignore collision with other projectiles
            if (collision.gameObject.layer == LayerMask.NameToLayer("Projectile")) return;
            // Ignore collision with the owner's ship
            if (_owner != null && collision.gameObject == _owner.gameObject) return;
            Destroy();
        }

        public void Destroy()
        {
            // Reset ignored collider
            if (_ignoredCollider != null)
            {
                var projectileCollider = GetComponent<Collider2D>();
                if (projectileCollider != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, _ignoredCollider, false);
                }
                _ignoredCollider = null;
            }

            _timer = 0;
            _owner = null;
            gameObject.SetActive(false);
            
            ReturnToPool();
        }
    }
}
