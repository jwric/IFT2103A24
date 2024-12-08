using System.Collections;
using UnityEngine;

namespace Code.Client.Logic
{
    public class HardpointView : MonoBehaviour
    {
        [SerializeField] private Transform _ejection;
        [SerializeField] private Transform _base;
        [SerializeField] private Transform _barrelBase;
        [SerializeField] private Transform _barrelEnd;

        public Vector2 FirePosition => _base.TransformPoint(Vector3.zero);

        private const float ejectionTrayDistance = 1f / 32f * 5f;

        private float _recoilPercent;
        private float _endRecoilDistance = 1f / 32f * 6f;
        private float _baseRecoilDistance = 1f / 32f * 5f;

        private Coroutine _ejectionCoroutine;

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
        }

        private IEnumerator AnimateEjectionTray(bool unlock)
        {
            float time = 0.5f;
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

            // Ensure final position matches the target
            _ejection.localPosition = end;

            // Clear the coroutine reference when finished
            _ejectionCoroutine = null;
        }
    }
}
