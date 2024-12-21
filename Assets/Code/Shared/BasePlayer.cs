using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Shared
{
    public struct HitInfo
    {
        public BasePlayer Damager;
        public byte Damage;
        public Vector2 Position;
    }
    
    public abstract class BasePlayer
    {
        public readonly string Name;

        protected Ship _ship = null;
        // public readonly List<HardpointSlot> Hardpoints = new();

        // protected float _speed = 7f;
        // protected float _angularSpeed = 0.5f;
        protected GameTimer _shootTimer = new GameTimer(0.2f);
        protected byte Damage = 10;
        private BasePlayerManager _playerManager;

        protected Color _primaryColor = Utils.DecodeColor(0xFFFFFFFF);
        protected Color _secondaryColor = Utils.DecodeColor(0x3E3E3EFF);
        
        protected Vector2 _position;
        protected float _rotation;
        protected byte _health;
        protected Vector2 _velocity;
        protected float _angularVelocity;

        public const float Radius = 0.5f;
        public bool IsAlive => _health > 0;
        public byte Health => _health;
        public Vector2 Position => _position;
        public Vector2 Velocity => _velocity;
        public float Rotation => _rotation;
        public float AngularVelocity => _angularVelocity;
        public readonly byte Id;
        public int Ping;
        
        public List<HardpointSlot> Hardpoints => _ship.Hardpoints;

        protected BasePlayer(BasePlayerManager playerManager, string name, byte id)
        {
            Id = id;
            Name = name;
            _playerManager = playerManager;
        }

        public virtual void Spawn(Vector2 position)
        {
            _position = position;
            _rotation = 0;
            _health = _ship.Health;
        }

        protected void Shoot()
        {
            // const float MaxLength = 20f;
            // Vector2 dir = new Vector2(Mathf.Cos(_rotation), Mathf.Sin(_rotation));
            // var player = _playerManager.CastToPlayer(_position, dir, MaxLength, this);
            // Vector2 target = _position + dir * (player != null ? Vector2.Distance(_position, player._position) : MaxLength);
            // _playerManager.OnShoot(this, target, player, Damage);
        }
        
        public void OnHit(byte damage, BasePlayer damager)
        {
            _health -= damage;
            if (_health <= 0)
                _playerManager.OnPlayerDeath(this, damager);
        }
        
        public virtual void NotifyHit(HitInfo hit)
        {
            // TODO: find a way to do cleaner event handling
            // do not implement
        }
        
        public virtual void OnHardpointAction(HardpointAction action)
        {
            // todo: find a way to do cleaner event handling
            // dot not implement
        }

        public abstract void ApplyInput(PlayerInputPacket command, float delta);
        // {
        //     Vector2 velocity = Vector2.zero;
        //     
        //     if ((command.Keys & MovementKeys.Up) != 0)
        //         velocity.y = -1f;
        //     if ((command.Keys & MovementKeys.Down) != 0)
        //         velocity.y = 1f;
        //     
        //     if ((command.Keys & MovementKeys.Left) != 0)
        //         velocity.x = -1f;
        //     if ((command.Keys & MovementKeys.Right) != 0)
        //         velocity.x = 1f;     
        //     
        //     _position += velocity.normalized * (_speed * delta);
        //     _rotation = command.Rotation;
        //
        //     if ((command.Keys & MovementKeys.Fire) != 0)
        //     {
        //         if (_shootTimer.IsTimeElapsed)
        //         {
        //             _shootTimer.Reset();
        //             Shoot();
        //         }
        //     }
        //     
        // }
        
        public abstract void FrameUpdate(float delta);

        public virtual void Update(float delta)
        {
            _shootTimer.UpdateAsCooldown(delta);
            
            // update hardpoints
            for (var i = 0; i < _ship.Hardpoints.Count; i++)
            {
                var slot = _ship.Hardpoints[i];
                slot.Hardpoint.Update(delta);
                if (slot.Hardpoint.HasAction(out var action))
                {
                    var hardpointAction = new HardpointAction
                    {
                        SlotId = slot.Id,
                        ActionCode = action.ActionCode
                    };
                    _playerManager.OnHardpointAction(this, hardpointAction);
                    
                    // handle hardpoint actions
                    // for now all hardpoint fire actions are the same
                    if (action.ActionCode == 1)
                    {
                        // calculate the target position
                        // convert the hardpoint position to world space
                        Vector2 mountPosition = (Vector2)slot.Position * 1/32f;
                        Vector2 rotatedMountPosition = Quaternion.Euler(0, 0, _rotation * Mathf.Rad2Deg) * mountPosition;
                        Vector2 fireWorldPosition = _position + rotatedMountPosition;
                        
                        float hardpointSignedAngle = Mathf.DeltaAngle(0f, slot.Hardpoint.Rotation);
                        // Hardpoint angle is in Degrees and we need to convert it to Radians
                        float shotWorldAngle = hardpointSignedAngle * Mathf.Deg2Rad + _rotation;
                        
                        const float MaxLength = 20f;
                        Vector2 dir = new Vector2(Mathf.Cos(shotWorldAngle), Mathf.Sin(shotWorldAngle));
                        var player = _playerManager.CastToPlayer(fireWorldPosition, dir, MaxLength, this);
                        Vector2 target = fireWorldPosition + dir * (player != null ? Vector2.Distance(fireWorldPosition, player._position) : MaxLength);
                        _playerManager.OnShoot(this, slot.Id, target, player, Damage);

                        Debug.DrawLine(fireWorldPosition, target, Color.red, 0.5f);
                    }
                }
            }
        }
        
        public PlayerInitialInfo GetInitialInfo()
        {
            var hardpointsData = new HardpointSlotInfo[_ship.Hardpoints.Count];
            for (int i = 0; i < _ship.Hardpoints.Count; i++)
            {
                hardpointsData[i] = new HardpointSlotInfo
                {
                    Id = _ship.Hardpoints[i].Id,
                    Type = _ship.Hardpoints[i].Hardpoint.Type,
                    X = _ship.Hardpoints[i].Position.x,
                    Y = _ship.Hardpoints[i].Position.y,
                };
            }
            
            return new PlayerInitialInfo
            {
                Id = Id,
                UserName = Name,
                ShipType = _ship.Type,
                PrimaryColor = Utils.EncodeColor(_primaryColor),
                SecondaryColor = Utils.EncodeColor(_secondaryColor),
                Health = Health,
                NumHardpointSlots = (byte) _ship.Hardpoints.Count,
                Hardpoints = hardpointsData,
            };
        }
    }
}

