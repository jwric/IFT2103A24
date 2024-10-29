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

            // float timeDiff = command.Time - LastProcessedCommandTime;
            // Debug.Log($"Player {Id} received command {command.Id} with time diff {timeDiff}");
            
            
            // Update last processed command ID and apply the input
            LastProcessedCommandId = command.Id;
            // register the time of the last processed command
            LastProcessedCommandTime = command.Time;
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
