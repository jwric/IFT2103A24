using UnityEngine;

namespace Code.Shared
{
    public enum HardpointType : byte
    {
        None,
        Cannon,
        PulseLaser,
        Missile,
        Railgun
    }

    /// <summary>
    /// Represents a discrete action/event produced by a weapon during update.
    /// For example:
    /// 0 = no action
    /// 1 = fire projectile
    /// 2 = start charging
    /// 3 = release charge
    /// etc.
    /// Interpretation depends on weapon type.
    /// </summary>
    public struct WeaponAction
    {
        public byte ActionCode;

        public WeaponAction(byte code)
        {
            ActionCode = code;
        }

        public static readonly WeaponAction None = new(0);
    }

    public interface IFiringStrategy
    {
        WeaponAction Update(float delta, bool triggerHeld);
    }
    
    public class Hardpoint
    {
        private readonly HardpointType _type;
        private float _rotation;
        public readonly float MaxRotationAngle;
        private readonly IFiringStrategy _firingStrategy;
        
        private bool _triggerHeld;

        private WeaponAction _lastAction = WeaponAction.None;
        
        public HardpointType Type => _type;
        public float Rotation => _rotation;
        
        public bool HasAction(out WeaponAction action)
        {
            action = FetchLastAction();
            return action.ActionCode != 0;
        }
        
        public Hardpoint(HardpointType type, float maxRotationAngle, IFiringStrategy firingStrategy)
        {
            _type = type;
            _rotation = 0f;
            MaxRotationAngle = maxRotationAngle;
            _firingStrategy = firingStrategy;
        }

        /// <summary>
        /// Called each logic tick. Should advance internal timers,
        /// check input states, and possibly produce an action.
        /// </summary>
        public void Update(float delta)
        {
            _lastAction = _firingStrategy.Update(delta, _triggerHeld);
        }

        /// <summary>
        /// Updates the current rotation of the hardpoint.
        /// The rotation is clamped to the maximum rotation angle.
        /// </summary>
        public void SetRotation(float rotation)
        {
            _rotation = Mathf.Clamp(Mathf.DeltaAngle(0f, rotation), -MaxRotationAngle, MaxRotationAngle);
        }
        
        /// <summary>
        /// Sets whether the weapon's trigger is currently being held down.
        /// This is a "sticky" input that remains true until explicitly set false.
        /// </summary>
        public virtual void SetTriggerHeld(bool isHeld)
        {
            _triggerHeld = isHeld;
        }

        /// <summary>
        /// Retrieve and clear the last action produced by this weapon.
        /// Typically called after Update to see if something happened this tick.
        /// </summary>
        public WeaponAction FetchLastAction()
        {
            WeaponAction action = _lastAction;
            _lastAction = WeaponAction.None; // reset after fetching
            return action;
        }
    }

    public class EmptyFiringStrategy : IFiringStrategy
    {
        public WeaponAction Update(float delta, bool triggerHeld)
        {
            return WeaponAction.None;
        }
    }

    public class CannonFiringStrategy : IFiringStrategy
    {
        private float _fireCooldown;
        private float _timeSinceLastShot = 0f;

        public CannonFiringStrategy(float fireCooldown = 1.0f)
        {
            _fireCooldown = fireCooldown;
        }
        
        public WeaponAction Update(float delta, bool triggerHeld)
        {
            _timeSinceLastShot += delta;

            if (triggerHeld && _timeSinceLastShot >= _fireCooldown)
            {
                _timeSinceLastShot = 0f;
                return new WeaponAction(1); 
            }

            return WeaponAction.None;
        }
    }

    public class MissileFiringStrategy : IFiringStrategy
    {
        private float _lockOnTime = 2.0f; // time needed to lock before firing
        private float _currentLockTime = 0f;
        private bool _lockedOn = false;

        public WeaponAction Update(float delta, bool triggerHeld)
        {
            _currentLockTime += delta;
            if (_currentLockTime >= _lockOnTime && !_lockedOn)
            {
                _lockedOn = true;
                return new WeaponAction(2); // 2 = locked on
            }

            if (_lockedOn)
            {
                _lockedOn = false;
                _currentLockTime = 0f;
                return new WeaponAction(1); // 1 = fire missile
            }

            return WeaponAction.None;
        }
    }

    public class RailgunHardpoint : IFiringStrategy
    {
        private float _chargeTimeRequired = 1.5f;
        private float _currentChargeTime = 0f;

        private enum RailgunState
        {
            Idle,
            Charging,
            Fired
        }

        private RailgunState _state = RailgunState.Idle;
        private bool _wasTriggerHeldLastFrame = false;

        public WeaponAction Update(float delta, bool triggerHeld)
        {
            bool freshPress = (triggerHeld && !_wasTriggerHeldLastFrame);
            bool triggerReleased = (!triggerHeld && _wasTriggerHeldLastFrame);
            
            var action = WeaponAction.None;
            
            switch (_state)
            {
                case RailgunState.Idle:
                    if (freshPress)
                    {
                        _state = RailgunState.Charging;
                        _currentChargeTime = 0f;
                        action = new WeaponAction(2); // 2 = begin charging
                    }
                    break;

                case RailgunState.Charging:
                    if (!triggerHeld)
                    {
                        _state = RailgunState.Idle;
                        _currentChargeTime = 0f;
                    }
                    else
                    {
                        _currentChargeTime += delta;
                        if (_currentChargeTime >= _chargeTimeRequired)
                        {
                            action = new WeaponAction(1); // 1 = fire
                            _state = RailgunState.Fired;
                        }
                    }
                    break;
                case RailgunState.Fired:
                    if (triggerReleased)
                    {
                        _state = RailgunState.Idle;
                        _currentChargeTime = 0f;
                    }
                    break;
            }

            _wasTriggerHeldLastFrame = triggerHeld;
            return action;
        }
    }

    public struct HardpointAction
    {
        public byte SlotId;
        public byte ActionCode;
    }
    
    public class HardpointSlot
    {
        public readonly byte Id;
        public readonly Hardpoint Hardpoint;
        // Position on ship in pixels; 1 pixel = 1/32 units in Unity
        public readonly Vector2Int Position;
        
        public HardpointSlot(byte id, Hardpoint hardpoint, Vector2Int position)
        {
            Id = id;
            Hardpoint = hardpoint;
            Position = position;
        }

        public bool IsEmpty => Hardpoint.Type == HardpointType.None;
    }
    
    public class HardpointFactory
    {
        public static Hardpoint CreateHardpoint(HardpointType type)
        {
            switch (type)
            {
                case HardpointType.Cannon:
                    return new Hardpoint(HardpointType.Cannon, 30f, new CannonFiringStrategy(1.0f));
                case HardpointType.PulseLaser:
                    return new Hardpoint(HardpointType.PulseLaser, 45f, new CannonFiringStrategy(0.25f));
                case HardpointType.Missile:
                    return new Hardpoint(HardpointType.Missile, 180f, new MissileFiringStrategy());
                case HardpointType.Railgun:
                    return new Hardpoint(HardpointType.Railgun, 0f, new RailgunHardpoint());
                default:
                    return new Hardpoint(HardpointType.None, 0f, new EmptyFiringStrategy());
            }
        }
    }
}
