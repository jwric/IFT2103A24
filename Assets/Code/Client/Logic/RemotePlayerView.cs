using UnityEngine;

namespace Code.Client.Logic
{
    [RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
    public class RemotePlayerView : MonoBehaviour, IPlayerView
    {
        private RemotePlayer _player;

        private Rigidbody2D _rb;
        private Collider2D _collider;
        
        public Rigidbody2D Rb => _rb;
        
        public static RemotePlayerView Create(RemotePlayerView prefab, RemotePlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, player.Position, rot);
            obj._player = player;
            return obj;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            // _player.UpdatePosition(Time.deltaTime);
            // // transform.position = _player.Position;
            // // transform.rotation =  Quaternion.Euler(0f, 0f, _player.Rotation * Mathf.Rad2Deg);
            // _rb.position = _player.Position;
            // _rb.rotation = _player.Rotation * Mathf.Rad2Deg;
            // _rb.velocity = _player.Velocity;
        }

        private void FixedUpdate()
        {
            _player.UpdatePosition(Time.fixedDeltaTime);
            _rb.MovePosition(_player.Position);
            _rb.MoveRotation(_player.Rotation * Mathf.Rad2Deg);
            _rb.velocity = _player.Velocity;
            _rb.angularVelocity = _player.AngularVelocity;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}