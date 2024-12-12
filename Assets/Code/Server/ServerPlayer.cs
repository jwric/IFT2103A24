using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        private ServerPlayerView _playerView;
        private readonly ServerPlayerManager _playerManager;
        public readonly NetPeer AssociatedPeer;
        public PlayerState NetworkState;
        public ushort LastProcessedCommandId { get; private set; }
        public float LastProcessedCommandTime { get; private set; }
        public ushort LastProcessedCommandTick { get; private set; }
        private ushort _lastTickDiff;
        
        public PlayerInputPacket LastProcessedCommand { get; private set; }
        
        private float _tickTime = 0f;
        private bool _isFirstStateReceived = false;
        
        public int TickUpdateCount { get; private set; }

        public ServerPlayer(ServerPlayerManager playerManager, string name, byte id, NetPeer peer) : base(playerManager, name, id)
        {
            // Add hardpoints
            // for now this will set to a single cannon hardpoint for all players
            Hardpoints.Add(new HardpointSlot(0, HardpointFactory.CreateHardpoint(HardpointType.Cannon), new Vector2Int(-4, 0)));
            // Hardpoints.Add(new HardpointSlot(1, HardpointFactory.CreateHardpoint(HardpointType.Cannon), new Vector2Int(-4, -20)));
            
            _playerManager = playerManager;
            AssociatedPeer = peer;
            peer.Tag = this;  // Allows easy identification of this player through the peer
            
            NetworkState = new PlayerState
            {
                Id = Id, 
                NumHardpoints = (byte)Hardpoints.Count, 
                Hardpoints = new HardpointState[Hardpoints.Count]
            };
            
            for (int i = 0; i < Hardpoints.Count; i++)
            {
                NetworkState.Hardpoints[i] = new HardpointState
                {
                    Id = Hardpoints[i].Id,
                    Rotation = Hardpoints[i].Hardpoint.Rotation
                };
            }
            
            // Set the player's initial state
            NetworkState.Position = Vector2.zero;
            NetworkState.Velocity = Vector2.zero;
            NetworkState.Rotation = 0f;
            NetworkState.AngularVelocity = 0f;
            NetworkState.Tick = 0;
            NetworkState.Time = 0f;
            NetworkState.Health = 100;
        }
        
        public void SetPlayerView(ServerPlayerView playerView)
        {
            _playerView = playerView;
        }
        
        public void ApplyForce(Vector2 force)
        {
            _playerView.Move(force);
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

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            // Ensure only new commands are processed
            if (NetworkGeneral.SeqDiff(command.Id, LastProcessedCommandId) <= 0)
            {
                // var gap = NetworkGeneral.SeqDiff(LastProcessedCommandId, command.Id);
                // Debug.Log($"Player {Id} received an old command {command.Id} (last processed {LastProcessedCommandId})");
                return;
            }
            
            
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
                    _tickTime = 0f;
                    Debug.LogWarning($"Player {Id} sent too many commands in a single tick: {TickUpdateCount}");
                    TickUpdateCount = 0;
                    // log how many commands were received from player in the last tick
                    Debug.Log($"Player {Id} received {tickDiff} commands in the last tick");
                    _lastTickDiff = (ushort)tickDiff;
                }
            }

            _tickTime += delta;
            TickUpdateCount++;
            
            const float MaxAllowedTime = 1/30f;
            float margin = Mathf.Min(delta, MaxAllowedTime) * 3;
            float maxTime = LogicTimerServer.FixedDelta * _lastTickDiff + margin;
            
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
                // _playerView.SetRotation(command.Rotation);

                // disable other player's physics

                // _position = _playerView.Position;
                // _velocity = _playerView.Velocity;
                // _rotation = _playerView.Rotation;
                // _angularVelocity = _playerView.AngularVelocity;
                // _rotation = command.Rotation;

                // deprecated for new hardpoint system
                // if ((command.Keys & MovementKeys.Fire) != 0)
                // {
                //     if (_shootTimer.IsTimeElapsed)
                //     {
                //         _shootTimer.Reset();
                //         Shoot();
                //     }
                // }
                
                // Apply hardpoint actions and state
                for (int i = 0; i < command.NumHardpoints; i++)
                {
                    var hardpointState = command.Hardpoints[i];
                    // optimise this
                    var correspondingHardpoint = Hardpoints.Find(h => h.Id == hardpointState.Id);
                    if (correspondingHardpoint == null)
                    {
                        Debug.LogWarning($"Player {Id} received a command for an unknown hardpoint {hardpointState.Id}");
                        continue;
                    }
                    correspondingHardpoint.Hardpoint.SetRotation(hardpointState.Rotation);

                    bool isFiring = hardpointState.Fire;
                    correspondingHardpoint.Hardpoint.SetTriggerHeld(isFiring);
                }
            }
            
        }

        // Updates the player’s state and prepares it for network synchronization
        public override void Update(float delta)
        {
            base.Update(delta);

            // // apply forces based on the player's last input
            // {
            //     var keys = LastProcessedCommand.Keys;
            //     Vector2 velocity = Vector2.zero;
            //     if ((keys & MovementKeys.Up) != 0)
            //         velocity.y = -1f;
            //     if ((keys & MovementKeys.Down) != 0)
            //         velocity.y = 1f;
            //     if ((keys & MovementKeys.Left) != 0)
            //         velocity.x = -1f;
            //     if ((keys & MovementKeys.Right) != 0)
            //         velocity.x = 1f;
            //     
            //     if (velocity != Vector2.zero)
            //         _playerView.Move(velocity.normalized * _speed);
            // }
            
            // set the player's position, rotation, and velocity to the player view
            _position = _playerView.Position;
            _rotation = _playerView.Rotation;
            _velocity = _playerView.Velocity;
            _angularVelocity = _playerView.AngularVelocity;
            
            
            // Update the network state with the player's latest position, rotation, and tick information
            NetworkState.Position = _position;
            NetworkState.Velocity = _velocity;
            NetworkState.Rotation = _rotation;
            NetworkState.AngularVelocity = _angularVelocity;
            NetworkState.Tick = LastProcessedCommandId;
            NetworkState.Time = Time.time;
            NetworkState.Health = _health;
            
            // Update the hardpoints
            for (int i = 0; i < Hardpoints.Count; i++)
            {
                NetworkState.Hardpoints[i].Rotation = Hardpoints[i].Hardpoint.Rotation;
            }
            
            // Debug.Log($"Player {Id} updated to tick {NetworkState.Tick} at {NetworkState.Time}");
            // Draw a cross at the player's position for visual debugging
            DrawDebugCross(Position, 0.1f, Color.white);
        }

        public override void FrameUpdate(float delta)
        {
        }

        // Utility function to draw a cross at the player's position
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
