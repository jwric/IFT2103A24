using UnityEngine;

namespace Code.Client
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothSpeed = 8f;
        private Camera _camera;

        void OnEnable()
        {
            _camera = GetComponent<Camera>();
        }
        
        private void LateUpdate()
        {
            // if (!target && _camera.enabled)
            // {
            //     _camera.enabled = false;
            // }
            // else if (target && !_camera.enabled)
            // {
            //     _camera.enabled = true;
            // }

            
            Vector3 desiredPosition = target ? new Vector3(target.position.x, target.position.y, -10) : Vector3.zero;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, -10);
        }
    }
}