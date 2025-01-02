using System;
using System.Collections;
using Code.Shared;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Code.Client.Logic
{
    public class CannonHardpointView : MonoBehaviour, IHardpointView
    {
        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        
        private Transform _parentTransform;
        private PlayerView _playerView;
        
        [SerializeField] private Transform _ejection;
        [SerializeField] private Transform _base;
        [SerializeField] private Transform _barrelBase;
        [SerializeField] private Transform _barrelEnd;
        [SerializeField] private SpriteRenderer _glowSprite;
        [SerializeField] private Light2D _glowLight;

        // sounds
        [SerializeField] private AudioClip[] _fireSounds;
        [SerializeField] private GameAudioSource _fireSource;
        
        [SerializeField] private AudioClip _ejectionLockSound;
        [SerializeField] private AudioClip _ejectionUnlockSound;
        [SerializeField] private AudioClip[] _reloadSounds;
        [SerializeField] private GameAudioSource _mechanismSource;
        
        
        public Vector2 FirePosition => GetHardpointFirePosition();

        private const float ejectionTrayDistance = 1f / 32f * 5f;

        private float _recoilPercent;
        private float _endRecoilDistance = 1f / 32f * 6f;
        private float _baseRecoilDistance = 1f / 32f * 5f;
        
        private const float turretRotationSpeed = 30f;

        private Vector2 _cannonOriginalPos;
        private float _currentRecoil;
        private float maxCannonRecoil = 0.1f;
        private float cannonResetTime = 0.25f;
        private float cannonRecoilTime = 0.070f;
        
        private Coroutine _ejectionCoroutine;
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

        public void SetRecoilPercent(float percent)
        {
            // Telescopic barrel recoil, x axis
            // 0 is no recoil, 1 is max recoil

            _recoilPercent = percent;

            // Define the thresholds for each segment
            float firstSegmentThreshold = 0.5f;

            // Calculate positions for the first segment (barrel base)
            if (percent <= firstSegmentThreshold)
            {
                // Map percent to the first segment's range [0, 0.5]
                float segmentPercent = percent / firstSegmentThreshold;
                Vector3 start = Vector3.zero;
                Vector3 end = start - Vector3.right * _endRecoilDistance;
                _barrelEnd.localPosition = Vector3.Lerp(start, end, segmentPercent);

                // Keep the second segment (barrel end) at its initial position
                _barrelBase.localPosition = Vector3.zero;
            }
            else
            {
                // First segment is fully retracted, keep it at the max position
                _barrelEnd.localPosition = Vector3.zero - Vector3.right * _endRecoilDistance;

                // Map percent to the second segment's range [0.5, 1]
                float segmentPercent = (percent - firstSegmentThreshold) / (1f - firstSegmentThreshold);
                Vector3 start = Vector3.zero;
                Vector3 end = start - Vector3.right * _baseRecoilDistance;
                _barrelBase.localPosition = Vector3.Lerp(start, end, segmentPercent);
            }
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

                // Update recoil percentage
                SetRecoilPercent(_currentRecoil / maxCannonRecoil);
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            // Final adjustment to ensure position is correct
            Vector2 finalRecoilDirection = -transform.right * _currentRecoil;
            transform.localPosition = _cannonOriginalPos + (Vector2)_parentTransform.InverseTransformVector(finalRecoilDirection);
            SetRecoilPercent(1f);



            // Reset back to the original position
            t = 0f;
            Vector2 startPosition = transform.localPosition;
            while (t < cannonResetTime)
            {
                transform.localPosition = Vector2.Lerp(startPosition, _cannonOriginalPos, t / cannonResetTime);
                SetRecoilPercent(1f - (t / cannonResetTime));
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            // Ensure exact position at the end
            transform.localPosition = _cannonOriginalPos;
            SetRecoilPercent(0f);
            _currentRecoil = 0f; // Reset recoil after fully resetting
        }

        public void UnlockEjectionTray()
        {
            // Start unlocking animation
            StartEjectionAnimation(true);
        }

        public void LockEjectionTray()
        {
            // Start locking animation
            StartEjectionAnimation(false);
        }

        private void StartEjectionAnimation(bool unlock)
        {
            // Stop any existing animation to prevent conflicts
            if (_ejectionCoroutine != null)
            {
                StopCoroutine(_ejectionCoroutine);
            }

            // Start a new animation coroutine
            _ejectionCoroutine = StartCoroutine(AnimateEjectionTray(unlock));
            // spawn ejected shell
            if (unlock)
            {
                // cooldown glow
                StartCoroutine(CooldownGlow());
                
                var shellPos = _base.TransformPoint(Vector3.left * 8/32f);
                var shellVel = _base.TransformDirection(Vector3.up * 2 + Vector3.left * Random.Range(1f, 0f));
                var shellRot = _base.rotation.eulerAngles.z;
                var shellAngVel = Random.Range(-100f, 100f);
                var shell = _objectPoolManager.GetObject<EjectedShell>("ejectedShell");
                shell.Spawn(shellPos, shellVel, shellRot, shellAngVel, 10f);
            }
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

        private IEnumerator AnimateEjectionTray(bool unlock)
        {
            float time = 0.1f;
            float t = 0f;

            // Determine start and end positions
            Vector3 start = _ejection.localPosition;
            Vector3 end = unlock
                ? start - Vector3.right * ejectionTrayDistance
                : start + Vector3.right * ejectionTrayDistance;

            while (t < time)
            {
                t += Time.deltaTime;
                _ejection.localPosition = Vector3.Lerp(start, end, t / time);
                yield return new WaitForEndOfFrame();
            }
            
            // Play lock sound at the end of the animation
            if (!unlock)
            {
                _mechanismSource.PlayOneShot(_ejectionLockSound);
            }

            // Ensure final position matches the target
            _ejection.localPosition = end;

            // Clear the coroutine reference when finished
            _ejectionCoroutine = null;
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
            const float maxAngle = 30f;
            Vector2 direction = (target - GetHardpointBasePosition()).normalized;
            float targetAngle = Vector2.SignedAngle(_parentTransform.right, direction);
            targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);
            float currentAngle = transform.localRotation.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turretRotationSpeed * dt);
            transform.localRotation = Quaternion.Euler(0f, 0f, newAngle);
        }
        
        public Vector2 GetHardpointFirePosition()
        {
            return transform.TransformPoint(Vector3.right * 0.75f);
        }
        
        private IEnumerator LockEjectionTrayAfterDelay()
        {
            // Play reload sound
            yield return new WaitForSeconds(0.25f); // Match the animation time in HardpointView
            AudioClip rlSfx = _reloadSounds.GetRandomElement();
            _mechanismSource.PlayOneShot(rlSfx);
            // Wait for the unlock animation to finish
            yield return new WaitForSeconds(rlSfx.length); // Match the animation time in HardpointView
            LockEjectionTray();
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
                
                // eject shell
                UnlockEjectionTray();
                StartCoroutine(LockEjectionTrayAfterDelay());
            }
        }

        public void SpawnShoot(Vector2 from, Vector2 to)
        {
            var particles = _objectPoolManager.GetObject<PooledParticleSystem>("hit");
            var effDir = (to - from).normalized;
            var effPos = from + effDir * 0.5f;
            var effAngle = Mathf.Atan2(effDir.y, effDir.x) * Mathf.Rad2Deg;
            particles.Spawn(effPos, effAngle);
            
            var eff = _objectPoolManager.GetObject<Projectile>("bullet");
            
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