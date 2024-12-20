using System;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public class ShootEffect : SpawnableObject
    {
        [SerializeField] private LineRenderer _trailRenderer;
        [SerializeField] private GameAudioSource _source;
        [SerializeField] private GameAudioSource _target;
        [SerializeField] private AudioClip[] _shootClips;
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private Projectile _bulletPrefab; // Reference to your bullet prefab
        [SerializeField] private float _bulletSpeed = 20f; // Bullet speed in units per second

        private GameTimer _aliveTimer = new GameTimer(0.3f);
        private Vector3[] _positions = new Vector3[2];
        private Projectile _bulletInstance;
        private bool _bulletMoving;
        private Vector2 _bulletStart;
        private Vector2 _bulletEnd;
        private float _bulletTravelTime;
        private float _bulletElapsedTime;

        public void Spawn(Vector2 from, Vector2 to, PlayerView owner)
        {
            _source.transform.position = from;
            _target.transform.position = to;

            // Initialize LineRenderer
            _trailRenderer.transform.position = from;
            _positions[0] = from;
            _positions[1] = to;
            _trailRenderer.gameObject.SetActive(false);
            _trailRenderer.SetPositions(_positions);
            gameObject.SetActive(true);

            // Play audio
            _source.PlayOneShot(_shootClips.GetRandomElement());
            _target.PlayOneShot(_hitClips.GetRandomElement());

            // Instantiate and move the bullet
            if (_bulletPrefab)
            {
                if (!_bulletInstance)
                {
                    _bulletInstance = Instantiate(_bulletPrefab);
                }

                _bulletInstance.Spawn(from, to - from, owner);

                // Prepare movement
                _bulletStart = from;
                _bulletEnd = to;
                _bulletTravelTime = Vector2.Distance(from, to) / _bulletSpeed; // Calculate travel time
                _bulletElapsedTime = 0f;
                _bulletMoving = true;
            }
        }

        private void Update()
        {
            // Update the lifetime timer
            _aliveTimer.Update(Time.deltaTime, () => { });

            // Update LineRenderer color fade
            float t1 = _aliveTimer.Time / _aliveTimer.MaxTime;
            float t2 = _aliveTimer.Time / (_aliveTimer.MaxTime * 2f);
            Color a = new Color(1f, 1f, 0f, 1f);
            Color b = new Color(1f, 1f, 0f, 0f);
            _trailRenderer.startColor = Color.Lerp(a, b, t1);
            _trailRenderer.endColor = Color.Lerp(a, b, t2);

            // Update bullet movement
            // if (_bulletMoving)
            // {
            //     _bulletElapsedTime += Time.deltaTime;
            //     float t = _bulletElapsedTime / _bulletTravelTime;
            //
            //     if (t >= 1f)
            //     {
            //         t = 1f;
            //         _bulletMoving = false;
            //         
            //         OnDeath();
            //     }
            //     //
            //     // Vector2 currentPosition = Vector2.Lerp(_bulletStart, _bulletEnd, t);
            //     // _bulletInstance.transform.position = currentPosition;
            //
            //     // Update rotation to face the direction
            //     // Vector2 direction = (_bulletEnd - _bulletStart).normalized;
            //     // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            //     // _bulletInstance.transform.rotation = Quaternion.Euler(0, 0, angle);
            // }
            
            if (_bulletInstance)
            {
                if (_bulletInstance.IsDead)
                {
                    OnDeath();
                    Destroy(_bulletInstance.gameObject);
                }
            }

        }

        private void OnDeath()
        {
            ReturnToPool();
        }
    }
}
