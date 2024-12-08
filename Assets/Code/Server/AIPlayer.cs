using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class AIPlayer : BasePlayer
    {
        private ServerPlayerView _playerView;
        private readonly ServerPlayerManager _playerManager;
        public PlayerState NetworkState;
        public ushort LastProcessedCommandId { get; private set; }
        public float LastProcessedCommandTime { get; private set; }
        public ushort LastProcessedCommandTick { get; private set; }
        private ushort _lastTickDiff;
        
        public PlayerInputPacket LastProcessedCommand { get; private set; }
        
        private float _tickTime = 0f;
        private bool _isFirstStateReceived = false;
        private int _numExceeding;
        
        private BasePlayer _lastDamager = null;
        
        private enum AIState
        {
            Patrol,
            Attack
        }

        private AIState _currentState = AIState.Patrol;
        private Vector2 _patrolTarget;
        private float _patrolRange = 5f;
        private BasePlayer _targetPlayer;
        private float _attackRange = 5f;
        private float _stateChangeCooldown = 1f;
        private float _stateChangeTimer = 0f;
        
        public int TickUpdateCount { get; private set; }

        public AIPlayer(ServerPlayerManager playerManager, string name, byte id, int serverTick) : base(playerManager, name, id)
        {
            _playerManager = playerManager;
            NetworkState = new PlayerState { Id = (byte)id };
            
            LastProcessedCommandTick = (ushort)serverTick;
            
            _patrolTarget = Position + Random.insideUnitCircle * _patrolRange;
        }
        
        public void SetPlayerView(ServerPlayerView playerView)
        {
            _playerView = playerView;
        }


        public override void Spawn(Vector2 position)
        {
            base.Spawn(position);
            _playerView.Spawn(position);
        }
        
        public void Die()
        {
            _playerView.Die();
        }

        // Applies player input if it's newer than the last processed command
        public override void ApplyInput(PlayerInputPacket command, float delta)
        {

            if (!_isFirstStateReceived)
            {
                _isFirstStateReceived = true;
                _lastTickDiff = 1;
            }
            else
            {
                int tickDiff = NetworkGeneral.SeqDiff(command.ServerTick, LastProcessedCommandTick);
                // Old tick, ignore
                if (tickDiff < 0)
                {
                    Debug.LogWarning($"Player {Id} received a command from the past: {command.ServerTick} (last processed {LastProcessedCommandTick})");
                    return;
                }
                // New tick, reset tick time
                if (tickDiff > 0)
                {
                    // Debug.Log($"Num exceeding: {_numExceeding} (total {TickUpdateCount} updates)");
                    // New tick, reset tick time
                    _tickTime = 0f;
                    TickUpdateCount = 0;
                    _lastTickDiff = (ushort)tickDiff;
                    _numExceeding = 0;
                }
            }

            _tickTime += delta;
            TickUpdateCount++;
            
            // Update last processed command ID and apply the input
            LastProcessedCommandId = command.Id;
            // register the time of the last processed command
            LastProcessedCommandTime = command.Time;
            // Update the tick of the last processed command
            LastProcessedCommandTick = command.ServerTick;
            LastProcessedCommand = command;

            // Apply the input command
            {
                _playerView.Move(command.Thrust * _speed);
                _playerView.Rotate(command.AngularThrust * _angularSpeed);


                _position = _playerView.Position;
                _velocity = _playerView.Velocity;
                _rotation = _playerView.Rotation;
                _angularVelocity = _playerView.AngularVelocity;

                if ((command.Keys & MovementKeys.Fire) != 0)
                {
                    if (_shootTimer.IsTimeElapsed)
                    {
                        _shootTimer.Reset();
                        Shoot();
                    }
                }
            }
            
        }

        public override void NotifyHit(HitInfo hit)
        {
            _lastDamager = hit.Damager;
        }

        public override void Update(float delta)
        {
            base.Update(delta);
            _stateChangeTimer -= delta;

            switch (_currentState)
            {
                case AIState.Patrol:
                    HandlePatrolState(delta);
                    break;

                case AIState.Attack:
                    HandleAttackState(delta);
                    break;
            }

            UpdateNetworkState();
        }

        public override void FrameUpdate(float delta)
        {
        }

        private void HandlePatrolState(float delta)
        {
            float distanceToTarget = Vector2.Distance(Position, _patrolTarget);
            float safeDistance = 0.5f;

            float thrust = CalculateThrust(distanceToTarget, safeDistance);

            Vector2 direction = (_patrolTarget - Position).normalized;
            float angularThrust = CalculateAngularThrust(direction);

            MoveTowards(_patrolTarget, delta, angularThrust, thrust);

            if (distanceToTarget <= safeDistance)
            {
                _patrolTarget = Position + Random.insideUnitCircle * _patrolRange;
            }

            // Check for nearby players to attack
            _targetPlayer = GetClosestPlayerWithinRange(_attackRange);
            if (_targetPlayer != null && _stateChangeTimer <= 0f)
            {
                _currentState = AIState.Attack;
                _stateChangeTimer = _stateChangeCooldown;
            }
            
            // Check for the last damager to attack
            if (_lastDamager != null && _stateChangeTimer <= 0f)
            {
                _targetPlayer = _lastDamager;
                _currentState = AIState.Attack;
                _stateChangeTimer = _stateChangeCooldown;
            }
        }
        
        private float CalculateAngularThrust(Vector2 targetDirection)
        {
            float targetRotation = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;

            // Calculate the angle difference
            float rotationDiff = GetAngleDifference(_rotation * Mathf.Rad2Deg, targetRotation);

            // Proportional and Derivative Control
            float kP = 0.5f;
            float kD = 0.1f;
            float torque = (kP * rotationDiff) - (kD * _angularVelocity);

            // Clamp the torque input to [-1, 1]
            return Mathf.Clamp(torque, -1f, 1f);
        }
        
        private void HandleAttackState(float delta)
        {
            if (_targetPlayer is not { IsAlive: true })
            {
                _currentState = AIState.Patrol;
                return;
            }

            Vector2 direction = (_targetPlayer.Position - Position).normalized;
            float distanceToTarget = Vector2.Distance(Position, _targetPlayer.Position);

            float safeDistance = 1.5f;

            float thrust = CalculateThrust(distanceToTarget, safeDistance);
            float angularThrust = CalculateAngularThrust(direction);

            MoveTowards(_targetPlayer.Position, delta, angularThrust, thrust);

            var angleDiff = GetAngleDifference(_rotation * Mathf.Rad2Deg, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (Mathf.Abs(angleDiff) < 10f && distanceToTarget <= _attackRange)
                SimulateAttack();

            if (distanceToTarget > _attackRange + safeDistance)
            {
                _currentState = AIState.Patrol;
            }
        }

        private float CalculateThrust(float distanceToTarget, float safeDistance)
        {
            // PD control parameters
            float kP = 1.0f; // Proportional gain
            float kD = 0.5f; // Derivative gain

            // Distance error (how far we are from the safe distance)
            float error = distanceToTarget - safeDistance;

            // Derivative term (rate of change of error)
            float derivative = -_velocity.magnitude; // Using current velocity as an approximation

            // Calculate thrust
            float thrust = (kP * error) + (kD * derivative);

            // Clamp thrust to prevent overshooting or reversing
            return Mathf.Clamp(thrust, 0f, 1f);
        }

        private void MoveTowards(Vector2 target, float delta, float angularThrust, float thrust)
        {
            Vector2 direction = (target - Position).normalized;
            var simulatedInput = new PlayerInputPacket
            {
                Thrust = direction * thrust, // Adjust thrust dynamically
                AngularThrust = angularThrust,
                Delta = delta,
                Id = (ushort)((LastProcessedCommandTick + 1) % NetworkGeneral.MaxGameSequence),
                Time = Time.time,
                ServerTick = (ushort)((LastProcessedCommandTick + 1) % NetworkGeneral.MaxGameSequence)
            };
            ApplyInput(simulatedInput, delta);
        }
        
        public static float GetAngleDifference(float angle1, float angle2)
        {
            float difference = angle2 - angle1;
            difference = (difference + 180) % 360;
            if (difference < 0)
            {
                difference += 360;
            }
            return difference - 180;
        }
        
        private void SimulateAttack()
        {
            if (_shootTimer.IsTimeElapsed)
            {
                _shootTimer.Reset();
                Shoot();
            }
        }

        private BasePlayer GetClosestPlayerWithinRange(float range)
        {
            BasePlayer closest = null;
            float minDistance = range;

            foreach (var player in _playerManager)
            {
                if (player == this)
                    continue;

                float distance = Vector2.Distance(Position, player.Position);
                if (distance < minDistance)
                {
                    closest = player;
                    minDistance = distance;
                }
            }

            return closest;
        }

        private void UpdateNetworkState()
        {
            NetworkState.Position = _playerView.Position;
            NetworkState.Velocity = _playerView.Velocity;
            NetworkState.Rotation = _playerView.Rotation;
            NetworkState.AngularVelocity = _playerView.AngularVelocity;
            NetworkState.Tick = LastProcessedCommandId;
            NetworkState.Time = Time.time;
            NetworkState.Health = _health;
        }
        
        private void DrawDebugCross(Vector2 position, float size, Color color)
        {
            Debug.DrawLine(
                new Vector2(position.x - size, position.y),
                new Vector2(position.x + size, position.y),
                color);
            Debug.DrawLine(
                new Vector2(position.x, position.y - size),
                new Vector2(position.x, position.y + size),
                color);
        }
        
        public void DrawGizmos()
        {
            // draw rotation
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Position, Position + new Vector2(Mathf.Cos(_rotation), Mathf.Sin(_rotation)));
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(Position, 0.1f);
            
        }
        
        public string GetDebugInfo()
        {
            return $"\n---- Player {Id} ----" 
                + $"\nPosition: {Position}"
                + $"\nRotation: {_rotation}"
                + $"\nLastProcessedCommandId: {LastProcessedCommandId}"
                + $"\nLastProcessedCommandTime: {LastProcessedCommandTime}";
        }
    }
}
