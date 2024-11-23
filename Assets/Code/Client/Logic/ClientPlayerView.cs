using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Client.Logic
{
    [RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
    public class ClientPlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private TextMesh _name;
        [SerializeField] private GameObject _view;

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
            
            obj._view = Instantiate(obj._view);
            
            return obj;
        }
        
        public void UpdateView(Vector2 position, float rotation)
        {
            _view.transform.position = position;
            _view.transform.rotation = Quaternion.Euler(0f, 0f, rotation * Mathf.Rad2Deg);
        }
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }
        
        public void Move(Vector2 amount)
        {
            _rb.AddForce(amount, ForceMode2D.Force);
        }

        // takes in the absolute rotation difference
        public float AbsoluteRotationDiff(float r1, float r2)
        {
            float diff = Mathf.Abs(r1 - r2);
            if (diff > Mathf.PI)
                diff = Mathf.PI * 2f - diff;
            return diff;
        } 
        
        public static float GetAngleDifference(float angle1, float angle2)
        {
            float difference = angle2 - angle1;

            difference = (difference + 180) % 360;
            if (difference < 0)
            {
                difference += 360;
            }
            return difference - 180;
        }

        
        private void Update()
        {
            var vert = Input.GetAxis("Vertical");
            var horz = Input.GetAxis("Horizontal");
            var fire = Input.GetAxis("Fire1");

            Vector2 velocity = new Vector2(horz, vert);

            Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = mousePos - _rb.position;
            float targetRotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Calculate the angle difference
            float rotationDiff = GetAngleDifference(_rb.rotation, targetRotation);

            // Proportional and Derivative Control
            float angularVelocity = _rb.angularVelocity;
            float kP = 0.5f;
            float kD = 0.1f;

            float torque = (kP * rotationDiff) - (kD * angularVelocity);

            // Clamp the torque input to [-1, 1]
            torque = Mathf.Clamp(torque, -1f, 1f);

            _player.SetInput(velocity, torque, fire > 0f);
        }


        public void Spawn(Vector2 position, float rotation)
        {
            _rb.position = position;
            _rb.rotation = rotation;
            enabled = true;
        }

        public void Die()
        {
            enabled = false;            
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}