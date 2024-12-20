using System.Collections.Generic;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public class OmnidirectionalThrusterController2 : MonoBehaviour
    {
        private ObjectPoolManager _objectPoolManager;

        [SerializeField] private ThrusterView[] _forwardThrusters;
        [SerializeField] private ThrusterView[] _backwardThrusters;
        [SerializeField] private ThrusterView[] _upThrusters;
        [SerializeField] private ThrusterView[] _downThrusters;
        [SerializeField] private ThrusterView[] _leftThrusters;
        [SerializeField] private ThrusterView[] _rightThrusters;

        private Dictionary<ThrusterView, float> _thrustRequests;

        private void Awake()
        {
            _thrustRequests = new Dictionary<ThrusterView, float>();

            // Initialize the dictionary with all thrusters, ensuring no duplicates
            InitializeThrusterDictionary(_forwardThrusters);
            InitializeThrusterDictionary(_backwardThrusters);
            InitializeThrusterDictionary(_upThrusters);
            InitializeThrusterDictionary(_downThrusters);
            InitializeThrusterDictionary(_leftThrusters);
            InitializeThrusterDictionary(_rightThrusters);
        }

        private void InitializeThrusterDictionary(ThrusterView[] thrusters)
        {
            foreach (var thruster in thrusters)
            {
                if (!_thrustRequests.ContainsKey(thruster))
                {
                    _thrustRequests[thruster] = 0f;
                }
            }
        }

        public void Initialize(ObjectPoolManager objectPoolManager)
        {
            _objectPoolManager = objectPoolManager;
            InitializeThrusters(_forwardThrusters);
            InitializeThrusters(_backwardThrusters);
            InitializeThrusters(_upThrusters);
            InitializeThrusters(_downThrusters);
            InitializeThrusters(_leftThrusters);
            InitializeThrusters(_rightThrusters);
        }

        private void InitializeThrusters(ThrusterView[] thrusters)
        {
            foreach (var thruster in thrusters)
            {
                thruster.Initialize(_objectPoolManager);
            }
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

            // Reset thrust requests using a temporary list of keys
            var keys = new List<ThrusterView>(_thrustRequests.Keys);
            foreach (var key in keys)
            {
                _thrustRequests[key] = 0f;
            }
            
            // Compute thrust for linear movement
            float maxForce = 1f; // Normalization factor
            ApplyGroupThrust(_forwardThrusters, Mathf.Clamp(force.x / maxForce, 0f, 1f));
            ApplyGroupThrust(_backwardThrusters, Mathf.Clamp(-force.x / maxForce, 0f, 1f));
            ApplyGroupThrust(_upThrusters, Mathf.Clamp(force.y / maxForce, 0f, 1f));
            ApplyGroupThrust(_downThrusters, Mathf.Clamp(-force.y / maxForce, 0f, 1f));

            // Compute thrust for torque
            float maxTorque = 1f; // Normalization factor
            ApplyGroupThrust(_leftThrusters, Mathf.Clamp(torque / maxTorque, 0f, 1f));
            ApplyGroupThrust(_rightThrusters, Mathf.Clamp(-torque / maxTorque, 0f, 1f));

            // Apply aggregated thrust values
            foreach (var kvp in _thrustRequests)
            {
                kvp.Key.SetThrust(kvp.Value);
            }
        }

        private void ApplyGroupThrust(ThrusterView[] thrusters, float thrustValue)
        {
            if (thrustValue <= 0f) return;

            foreach (var thruster in thrusters)
            {
                _thrustRequests[thruster] += thrustValue;
            }
        }
    }
}
