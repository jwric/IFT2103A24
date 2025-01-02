using System.Collections.Generic;
using UnityEngine;

namespace Code.Shared
{
    public enum ShipType : byte
    {
        Artillery,
        Fighter,
    }
    
    /// <summary>
    /// A model class representing a ship.
    /// </summary>
    public class Ship
    {
        private readonly ShipType _type;
        private readonly byte _health;
        private readonly List<HardpointSlot> _hardpoints;
        
        private readonly float angularThrust;
        private readonly float strafeThrust;
        private readonly float forwardThrust;
        private readonly float reverseThrust;
        
        private readonly Vector2Int _size;
        
        
        public ShipType Type => _type;
        public byte Health => _health;
        public List<HardpointSlot> Hardpoints => _hardpoints;
        public float AngularThrust => angularThrust;
        public float StrafeThrust => strafeThrust;
        public float ForwardThrust => forwardThrust;
        public float ReverseThrust => reverseThrust;
        public Vector2Int Size => _size;
        
        public Ship(ShipType type, byte health, List<HardpointSlot> hardpoints, float angularThrust, float strafeThrust, float forwardThrust, float reverseThrust, Vector2Int size)
        {
            _type = type;
            _health = health;
            _hardpoints = hardpoints;
            this.angularThrust = angularThrust;
            this.strafeThrust = strafeThrust;
            this.forwardThrust = forwardThrust;
            this.reverseThrust = reverseThrust;
            _size = size;
        }
        

        // This function calculates the thrust force of the ship if it wants thrust in a certain direction
        public Vector2 CalculateDirThrustForce(Vector2 thrustPercents)
        {
            thrustPercents.x = Mathf.Clamp(thrustPercents.x, -1f, 1f);
            thrustPercents.y = Mathf.Clamp(thrustPercents.y, -1f, 1f);
            
            Vector2 thrustForce = Vector2.zero;
            thrustForce += thrustPercents.x > 0 ? Vector2.right * (forwardThrust * thrustPercents.x) : Vector2.right * (reverseThrust * thrustPercents.x);
            thrustForce += Vector2.up * (strafeThrust * thrustPercents.y);
            
            return thrustForce;
        }

        public float CalculateAngularThrustTorque(float thrustPercent)
        {
            thrustPercent = Mathf.Clamp(thrustPercent, -1f, 1f);
            return thrustPercent * angularThrust;
        }
        
        public Vector2 CalculateInverseThrustPercents(Vector2 direction, float rotation, Vector2 maxForcePercent)
        {
            // Normalize the direction vector to ensure consistent thrust calculation
            if (direction.sqrMagnitude > 0)
            {
                direction.Normalize();
            }
            else
            {
                return Vector2.zero;
            }

            // Rotate the direction vector to ship's local space
            Vector2 localDirection = Quaternion.Euler(0, 0, -rotation) * direction;

            // Compute raw thrust percentages for each axis
            float rawXThrustPercent = localDirection.x > 0
                ? localDirection.x / forwardThrust
                : localDirection.x / reverseThrust;
            float rawYThrustPercent = localDirection.y / strafeThrust;

            // Compute the scaling factor to maximize thrust
            float scaleFactor = Mathf.Min(
                Mathf.Abs(maxForcePercent.x / rawXThrustPercent),
                Mathf.Abs(maxForcePercent.y / rawYThrustPercent)
            );

            // Apply scaling to balance the forces and maximize thrust
            float xThrustPercent = rawXThrustPercent * scaleFactor;
            float yThrustPercent = rawYThrustPercent * scaleFactor;

            // Clamp the final thrust percentages to ensure they are within the allowable range
            xThrustPercent = Mathf.Clamp(xThrustPercent, -maxForcePercent.x, maxForcePercent.x);
            yThrustPercent = Mathf.Clamp(yThrustPercent, -maxForcePercent.y, maxForcePercent.y);

            return new Vector2(xThrustPercent, yThrustPercent);
        }


    }


    public class ShipFactory
    {
        public static Ship CreateShip(ShipType type)
        {
            switch (type)
            {
                case ShipType.Artillery:
                    return new Ship(ShipType.Artillery, 100, new List<HardpointSlot>
                    {
                        new(0, HardpointFactory.CreateHardpoint(HardpointType.Cannon), new Vector2Int(-4, 0)),
                    }, 0.5f, 3f, 5f, 3f, new Vector2Int(4, 4));
                case ShipType.Fighter:
                    return new Ship(ShipType.Fighter, 100, new List<HardpointSlot>
                    {
                        new(0, HardpointFactory.CreateHardpoint(HardpointType.PulseLaser), new Vector2Int(16, 8)),
                        new(1, HardpointFactory.CreateHardpoint(HardpointType.PulseLaser), new Vector2Int(16, -8)),
                    }, 0.5f, 3f, 7f, 3f, new Vector2Int(5, 6));
                default:
                    return null;
            }
        }
    } 
}