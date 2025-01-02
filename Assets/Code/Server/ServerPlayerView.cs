using System;
using Code.Client;
using Code.Shared;
using UnityEngine;

namespace Code.Server
{
    [RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
    public class ServerPlayerView : MonoBehaviour
    {
        private BasePlayer _player;
        private Rigidbody2D _rb;
        private Collider2D _collider;
        
        public Vector2 Position => _rb.position;
        public Vector2 Velocity => _rb.velocity;
        public float Rotation => _rb.rotation * Mathf.Deg2Rad;
        public float AngularVelocity => _rb.angularVelocity;
        
        public Rigidbody2D Rb => _rb;
        
        public static ServerPlayerView Create(ServerPlayerView prefab, BasePlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, player.Position, rot);
            obj._player = player;
            return obj;
        }
        
        public void Spawn(Vector2 position)
        {
            _rb.position = position;
            enabled = true;
        }
        
        public void Die()
        {
            enabled = false;
        }

        public void Move(Vector2 amount)
        {
            _rb.AddForce(amount, ForceMode2D.Force);
        }
        
        public void Rotate(float amount)
        {
            _rb.AddTorque(amount, ForceMode2D.Force);
        }
        
        public void SetRotation(float rotation)
        {
            _rb.MoveRotation(rotation * Mathf.Rad2Deg);
        }
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            // _player.UpdatePosition(Time.deltaTime);
            // transform.position = _player.Position;
            // transform.rotation =  Quaternion.Euler(0f, 0f, _player.Rotation * Mathf.Rad2Deg);
            // _rb.MoveRotation(_player.Rotation * Mathf.Rad2Deg);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}