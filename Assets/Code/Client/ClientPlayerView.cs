using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Client
{
    [RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
    public class ClientPlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private TextMesh _name;
        private ClientPlayer _player;
        private Camera _mainCamera;
        
        private Rigidbody2D _rb;
        private Collider2D _collider;

        public Rigidbody2D Rb => _rb;
        
        public static ClientPlayerView Create(ClientPlayerView prefab, ClientPlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, player.Position, rot);
            obj._player = player;
            obj._name.text = player.Name;
            obj._mainCamera = Camera.main;
            return obj;
        }
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }
        
        public void Move(Vector2 amount)
        {
            _rb.AddForce(amount, ForceMode2D.Impulse);
        }
        
        private void FixedUpdate()
        {
            var vert = Input.GetAxis("Vertical");
            var horz = Input.GetAxis("Horizontal");
            var fire = Input.GetAxis("Fire1");
            
            Vector2 velocty = new Vector2(horz, vert);

            Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = mousePos - _rb.position;
            float rotation = Mathf.Atan2(dir.y, dir.x);
            _player.SetInput(velocty, rotation, fire > 0f, Time.fixedDeltaTime);

            float lerpT = ClientLogic.LogicTimer.LerpAlpha;
            // transform.position = Vector2.Lerp(_player.LastPosition, _player.Position, lerpT);

            float lastAngle = _player.LastRotation;
            if (Mathf.Abs(lastAngle - _player.Rotation) > Mathf.PI)
            {
                if (lastAngle < _player.Rotation)
                    lastAngle += Mathf.PI * 2f;
                else
                    lastAngle -= Mathf.PI * 2f;
            }
            
            float angle = Mathf.Lerp(lastAngle, _player.Rotation, lerpT);
            _rb.MoveRotation(_player.Rotation * Mathf.Rad2Deg);
        }
        
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}