using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        private readonly ServerPlayerManager _playerManager;
        public readonly NetPeer AssociatedPeer;
        public PlayerState NetworkState;
        public ushort LastProcessedCommandId { get; private set; }
        public float LastProcessedCommandTime { get; private set; }
        public ushort LastProcessedCommandTick { get; private set; }
        private ushort _lastTickDiff;
        
        private float _tickTime = 0f;
        private bool _isFirstStateReceived = false;
        private int _numExceeding;
        
        public int TickUpdateCount { get; private set; }

        public ServerPlayer(ServerPlayerManager playerManager, string name, NetPeer peer) : base(playerManager, name, (byte)peer.Id)
        {
            _playerManager = playerManager;
            AssociatedPeer = peer;
            peer.Tag = this;  // Allows easy identification of this player through the peer
            NetworkState = new PlayerState { Id = (byte)peer.Id };
        }

        // Applies player input if it's newer than the last processed command
        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            // Ensure only new commands are processed
            if (NetworkGeneral.SeqDiff(command.Id, LastProcessedCommandId) <= 0)
            {
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
                    // Debug.LogWarning($"Player {Id} received a command from the past: {command.ServerTick} (last processed {LastProcessedCommandTick})");
                    return;
                }
                // New tick, reset tick time
                if (tickDiff > 0)
                {
                    Debug.Log($"Num exceeding: {_numExceeding} (total {TickUpdateCount} updates)");
                    // New tick, reset tick time
                    _tickTime = 0f;
                    TickUpdateCount = 0;
                    _lastTickDiff = (ushort)tickDiff;
                    _numExceeding = 0;
                }
            }

            _tickTime += delta;
            TickUpdateCount++;
            
            const float MaxAllowedTime = 1/30f;
            float margin = Mathf.Min(delta, MaxAllowedTime) * 3;
            float maxTime = LogicTimerServer.FixedDelta * _lastTickDiff + margin;
            // Check if the tick time exceeds the fixed delta
            if (_tickTime > maxTime)
            {
                Debug.LogWarning($"Player {Id} tick time exceeded: {_tickTime} (max {LogicTimerServer.FixedDelta*_lastTickDiff} ({_lastTickDiff} tick updates)), after {TickUpdateCount} updates");
                _numExceeding++;
                return;
            }
            
            // float timeDiff = command.Time - LastProcessedCommandTime;
            // Debug.Log($"Player {Id} received command {command.Id} with time diff {timeDiff}");
            
            
            // Update last processed command ID and apply the input
            LastProcessedCommandId = command.Id;
            // register the time of the last processed command
            LastProcessedCommandTime = command.Time;
            // Update the tick of the last processed command
            LastProcessedCommandTick = command.ServerTick;
            base.ApplyInput(command, delta);
        }

        // ChangeState method to set position and rotation
        public void ChangeState(Vector2 newPosition, bool applyImmediately = false)
        {
            _position = newPosition;

            if (applyImmediately)
            {
                NetworkState.Position = _position;
            }
        }
        
        // Updates the player’s state and prepares it for network synchronization
        public override void Update(float delta)
        {
            base.Update(delta);

            // Update the network state with the player's latest position, rotation, and tick information
            NetworkState.Position = _position;
            NetworkState.Rotation = _rotation;
            NetworkState.Tick = LastProcessedCommandId;
            NetworkState.Time = LastProcessedCommandTime;
            // Debug.Log($"Player {Id} updated to tick {NetworkState.Tick} at {NetworkState.Time}");
            // Draw a cross at the player's position for visual debugging
            DrawDebugCross(Position, 0.1f, Color.white);
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
