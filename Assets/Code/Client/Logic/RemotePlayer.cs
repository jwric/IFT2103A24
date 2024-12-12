using Code.Client.Managers;
using Code.Shared;
using UnityEngine;
using System;

namespace Code.Client.Logic
{
    public class RemotePlayer : BasePlayer
    {
        private PlayerView _view;

        private readonly LiteRingBuffer<PlayerState> _buffer = new(30);
        private float _bufferTime;
        private float _interpolationTimer;
        private const float TargetBufferTime = 0.1f;

        // Store last frame's state for acceleration calculation
        private Vector2 _lastVelocity;
        private float _lastAngularVelocity;
        private bool _hasLastFrameState = false;

        public RemotePlayer(ClientPlayerManager manager, string name, PlayerJoinedPacket pjPacket) 
            : base(manager, name, pjPacket.InitialPlayerState.Id)
        {
            _position = pjPacket.InitialPlayerState.Position;
            _health = pjPacket.InitialInfo.Health;
            _rotation = pjPacket.InitialPlayerState.Rotation;
            _buffer.Add(pjPacket.InitialPlayerState);
            
            // Add hardpoints
            for (var index = 0; index < pjPacket.InitialInfo.Hardpoints.Length; index++)
            {
                var slot = pjPacket.InitialInfo.Hardpoints[index];
                // var state = pjPacket.InitialPlayerState.Hardpoints[index];

                var hardpoint = HardpointFactory.CreateHardpoint(slot.Type);
                // hardpoint.SetRotation(state.Rotation);
                Hardpoints.Add(new HardpointSlot(slot.Id, hardpoint, new Vector2Int(slot.X, slot.Y)));
            }
            
            // set kinematic
        }
        
        public Transform Transform => _view.transform; // TODO: Remove this
        
        public void SetPlayerView(PlayerView view)
        {
            _view = view;
        }

        public override void Spawn(Vector2 position)
        {
            _buffer.FastClear();
            _view.Spawn(position, 0f);
            base.Spawn(position);
            
            _view.Rb.isKinematic = true;
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            // Do nothing for remote players
        }
        
        public Vector2 GetViewHardpointFirePosition(byte id)
        {
            _view.GetHardpointView(id, out var hardpointView);
            return hardpointView?.GetFirePosition() ?? Position;
        }

        public override void OnHardpointAction(HardpointAction action)
        {
            _view.GetHardpointView(action.SlotId, out var hardpointView);
            hardpointView?.OnHardpointAction(action.ActionCode);
        }

        public override void FrameUpdate(float delta)
        {
        }

        public override void NotifyHit(HitInfo hit)
        {
            _view.OnHit(hit);
        }

        public void UpdateView()
        {
            _view.Rb.MovePosition(Position);
            _view.Rb.MoveRotation(Rotation * Mathf.Rad2Deg);
            _view.Rb.velocity = Velocity;
            _view.Rb.angularVelocity = AngularVelocity;
            
            // update hardpoint views
            for (int i = 0; i < Hardpoints.Count; i++)
            {
                var slot = Hardpoints[i];
                var hardpoint = slot.Hardpoint;
                _view.GetHardpointView(slot.Id, out var hardpointView);
                hardpointView?.CurrentRotation(hardpoint.Rotation);
            }
        }
        
        public void UpdatePosition(float delta)
        {
            if (_buffer.Count < 2)
            {
                if (_buffer.Count == 1)
                {
                    var singleData = _buffer[0];
                    _position = Vector2.Lerp(_position, singleData.Position, delta * 0.5f); 
                    _rotation = Mathf.Lerp(_rotation, singleData.Rotation, delta * 0.5f); 
                    _velocity = Vector2.Lerp(_velocity, singleData.Velocity, delta * 0.5f); 
                    _angularVelocity = Mathf.Lerp(_angularVelocity, singleData.AngularVelocity, delta * 0.5f); 

                    // update hardpoints
                    for (int i = 0; i < Hardpoints.Count; i++)
                    {
                        Hardpoints[i].Hardpoint.SetRotation(
                            Mathf.LerpAngle(Hardpoints[i].Hardpoint.Rotation, singleData.Hardpoints[i].Rotation, delta * 0.5f));
                    }
                }
                return;
            }

            var dataA = _buffer[0];
            var dataB = _buffer[1];

            float stateDeltaTime = dataB.Time - dataA.Time;
            if (stateDeltaTime <= 0) return;

            float lerpT = _interpolationTimer / stateDeltaTime;
            _position = Vector2.Lerp(dataA.Position, dataB.Position, lerpT);

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

            // update hardpoints
            for (int i = 0; i < Hardpoints.Count; i++)
            {
                Hardpoints[i].Hardpoint.SetRotation(
                    Mathf.LerpAngle(dataA.Hardpoints[i].Rotation, dataB.Hardpoints[i].Rotation, lerpT));
            }
            
            _interpolationTimer += delta; // or delta if that's the frame delta

            if (_interpolationTimer >= stateDeltaTime)
            {
                _buffer.RemoveFromStart(1);
                _interpolationTimer -= stateDeltaTime;
                _bufferTime -= stateDeltaTime;
            }
            
            if (_buffer.Count < 3 && _bufferTime < TargetBufferTime * 0.5f)
            {
                _interpolationTimer *= 0.8f;
            }
        }

        public void OnPlayerState(PlayerState state)
        {
            if (!GameManager.Instance.Settings.EntityInterpolation)
            {
                _position = state.Position;
                _rotation = state.Rotation;
                _velocity = state.Velocity;
                _angularVelocity = state.AngularVelocity;
                // update hardpoints
                for (int i = 0; i < Hardpoints.Count; i++)
                {
                    Hardpoints[i].Hardpoint.SetRotation(state.Hardpoints[i].Rotation);
                }
                return;
            }
            
            _health = state.Health;
            
            float timeDiff = state.Time - (_buffer.Count > 0 ? _buffer.Last.Time : 0f);
            if (timeDiff < 0)
            {
                return;
            }
            
            _bufferTime += timeDiff;

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

        public override void Update(float delta)
        {
            UpdatePosition(delta);
            UpdateView();
            UpdateThrusterVisualization(delta);
        }

        private void UpdateThrusterVisualization(float delta)
        {
            // We need to guess forces from velocity changes:
            if (_view == null || _view.Rb == null || _view.Rb.mass <= 0) return;
            
            float mass = _view.Rb.mass;
            float inertia = _view.Rb.inertia; // Moment of inertia for 2D rigidbody

            if (_hasLastFrameState)
            {
                // Compute linear acceleration
                Vector2 dv = _velocity - _lastVelocity;
                Vector2 acceleration = dv / delta;

                // Compute angular acceleration
                float dOmega = _angularVelocity - _lastAngularVelocity;
                float angularAcceleration = dOmega / delta;

                // Net force and torque
                Vector2 netForce = mass * acceleration;
                float netTorque = inertia * angularAcceleration;
                
                // If there's known external damping or drag, subtract it here
                // Example: netForce -= drag * velocity, etc.
                netForce -= _view.Rb.drag * _velocity;
                netTorque -= _view.Rb.angularDrag * _angularVelocity;

                // Now we have a "desired" localForce and netTorque similar to what the local player ApplyThrust would produce.
                // We'll reuse the logic that a local player would have: call ApplyThrust on the OmnidirectionalThrusterController
                // with these derived forces.
                _view.ApplyThrust(netForce, netTorque);
            }

            _lastVelocity = _velocity;
            _lastAngularVelocity = _angularVelocity;
            _hasLastFrameState = true;
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
