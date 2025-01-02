using System.Collections.Generic;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public class OmnidirectionalThrusterController : MonoBehaviour
    {
        private ObjectPoolManager _objectPoolManager;
        
        [SerializeField] private ThrusterView _mainThruster;
        [SerializeField] private ThrusterView _backLeftThruster;
        [SerializeField] private ThrusterView _backRightThruster;
        [SerializeField] private ThrusterView _frontLeftThruster;
        [SerializeField] private ThrusterView _frontRightThruster;
        [SerializeField] private ThrusterView _leftLowerThruster;
        [SerializeField] private ThrusterView _rightLowerThruster;
        [SerializeField] private ThrusterView _leftUpperThruster;
        [SerializeField] private ThrusterView _rightUpperThruster;

        private Dictionary<ThrusterView, float> _thrustRequests;

        private void Awake()
        {
            _thrustRequests = new Dictionary<ThrusterView, float>
            {
                {_mainThruster, 0f},
                {_backLeftThruster, 0f},
                {_backRightThruster, 0f},
                {_frontLeftThruster, 0f},
                {_frontRightThruster, 0f},
                {_leftLowerThruster, 0f},
                {_rightLowerThruster, 0f},
                {_leftUpperThruster, 0f},
                {_rightUpperThruster, 0f}
            };
        }

        public void Initialize(ObjectPoolManager objectPoolManager)
        {
            _objectPoolManager = objectPoolManager;
            _mainThruster.Initialize(_objectPoolManager);
            _backLeftThruster.Initialize(_objectPoolManager);
            _backRightThruster.Initialize(_objectPoolManager);
            _frontLeftThruster.Initialize(_objectPoolManager);
            _frontRightThruster.Initialize(_objectPoolManager);
            _leftLowerThruster.Initialize(_objectPoolManager);
            _rightLowerThruster.Initialize(_objectPoolManager);
            _leftUpperThruster.Initialize(_objectPoolManager);
            _rightUpperThruster.Initialize(_objectPoolManager);
        }
        
        public void ApplyLocalThrust(Vector2 force, float torque, float currentRotation)
        {
            // Convert global force to local force
            Quaternion rotation = Quaternion.Euler(0f, 0f, currentRotation);
            Vector2 localForce = Quaternion.Inverse(rotation) * force;
            
            ApplyThrust(localForce, torque);
        }
        
        public void ApplyThrust(Vector2 force, float torque)
        {
            if (_objectPoolManager == null)
                return;
            
            // Reset thrust requests
            var keys = new List<ThrusterView>(_thrustRequests.Keys);
            foreach (var key in keys)
                _thrustRequests[key] = 0f;

            // Convert global force to local force

            // Max values for normalization
            float maxForce = 1f;
            float maxTorque = 1f;

            // Compute thrust for linear movement
            float desiredForward = force.x; // +X = forward, -X = backward
            float desiredUp = force.y;      // +Y = up, -Y = down

            if (desiredForward > 0f)
                _thrustRequests[_mainThruster] += Mathf.Clamp(desiredForward / maxForce, 0f, 1f);
            else if (desiredForward < 0f)
            {
                float thrustValue = Mathf.Clamp(-desiredForward / maxForce, 0f, 1f);
                _thrustRequests[_frontLeftThruster] += thrustValue;
                _thrustRequests[_frontRightThruster] += thrustValue;
            }

            if (desiredUp > 0f)
            {
                float thrustValue = Mathf.Clamp(desiredUp / maxForce, 0f, 1f);
                _thrustRequests[_rightLowerThruster] += thrustValue;
                _thrustRequests[_rightUpperThruster] += thrustValue;
            }
            else if (desiredUp < 0f)
            {
                float thrustValue = Mathf.Clamp(-desiredUp / maxForce, 0f, 1f);
                _thrustRequests[_leftLowerThruster] += thrustValue;
                _thrustRequests[_leftUpperThruster] += thrustValue;
            }

            // Compute thrust for torque
            if (torque > 0f)
            {
                float thrustValue = Mathf.Clamp(torque / maxTorque, 0f, 1f);
                _thrustRequests[_frontLeftThruster] += thrustValue;
                _thrustRequests[_backRightThruster] += thrustValue;
            }
            else if (torque < 0f)
            {
                float thrustValue = Mathf.Clamp(-torque / maxTorque, 0f, 1f);
                _thrustRequests[_frontRightThruster] += thrustValue;
                _thrustRequests[_backLeftThruster] += thrustValue;
            }

            // Apply aggregated thrust values
            foreach (var kvp in _thrustRequests)
                kvp.Key.SetThrust(kvp.Value);
        }
    }
}
