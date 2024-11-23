using UnityEngine;

namespace Code.Shared
{
    public abstract class BasePlayer
    {
        public readonly string Name;

        protected float _speed = 7f;
        protected float _angularSpeed = 0.5f;
        protected GameTimer _shootTimer = new GameTimer(0.2f);
        protected byte Damage = 10;
        private BasePlayerManager _playerManager;
        
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
            _health = 100;
        }

        protected void Shoot()
        {
            const float MaxLength = 20f;
            Vector2 dir = new Vector2(Mathf.Cos(_rotation), Mathf.Sin(_rotation));
            var player = _playerManager.CastToPlayer(_position, dir, MaxLength, this);
            Vector2 target = _position + dir * (player != null ? Vector2.Distance(_position, player._position) : MaxLength);
            _playerManager.OnShoot(this, target, player, Damage);
        }
        
        public void OnHit(byte damage, BasePlayer damager)
        {
            _health -= damage;
            if (_health <= 0)
                _playerManager.OnPlayerDeath(this, damager);
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

        public virtual void Update(float delta)
        {
            _shootTimer.UpdateAsCooldown(delta);
        }
    }
}

