using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public class RemotePlayer : BasePlayer
    {
        private RemotePlayerView _view;
        
        private readonly LiteRingBuffer<PlayerState> _buffer = new LiteRingBuffer<PlayerState>(30);
        private float _bufferTime; // Running time in buffer
        private float _interpolationTimer;
        private const float TargetBufferTime = 0.1f; // Target buffer time in seconds (100 ms)

        public RemotePlayer(ClientPlayerManager manager, string name, PlayerJoinedPacket pjPacket) : base(manager, name, pjPacket.InitialPlayerState.Id)
        {
            _position = pjPacket.InitialPlayerState.Position;
            _health = pjPacket.Health;
            _rotation = pjPacket.InitialPlayerState.Rotation;
            _buffer.Add(pjPacket.InitialPlayerState);
        }
        
        public Transform Transform => _view.transform;
        
        public void SetPlayerView(RemotePlayerView view)
        {
            _view = view;
        }

        public override void Spawn(Vector2 position)
        {
            _buffer.FastClear();
            _view.Spawn(position, 0f);
            base.Spawn(position);
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            // No input processing for remote players
        }

        public void UpdatePosition(float delta)
        {
            // Only proceed if we have enough data for interpolation or apply adaptive smoothing
            if (_buffer.Count < 2)
            {
                // If we only have one state left, use it with adaptive smoothing to avoid choppiness
                if (_buffer.Count == 1)
                {
                    var singleData = _buffer[0];
                    _position = Vector2.Lerp(_position, singleData.Position, delta * 0.5f); // Reduced speed
                    _rotation = Mathf.Lerp(_rotation, singleData.Rotation, delta * 0.5f); // Reduced speed
                    _velocity = Vector2.Lerp(_velocity, singleData.Velocity, delta * 0.5f); // Reduced speed
                    _angularVelocity = Mathf.Lerp(_angularVelocity, singleData.AngularVelocity, delta * 0.5f); // Reduced speed
                }
                return;
            }

            var dataA = _buffer[0];
            var dataB = _buffer[1];

            // Calculate time between the two states
            float stateDeltaTime = dataB.Time - dataA.Time;
            if (stateDeltaTime <= 0) return;

            // Calculate interpolation factor with error correction for lag
            float lerpT = _interpolationTimer / stateDeltaTime;
            _position = Vector2.Lerp(dataA.Position, dataB.Position, lerpT);
            // _position = dataB.Position; // No interpolation for position

            // Smooth rotation interpolation handling wrap-around at 360 degrees
            float startRotation = dataA.Rotation;
            float endRotation = dataB.Rotation;
            if (Mathf.Abs(startRotation - endRotation) > Mathf.PI)
            {
                if (startRotation < endRotation)
                    startRotation += Mathf.PI * 2f;
                else
                    startRotation -= Mathf.PI * 2f;
            }
            _rotation = Mathf.Lerp(startRotation, endRotation, lerpT);
            
            _velocity = Vector2.Lerp(dataA.Velocity, dataB.Velocity, lerpT);
            
            _angularVelocity = Mathf.Lerp(dataA.AngularVelocity, dataB.AngularVelocity, lerpT);

            // Update interpolation timer
            _interpolationTimer += delta;

            // Move to the next buffer state when interpolation completes
            if (_interpolationTimer >= stateDeltaTime)
            {
                _buffer.RemoveFromStart(1);
                _interpolationTimer -= stateDeltaTime;
                _bufferTime -= stateDeltaTime;
            }
            
            // Adaptive smoothing based on buffer count and buffer time
            if (_buffer.Count < 3 && _bufferTime < TargetBufferTime * 0.5f)
            {
                _interpolationTimer *= 0.8f; // Reduce interpolation rate to smooth out movement
            }
        }

        public void OnPlayerState(PlayerState state)
        {
            // Skip outdated states
            // if (_buffer.Count > 0 && NetworkGeneral.SeqDiff(state.Tick, _buffer.Last.Tick) <= 0)
            //     return;

            // Determine if time adjustment is needed
            float timeDiff = state.Time - (_buffer.Count > 0 ? _buffer.Last.Time : 0f);
            // if (timeDiff < LogicTimerClient.FixedDelta * 0.5f)
            // {
            //     Debug.LogWarning($"Skipping outdated state with diff {timeDiff}");
            //     return;
            // }

            if (timeDiff < 0)
            {
                return;
            }
            
            _bufferTime += timeDiff;
            // Prevent excessive buffering by dynamically adjusting the buffer
            if (_bufferTime > TargetBufferTime * 1.5f)
            {
                int i = 0;
                while (_buffer.Count > 2 && _bufferTime > TargetBufferTime)
                {
                    i++;
                    _buffer.RemoveFromStart(1);
                    _bufferTime -= timeDiff;
                }
                if (i > 0)
                    Debug.LogWarning($"[C] Remote: Lag detected, cleared {i} buffer entries"); 
            }
            
            _buffer.Add(state);
        }

        public void Die()
        {
            _view.Die();
        }
        
        public string GetDebugInfo()
        {
            return $"\n---- Player {Id} ----" +
                   $"\nBuffer Count: {_buffer.Count}" +
                   $"\nBuffer Time: {_bufferTime}" +
                   $"\nInterpolation Timer: {_interpolationTimer}" +
                   $"\nTarget Buffer Time: {TargetBufferTime}";
        }
        
    }
}
