using System;
using System.Collections;
using Code.Shared;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Code.Client.Logic
{
    public class PulseHardpointView : MonoBehaviour, IHardpointView
    {
        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        
        private Transform _parentTransform;
        private PlayerView _playerView;
        
        [SerializeField] private SpriteRenderer _glowSprite;
        [SerializeField] private Light2D _glowLight;

        // sounds
        [SerializeField] private AudioClip[] _fireSounds;
        [SerializeField] private GameAudioSource _fireSource;
        [SerializeField] private GameAudioSource _mechanismSource;
        
        public Vector2 FirePosition => GetHardpointFirePosition();

        private float _recoilPercent;
        
        private const float turretRotationSpeed = 90f;

        private Vector2 _cannonOriginalPos;
        private float _currentRecoil;
        private float maxCannonRecoil = 2/32f;
        private float cannonResetTime = 0.25f;
        private float cannonRecoilTime = 0.070f;
        
        private Coroutine _shootEffect;

        private ObjectPoolManager _objectPoolManager;

        // glow material
        private Material _glowMaterial;

        // private Color _glowColor = new Color(1f, 1f, 1f, 1f);
        // private Color _glowColorDisabled = new Color(0.5f, 0.5f, 0.5f, 1f);
        private float _glowHDRIntensity = 4f;
        private float _glowHDRIntensityDisabled = 0f;
        
        private void Start()
        {
            // get the cannonBase material
            _glowMaterial = _glowSprite.material;
            
            // set the light intensity
            _glowLight.intensity = 0f;
            
            // set the glow material intensity
            _glowMaterial.SetFloat(Intensity, _glowHDRIntensityDisabled);
        }

        public IEnumerator ShootEffect()
        {
            // Play fire sound
            _fireSource.PlayOneShot(_fireSounds.GetRandomElement());
            
            float t = 0f;

            // Recoil backward
            float startRecoil = _currentRecoil;
            float targetRecoil = Mathf.Clamp(_currentRecoil + maxCannonRecoil, 0f, maxCannonRecoil);
            while (t < cannonRecoilTime)
            {
                _currentRecoil = Mathf.Lerp(startRecoil, targetRecoil, t / cannonRecoilTime);

                // Calculate recoil in world space, accounting for rotation
                Vector2 recoilDirection = -transform.right * _currentRecoil; // Local right becomes world direction
                transform.localPosition = _cannonOriginalPos + (Vector2)_parentTransform.InverseTransformVector(recoilDirection);

                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            // Final adjustment to ensure position is correct
            Vector2 finalRecoilDirection = -transform.right * _currentRecoil;
            transform.localPosition = _cannonOriginalPos + (Vector2)_parentTransform.InverseTransformVector(finalRecoilDirection);


            // Reset back to the original position
            t = 0f;
            Vector2 startPosition = transform.localPosition;
            while (t < cannonResetTime)
            {
                transform.localPosition = Vector2.Lerp(startPosition, _cannonOriginalPos, t / cannonResetTime);
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            // Ensure exact position at the end
            transform.localPosition = _cannonOriginalPos;
            _currentRecoil = 0f; // Reset recoil after fully resetting
        }

        private IEnumerator CooldownGlow()
        {
            float time = 0.5f;
            float t = 0f;

            _glowMaterial.SetFloat(Intensity, _glowHDRIntensity);
            _glowLight.intensity = 1f;
            while (t < time)
            {
                t += Time.deltaTime;
                // set hdr intensity
                _glowMaterial.SetFloat(Intensity, Mathf.Lerp(_glowHDRIntensity, _glowHDRIntensityDisabled, t / time));
                _glowLight.intensity = Mathf.Lerp(1f, 0f, t / time);
                yield return new WaitForEndOfFrame();
            }
            _glowMaterial.SetFloat(Intensity, _glowHDRIntensityDisabled);
            _glowLight.intensity = 0f;
            
            // Ensure final color matches the target
            // _glowSprite.color = _glowColorDisabled;
        }
        
        public void Initialize(Transform parent, Vector2Int position, ObjectPoolManager objectPoolManager)
        {
            _objectPoolManager = objectPoolManager;
            _parentTransform = parent;
            // Set position relative to the parent
            transform.SetParent(parent);
            transform.localPosition = new Vector3((float)position.x * 1/32f, (float)position.y * 1/32f, 0);
            
            _cannonOriginalPos = transform.localPosition;
            
            // set the player view
            _playerView = GetComponentInParent<PlayerView>();
        }

        public void AimAt(Vector2 target, float dt)
        {
            const float maxAngle = 45f;
            Vector2 direction = (target - GetHardpointBasePosition()).normalized;
            float targetAngle = Vector2.SignedAngle(_parentTransform.right, direction);
            targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);
            float currentAngle = transform.localRotation.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turretRotationSpeed * dt);
            transform.localRotation = Quaternion.Euler(0f, 0f, newAngle);
        }
        
        public Vector2 GetHardpointFirePosition()
        {
            return transform.TransformPoint(Vector3.right * 10/32f);
        }
        
        public Vector2 GetHardpointBasePosition()
        {
            return transform.TransformPoint(Vector3.zero);
        }

        public void CurrentRotation(float rotation)
        {
            // Update the rotation directly
            transform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        public float GetRotation()
        {
            // Return the current rotation
            return transform.localRotation.eulerAngles.z;
        }

        public void OnHardpointAction(byte action)
        {
            // Perform action (e.g., fire, play animation, etc.)
            if (action == 1)
            {
                // Fire
                if (_shootEffect != null)
                {
                    StopCoroutine(_shootEffect);
                }
                _shootEffect = StartCoroutine(ShootEffect());
                
            }
        }

        public void SpawnShoot(Vector2 from, Vector2 to)
        {
            // var particles = _objectPoolManager.GetObject<PooledParticleSystem>("hit");
            // var effDir = (to - from).normalized;
            // var effPos = from + effDir * 0.5f;
            // var effAngle = Mathf.Atan2(effDir.y, effDir.x) * Mathf.Rad2Deg;
            // particles.Spawn(effPos, effAngle);
            //
            var eff = _objectPoolManager.GetObject<Projectile>("pulse");
            
            var dir = (to - from).normalized;
            eff.Spawn(from, dir, _playerView);
        }
        
        public void SpawnFire(Vector2 to)
        {
            SpawnShoot(GetHardpointFirePosition(), to);
        }

        public Vector2 GetFirePosition()
        {
            return GetHardpointFirePosition();
        }

        public void Destroy()
        {
            // Clean up
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