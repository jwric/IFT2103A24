using System;
using UnityEngine;

namespace Code.Client.Logic
{
    /// <summary>
    /// Layout of thrusters on a ship:
    /// main thruster: Located at the back of the ship.
    /// back left/right thrusters: they are located at the back of the ship.
    /// front left/right thrusters: they are located at the front of the ship.
    /// left lower/upper thrusters: they are located on the left side of the ship.
    /// right lower/upper thrusters: they are located on the right side of the ship.
    /// </summary>
    public class OmnidirectionalThrusterController : MonoBehaviour
    {
        [SerializeField]
        private ThrusterView _mainThruster;
        [SerializeField]
        private ThrusterView _backLeftThruster;
        [SerializeField]
        private ThrusterView _backRightThruster;
        [SerializeField]
        private ThrusterView _frontLeftThruster;
        [SerializeField]
        private ThrusterView _frontRightThruster;
        [SerializeField]
        private ThrusterView _leftLowerThruster;
        [SerializeField]
        private ThrusterView _rightLowerThruster;
        [SerializeField]
        private ThrusterView _leftUpperThruster;
        [SerializeField]
        private ThrusterView _rightUpperThruster;

        public OmnidirectionalThrusterController(
            ThrusterView mainThruster,
            ThrusterView backLeftThruster,
            ThrusterView backRightThruster,
            ThrusterView frontLeftThruster,
            ThrusterView frontRightThruster,
            ThrusterView leftLowerThruster,
            ThrusterView rightLowerThruster,
            ThrusterView leftUpperThruster,
            ThrusterView rightUpperThruster)
        {
            _mainThruster = mainThruster;
            _backLeftThruster = backLeftThruster;
            _backRightThruster = backRightThruster;
            _frontLeftThruster = frontLeftThruster;
            _frontRightThruster = frontRightThruster;
            _leftLowerThruster = leftLowerThruster;
            _rightLowerThruster = rightLowerThruster;
            _leftUpperThruster = leftUpperThruster;
            _rightUpperThruster = rightUpperThruster;
        }

        private void Start()
        {
            
        }

        /// <summary>
        /// Applies thrust to the thrusters based on the force and torque given.
        /// The current rotation of the ship is needed to determine which thrusters to fire.
        /// </summary>
        public void ApplyThrust(Vector2 force, float torque, float currentRotation)
        {
            // Convert the global force vector into the ship's local space
            Quaternion rotation = Quaternion.Euler(0f, 0f, currentRotation);
            Vector2 localForce = Quaternion.Inverse(rotation) * force;

            // Reset all thrusters before applying new settings
            ResetAllThrusters();

            // Define max values for scaling thrust output
            float maxForce = 7f;   // Example max linear force scaling
            float maxTorque = 0.5f;   // Example max torque scaling

            // Extract desired local X and Y forces
            float desiredForward = localForce.x; // +X = forward, -X = backward
            float desiredUp = localForce.y;      // +Y = up, -Y = down

            // --------------------------
            // LINEAR FORCE ALLOCATION
            // --------------------------
            // Forward/Backward:
            if (desiredForward > 0f)
            {
                // Want to move forward: use main thruster
                float thrustValue = Mathf.Clamp(desiredForward / maxForce, 0f, 1f);
                _mainThruster.SetThrust(thrustValue);
            }
            else if (desiredForward < 0f)
            {
                // Want to move backward: use front thrusters to push back
                float thrustValue = Mathf.Clamp(-desiredForward / maxForce, 0f, 1f);
                // If front thrusters produce forward thrust, to move backward we
                // reverse their logic. Assuming these thrusters point forward (nozzle facing -X),
                // firing them creates a force in +X direction on the ship:
                // To get backward movement, we might actually need them to exert force in -X direction.
                // If the front thrusters are oriented to push backward (nozzle facing +X), they will move ship backward.
                _frontLeftThruster.SetThrust(thrustValue);
                _frontRightThruster.SetThrust(thrustValue);
            }

            // Up/Down movement:
            if (desiredUp > 0f)
            {
                // Need to move UP: 
                // Previously we fired left side thrusters for up, but now we know left side thrusters are on top.
                // Top thrusters (left side) would push the ship downward if they fire outward.
                // Bottom thrusters (right side) push upward, so we use the right side thrusters instead.

                float thrustValue = Mathf.Clamp(desiredUp / maxForce, 0f, 1f);
                // Since the right side thrusters are at the bottom, firing them outward will push the ship up.
                _rightLowerThruster.SetThrust(thrustValue);
                _rightUpperThruster.SetThrust(thrustValue);
            }
            else if (desiredUp < 0f)
            {
                // Need to move DOWN:
                // Previously we fired right side thrusters for down, but now since left thrusters are on top,
                // firing them outward pushes the ship down.
    
                float thrustValue = Mathf.Clamp(-desiredUp / maxForce, 0f, 1f);
                // Since the left side thrusters are on top, firing them outward will push the ship downward.
                _leftLowerThruster.SetThrust(thrustValue);
                _leftUpperThruster.SetThrust(thrustValue);
            }

            // --------------------------
            // TORQUE ALLOCATION
            // --------------------------
            // Positive torque = rotate counterclockwise
            if (torque > 0f)
            {
                float thrustValue = Mathf.Clamp(torque / maxTorque, 0f, 1f);
                // One simple approach:
                // - Fire front-left thruster to push forward (+X)
                // - Fire back-right thruster to push backward (-X)
                // This creates a couple that rotates the ship CCW.
                _frontLeftThruster.SetThrust(thrustValue);
                _backRightThruster.SetThrust(thrustValue);
            }
            else if (torque < 0f)
            {
                float thrustValue = Mathf.Clamp(-torque / maxTorque, 0f, 1f);
                // Rotate clockwise:
                // - Fire front-right thruster to push forward (+X)
                // - Fire back-left thruster to push backward (-X)
                _frontRightThruster.SetThrust(thrustValue);
                _backLeftThruster.SetThrust(thrustValue);
            }

            // Note: If you find that combining torque and linear thrust directly
            // causes unintended effects, you might need a more sophisticated
            // blending logic—such as applying torque thrusts only after satisfying
            // linear forces, or distributing thrust among multiple thrusters differently.

            // Also note that depending on the physical placement and orientation of thrusters,
            // you might need to invert some directions or use different pairs to achieve pure torque
            // without adding net translation.
        }


        private void ResetAllThrusters()
        {
            _mainThruster.SetThrust(0);
            _backLeftThruster.SetThrust(0);
            _backRightThruster.SetThrust(0);
            _frontLeftThruster.SetThrust(0);
            _frontRightThruster.SetThrust(0);
            _leftLowerThruster.SetThrust(0);
            _rightLowerThruster.SetThrust(0);
            _leftUpperThruster.SetThrust(0);
            _rightUpperThruster.SetThrust(0);
        }
    }
}
