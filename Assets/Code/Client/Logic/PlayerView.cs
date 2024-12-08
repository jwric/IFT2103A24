using System;
using System.Collections;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    [RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
    public class PlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private TextMesh _name;
        
        private Rigidbody2D _rb;
        private Collider2D _collider;

        public Rigidbody2D Rb => _rb;
        
        [SerializeField]
        private HardpointView _hardpointView;
        
        [SerializeField]
        private Transform _cannon;
        private Vector2 _cannonOriginalPos;
        private float _currentRecoil;
        private float maxCannonRecoil = 0.2f;
        private float cannonResetTime = 0.25f;
        private float cannonRecoilTime = 0.070f;
        
        private float turretRotationSpeed = 30f;

        
        private Coroutine _shootEffect;
        
        public static PlayerView Create(PlayerView prefab, BasePlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, player.Position, rot);
            obj._name.text = player.Name;
            return obj;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _cannonOriginalPos = _cannon.localPosition;
        }
        
        public void OnHit(HitInfo hitInfo)
        {
            // set sprite red for a moment
            StartCoroutine(HitEffect());
        }
        
        private IEnumerator HitEffect()
        {
            var spriteRenderer = _name;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.cyan;
        }

        public void OnShoot(Vector2 target)
        {
            // Stop the current effect if running
            if (_shootEffect != null)
                StopCoroutine(_shootEffect);

            // Increase recoil and start the effect
            _shootEffect = StartCoroutine(ShootEffect());
            
            // // unlock the ejection tray animation
            // _hardpointView.UnlockEjectionTray();
            // StartCoroutine(LockEjectionTrayAfterDelay());
        }
        
        private IEnumerator LockEjectionTrayAfterDelay()
        {
            // Wait for the unlock animation to finish
            yield return new WaitForSeconds(0.5f); // Match the animation time in HardpointView
            _hardpointView.LockEjectionTray();
        }
        

        public IEnumerator ShootEffect()
        {
            float t = 0f;

            // Recoil backward
            float startRecoil = _currentRecoil;
            float targetRecoil = Mathf.Clamp(_currentRecoil + maxCannonRecoil, 0f, maxCannonRecoil);
            while (t < cannonRecoilTime)
            {
                _currentRecoil = Mathf.Lerp(startRecoil, targetRecoil, t / cannonRecoilTime);
                _cannon.localPosition = _cannonOriginalPos - Vector2.right * _currentRecoil;
                _hardpointView.SetRecoilPercent(_currentRecoil / maxCannonRecoil);
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame(); // Use WaitForEndOfFrame
            }

            _cannon.localPosition = _cannonOriginalPos - Vector2.right * _currentRecoil;
            _hardpointView.SetRecoilPercent(1f);

            // Reset back to the original position
            t = 0f;
            Vector2 startPosition = _cannon.localPosition;
            while (t < cannonResetTime)
            {
                _cannon.localPosition = Vector2.Lerp(startPosition, _cannonOriginalPos, t / cannonResetTime);
                _hardpointView.SetRecoilPercent(1f - (t / cannonResetTime));
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame(); // Use WaitForEndOfFrame
            }

            // Ensure exact position at the end
            _cannon.localPosition = _cannonOriginalPos;
            _hardpointView.SetRecoilPercent(0f);
            _currentRecoil = 0f; // Reset recoil after fully resetting
        }

        private void Update()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            AimTurretAt(mousePos);
        }

        
        public void AimTurretAt(Vector2 target)
        {
            const float maxAngle = 30f;
            Vector2 direction = (target - GetHardpointBasePosition()).normalized;
            float targetAngle = Vector2.SignedAngle(transform.right, direction);
            targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);
            float currentAngle = _hardpointView.transform.localRotation.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turretRotationSpeed * Time.deltaTime);
            _hardpointView.transform.localRotation = Quaternion.Euler(0f, 0f, newAngle);
        }
        
        public Vector2 GetHardpointFirePosition()
        {
            return _hardpointView.transform.TransformPoint(Vector3.right);
        }
        
        public Vector2 GetHardpointBasePosition()
        {
            return _hardpointView.transform.TransformPoint(Vector3.zero);
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

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            // hardpoint fire position
            Gizmos.DrawWireSphere(GetHardpointFirePosition(), 0.05f);
        }
    }
}