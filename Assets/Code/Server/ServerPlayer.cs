using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        public int inputHasBeenProcessed = 0;
        private ServerPlayerView _playerView;
        private readonly ServerPlayerManager _playerManager;
        public readonly NetPeer AssociatedPeer;
        public PlayerState NetworkState;
        public ushort LastProcessedCommandId { get; private set; }
        public float LastProcessedCommandTime { get; private set; }
        public ushort LastProcessedCommandTick { get; private set; }

        public ushort LastReceivedCommandId { get; private set; }
        private ushort _lastTickDiff;
        
        private int _lastInputBufferSize = 0;

        public PlayerInputPacket LastProcessedCommand { get; private set; }

        private float _tickTime = 0f;
        private bool _isFirstStateReceived = false;

        public int TickUpdateCount { get; private set; }

        public ServerPlayer(ServerPlayerManager playerManager, string name, ShipType shipType, byte id, NetPeer peer) : base(playerManager,
            name, id)
        {
            // create ship
            _ship = ShipFactory.CreateShip(shipType);
            
            _playerManager = playerManager;
            AssociatedPeer = peer;
            peer.Tag = this; // Allows easy identification of this player through the peer

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
            NetworkState.Time = Time.time;
            NetworkState.Health = 100;

            LastProcessedCommandId = NetworkGeneral.MaxGameSequence - 1;
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

        private readonly Queue<PlayerInputPacket> _inputQueue = new();

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            // Ensure only new commands are processed
            if (_isFirstStateReceived && NetworkGeneral.SeqDiff(command.Id, LastReceivedCommandId) <= 0)
            {
                // var gap = NetworkGeneral.SeqDiff(LastProcessedCommandId, command.Id);
                // Debug.Log($"Player {Id} received an old command {command.Id} (last processed {LastProcessedCommandId})");
                return;
            }
            _isFirstStateReceived = true;

            LastReceivedCommandId = command.Id;

            // Queue the input command for processing movement
            _inputQueue.Enqueue(command);
            
            // Update hardpoints
            // Update hardpoints
            foreach (var hardpointState in command.Hardpoints)
            {
                var correspondingHardpoint = Hardpoints.Find(h => h.Id == hardpointState.Id);
                if (correspondingHardpoint == null)
                {
                    Debug.LogWarning($"Player {Id} received a command for an unknown hardpoint {hardpointState.Id}");
                    continue;
                }

                if (hardpointState.Fire)
                {
                    Debug.Log($"Player {Id} fired hardpoint {hardpointState.Id}");
                }
                
                correspondingHardpoint.Hardpoint.SetRotation(hardpointState.Rotation);
                correspondingHardpoint.Hardpoint.SetTriggerHeld(hardpointState.Fire);
            }
        }

        public void PreUpdate()
        {
            const int MaxCommandsPerTick = 1; // Process at most 2 commands per physics tick
            const int MaxBufferSize = 10; // Maximum allowed buffer size to prevent extreme lag

            if (_inputQueue.Count == 0)
                return;

            // Warn if the buffer grows too large
            if (_inputQueue.Count > MaxBufferSize)
            {
                Debug.LogWarning(
                    $"Player {Id} input buffer too large ({_inputQueue.Count} commands). Dropping oldest inputs.");
                // Drop oldest inputs until the buffer is manageable
                while (_inputQueue.Count > MaxBufferSize)
                {
                    _inputQueue.Dequeue();
                }
            }

            // Process the input commands
            int commandsProcessed = 0;
            while (_inputQueue.Count > 0 && commandsProcessed < MaxCommandsPerTick)
            {
                ProcessCommand(_inputQueue.Dequeue());
                commandsProcessed++;
            }

            _lastInputBufferSize = _inputQueue.Count;
        }

        private void ProcessCommand(PlayerInputPacket command)
        {
            // Update last processed command ID and apply the input
            LastProcessedCommandId = command.Id;
            LastProcessedCommandTime = command.Time;
            LastProcessedCommandTick = command.ServerTick;

            // Apply forces
            var calculatedForce = _ship.CalculateDirThrustForce(command.Thrust);
            var calculatedTorque = _ship.CalculateAngularThrustTorque(command.AngularThrust);
            var rotatedForce = Quaternion.Euler(0, 0, _rotation * Mathf.Rad2Deg) * calculatedForce;
            _playerView.Move(rotatedForce);
            _playerView.Rotate(calculatedTorque);

            inputHasBeenProcessed++;
        }


        // Updates the player’s state and prepares it for network synchronization
        public override void Update(float delta)
        {
            base.Update(delta);

            if (inputHasBeenProcessed == 0)
            {
                Debug.LogWarning($"Player {Id} did not process input for tick {LastProcessedCommandTick}");
            }
            else if (inputHasBeenProcessed > 1)
            {
                Debug.LogWarning(
                    $"Player {Id} processed too many inputs for tick {LastProcessedCommandTick} => {inputHasBeenProcessed}");
            }
            
            inputHasBeenProcessed = 0;


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
            NetworkState.Tick = (ushort)((NetworkState.Tick + 1) % NetworkGeneral.MaxGameSequence);
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