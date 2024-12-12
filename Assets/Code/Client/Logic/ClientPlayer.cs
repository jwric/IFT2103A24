using System.Collections;
using System.Collections.Generic;
using Code.Client.Managers;
using Code.Shared;
using LiteNetLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Client.Logic
{
    public class ClientPlayer : BasePlayer
    {
        private PlayerView _view;
        private PlayerInputPacket _nextCommand;
        private readonly ClientLogic _clientLogic;
        private readonly ClientPlayerManager _playerManager;
        private readonly LiteRingBuffer<PlayerInputPacket> _predictionPlayerStates;
        private readonly LiteRingBuffer<PlayerState> _clientPlayerStates;
        private ServerState _lastServerState;
        private const int MaxStoredCommands = 100;
        private bool _firstStateReceived;
        private int _updateCount;
        private float _tickTime;
        private int _sentPackets;
        private float _lastRecvTime;
        private int _packetsToSend;
        private Vector2 _aimPosition;
        
        private Vector2 _positionError = Vector2.zero;
        private float _rotationError = 0f;
        private bool correctionSmoothing = false;
        
        private Rigidbody2D _rewindRb;  // Rigidbody for the rewind scene
        
        private readonly LiteRingBuffer<PlayerInputPacket> _packets;

        public Vector2 LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        public int StoredCommands => _predictionPlayerStates.Count;

        public Scene RewindScene { get; set; }
        public PhysicsScene2D RewindPhysicsScene { get; set; }
        
        Dictionary<int, Rigidbody2D> remoteRbs = new Dictionary<int, Rigidbody2D>();


        public int StatesReceivedThisClientTick = 0;
        public int ClientTicksWithoutServerState = 0;
        
        public ClientPlayer(ClientLogic clientLogic, ClientPlayerManager manager, PlayerInitialInfo initialInfo) : base(manager, initialInfo.UserName, initialInfo.Id)
        {
            // Add hardpoints
            for (var index = 0; index < initialInfo.Hardpoints.Length; index++)
            {
                var slot = initialInfo.Hardpoints[index];
                
                Hardpoints.Add(new HardpointSlot(slot.Id, HardpointFactory.CreateHardpoint(slot.Type), new Vector2Int(slot.X, slot.Y)));
            }
            
            _playerManager = manager;
            _predictionPlayerStates = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _packets = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _clientPlayerStates = new LiteRingBuffer<PlayerState>(MaxStoredCommands);
            _clientLogic = clientLogic;
            
            // Create a copy eof the Rigidbody for use in the rewind scene
            var hi = Object.Instantiate(_clientLogic.RewindGO);
            _rewindRb = hi.GetComponent<Rigidbody2D>();
            
            // Move the rewind Rigidbody to the rewind scene
            GameManager.Instance.StartCoroutine(MoveObjectToSceneAfterDelay(hi));
        }
        
        private IEnumerator MoveObjectToSceneAfterDelay(GameObject obj)
        {
            yield return null; // Wait for one frame to ensure everything is fully initialized
            SceneManager.MoveGameObjectToScene(obj, RewindScene);
        }
        
        public void SetPlayerView(PlayerView view)
        {
            _view = view;
        }
        
        public override void NotifyHit(HitInfo hit)
        {
            _view.OnHit(hit);
        }
        
        public override void OnHardpointAction(HardpointAction action)
        {
            _view.GetHardpointView(action.SlotId, out var hardpointView);
            hardpointView?.OnHardpointAction(action.ActionCode);
        }
        
        public Vector2 GetViewHardpointFirePosition(byte id)
        {
            _view.GetHardpointView(id, out var hardpointView);
            return hardpointView?.GetFirePosition() ?? Position;
        }
        
        private void ApplyInputToRigidbody(Rigidbody2D rb, PlayerInputPacket command)
        {
            // Vector2 velocity = Vector2.zero;

            // if ((command.Keys & MovementKeys.Up) != 0)
            //     velocity.y = -1f;
            // if ((command.Keys & MovementKeys.Down) != 0)
            //     velocity.y = 1f;
            //
            // if ((command.Keys & MovementKeys.Left) != 0)
            //     velocity.x = -1f;
            // if ((command.Keys & MovementKeys.Right) != 0)
            //     velocity.x = 1f;

            // _view.Move(velocity.normalized * (_speed * delta));
            rb.AddForce(command.Thrust * _speed, ForceMode2D.Force);
            rb.AddTorque(command.AngularThrust * _angularSpeed, ForceMode2D.Force);
        }

        private void SyncWithServerState(Rigidbody2D rb, PlayerState ourState)
        {
            // Sync Rigidbody with the server state
            rb.MovePosition(ourState.Position);
            rb.velocity = ourState.Velocity;
            rb.MoveRotation(ourState.Rotation * Mathf.Rad2Deg);
            rb.angularVelocity = ourState.AngularVelocity;
        }
        
        private void RewindAndReapplyPredictions(PlayerState ourState, bool onlyRotation = false)
        {
            Vector2 prevPosition = _view.Rb.position + _positionError;
            float prevRotation = _view.Rb.rotation + _rotationError;
            
            
            // SyncWithServerState(_view.Rb, ourState);
            SyncWithServerState(_rewindRb, ourState);

            // Fixes jittering, im not sure why
            // RewindPhysicsScene.Simulate(Time.fixedDeltaTime);
            
            // place remote objects in the rewind scene for collision prediction
            {
                var players = _playerManager.GetEnumerator();
                while (players.MoveNext())
                {
                    var player = players.Current;
                    if (player.Id == Id)
                        continue;
                    
                    if (remoteRbs.TryGetValue(player.Id, out var rb))
                    {
                        rb.position = player.Position;
                        rb.rotation = player.Rotation * Mathf.Rad2Deg;
                        rb.velocity = player.Velocity;
                        rb.angularVelocity = player.AngularVelocity;
                    }
                    else
                    {
                        // create object in rewind scene
                        var rewindObj = Object.Instantiate(_clientLogic.RewindGO, _rewindRb.transform);
                        var rewindRb = rewindObj.GetComponent<Rigidbody2D>();
                        rewindRb.position = player.Position;
                        rewindRb.rotation = player.Rotation * Mathf.Rad2Deg;
                        rewindRb.velocity = player.Velocity;
                        rewindRb.angularVelocity = player.AngularVelocity;
                        remoteRbs.Add(player.Id, rewindRb);

                        // move object to rewind scene
                        rewindRb.transform.SetParent(null);
                    }
                }
            }
            RewindPhysicsScene.Simulate(Time.fixedDeltaTime);

            // Apply predictions in the rewind scene
            for (var index = 0; index < _predictionPlayerStates.Count; index++)
            {
                var input = _predictionPlayerStates[index];
                ref var state = ref _clientPlayerStates[index];
                state.Position = _rewindRb.position;
                state.Rotation = _rewindRb.rotation * Mathf.Deg2Rad;
                state.Velocity = _rewindRb.velocity;
                state.AngularVelocity = _rewindRb.angularVelocity;
                ApplyInputToRigidbody(_rewindRb, input);
                RewindPhysicsScene.Simulate(Time.fixedDeltaTime);
            }
            
            List<int> toRemove = new List<int>();
            // Destroy the remote objects that are no longer in the game
            foreach (var rb in remoteRbs)
            {
                if (_playerManager.GetById((byte)rb.Key) == null)
                {
                    toRemove.Add(rb.Key);
                }
            }
            
            for (int i = 0; i < toRemove.Count; i++)
            {
                var rb = remoteRbs[toRemove[i]];
                remoteRbs.Remove(toRemove[i]);
                Object.Destroy(rb.gameObject);
            }
            
            
            // Update the view with the result of the rewind
            if (GameManager.Instance.Settings.ServerReconciliation)
            {
                if (onlyRotation)
                {
                    _view.Rb.MoveRotation(_rewindRb.rotation);
                    _view.Rb.angularVelocity = _rewindRb.angularVelocity;
                }
                else
                {
                    _view.Rb.MovePosition(_rewindRb.position);
                    _view.Rb.MoveRotation(_rewindRb.rotation);
                    _view.Rb.velocity = _rewindRb.velocity;
                    _view.Rb.angularVelocity = _rewindRb.angularVelocity;
                }
            }
            
            if ((prevPosition - _view.Rb.position).sqrMagnitude >= 4.0f)
            {
                _positionError = Vector2.zero;
                _rotationError = 0f;
            }
            else
            {
                _positionError = prevPosition - _view.Rb.position;
                _rotationError = prevRotation - _view.Rb.rotation;
            }
            
        }
        
        public void ReceiveServerState(ServerState serverState, PlayerState ourState)
        {
            if (!_firstStateReceived)
            {
                if (serverState.LastProcessedCommand == 0)
                    return;
                _firstStateReceived = true;
            }
            if (serverState.Tick == _lastServerState.Tick || 
                serverState.LastProcessedCommand == _lastServerState.LastProcessedCommand)
                return;
            
            StatesReceivedThisClientTick++;
            var tickGap = ClientTicksWithoutServerState;
            ClientTicksWithoutServerState = 0;
            Debug.Log($"[C] Received server state: tickGap: {tickGap}");
            // tick gap should be multiple of 3

            _health = ourState.Health;
            
            // if (Input.GetKey(KeyCode.P))
            // {
            //     return;
            // }
            
            
            // check if received old state
            if (NetworkGeneral.SeqDiff(serverState.LastProcessedCommand, _lastServerState.LastProcessedCommand) < 0)
            {
                Debug.LogWarning($"[C] Received old state: {serverState.LastProcessedCommand} < {_lastServerState.LastProcessedCommand}");
                return;
            }
            
            float timeToRecv = Time.time - _lastRecvTime;
            _lastRecvTime = Time.time;

            // Debug.Log($"Recv server state: tickTime: {_tickTime}, sent: {_sentPackets}, timeToRecv: {timeToRecv}");
            _tickTime = 0f;
            _sentPackets = 0;
            _lastServerState = serverState;
            
            // _position = ourState.Position;
            // _velocity = ourState.Velocity;
            // _rotation = ourState.Rotation;
            // _angularVelocity = ourState.AngularVelocity;

            if (_predictionPlayerStates.Count == 0)
                return;

            ushort lastProcessedCommand = _lastServerState.LastProcessedCommand;
            int diff = NetworkGeneral.SeqDiff(lastProcessedCommand, _predictionPlayerStates.First.Id);

            
            //apply prediction
            if (diff >= 0 && diff < _predictionPlayerStates.Count)
            {
                _predictionPlayerStates.RemoveFromStart(diff + 1);
                _clientPlayerStates.RemoveFromStart(diff + 1);
                
                if (!GameManager.Instance.Settings.ClientSidePrediction)
                {
                    SyncWithServerState(_view.Rb, ourState);
                    return;
                }
                
                Vector2 positionError = ourState.Position - _clientPlayerStates.First.Position;
                float rotationError = ourState.Rotation - _clientPlayerStates.First.Rotation;
                if (positionError.sqrMagnitude > 0.0000001f || Mathf.Abs(rotationError) > 0.00001f*Mathf.Deg2Rad)
                {
                    Debug.Log($"[C] Position error: {positionError.sqrMagnitude}, Rotation error: {rotationError}");
                    RewindAndReapplyPredictions(ourState, false);
                }
            }
            else if (diff >= _predictionPlayerStates.Count)
            {
                Debug.Log(
                    $"[C] Player input lag st: {_predictionPlayerStates.First.Id} ls:{lastProcessedCommand} df:{diff}");
                //lag
                _predictionPlayerStates.FastClear();
                _clientPlayerStates.FastClear();
                _nextCommand.Id = lastProcessedCommand;
            }
            else
            {
                Debug.Log(
                    $"[ERR] SP: {_lastServerState.LastProcessedCommand}, OUR: {_predictionPlayerStates.First.Id}, DF:{diff}, STORED: {StoredCommands}");
            }
        }

        public override void Spawn(Vector2 position)
        {
            _predictionPlayerStates.FastClear();
            _clientPlayerStates.FastClear();
            _view.Spawn(position,0f);
            base.Spawn(position);
        }

        public void SetInput(Vector2 velocity, float rotation, bool fire)
        {
            _nextCommand.Keys = 0;
            if(fire)
                _nextCommand.Keys |= MovementKeys.Fire;

            if (velocity.x < -0.5f)
            {
                _nextCommand.Keys |= MovementKeys.Left;
            }

            if (velocity.x > 0.5f)
            {
                _nextCommand.Keys |= MovementKeys.Right;
            }

            if (velocity.y < -0.5f)
            {
                _nextCommand.Keys |= MovementKeys.Up;
            }

            if (velocity.y > 0.5f)
            {
                _nextCommand.Keys |= MovementKeys.Down;
            }

            _nextCommand.Thrust = velocity;
            _nextCommand.AngularThrust = rotation;
            
            _nextCommand.NumHardpoints = (byte)Hardpoints.Count;
            _nextCommand.Hardpoints = new HardpointInputState[Hardpoints.Count];

            // update hardpoints
            for (int i = 0; i < Hardpoints.Count; i++)
            {
                var slot = Hardpoints[i];
                var hardpointView = _view.GetHardpointView(slot.Id);
                _nextCommand.Hardpoints[i] = new HardpointInputState
                {
                    Id = slot.Id,
                    Rotation = hardpointView.GetRotation(),
                    Fire = fire
                };
            }
            
            //Debug.Log($"[C] SetInput: {_nextCommand.Keys}");
            
            // UpdateLocal(dt);
        }

        public override void Update(float delta)
        {
            var ticksThisFrame = StatesReceivedThisClientTick;
            StatesReceivedThisClientTick = 0;
            ClientTicksWithoutServerState++;

            // Debug.Log($"[C] Ticks this frame: {ticksThisFrame}");
            
            if (Input.GetKey(KeyCode.R))
            {
                correctionSmoothing = false;
            }
            else
            {
                correctionSmoothing = true;
            }
            
            LastPosition = _view.Rb.position;
            LastRotation = _view.Rb.rotation;
            
            _position = _view.Rb.position;
            _velocity = _view.Rb.velocity;
            _rotation = _view.Rb.rotation * Mathf.Deg2Rad;
            _angularVelocity = _view.Rb.angularVelocity;
            
            
            // if (correctionSmoothing)
            // {
            //     _positionError = Vector2.Lerp(_positionError, Vector2.zero, 0.1f);
            //     _rotationError = Mathf.Lerp(_rotationError, 0f, 0.1f);
            // }
            // else 
            // {
            //     _positionError = Vector2.zero;
            //     _rotationError = 0f;
            // }
            //
            // _view.transform.position = _view.Rb.position + _positionError;
            // _view.transform.rotation = Quaternion.Euler(0, 0, _view.Rb.rotation + _rotationError);

            // _view.UpdateView(_view.Rb.position + _positionError, _view.Rb.rotation + _rotationError);
            
            _nextCommand.Id = (ushort)((_nextCommand.Id + 1) % NetworkGeneral.MaxGameSequence);
            _nextCommand.ServerTick = _lastServerState.Tick;
            _nextCommand.Delta = delta;
            _nextCommand.Time = Time.fixedTime;

            if (GameManager.Instance.Settings.ClientSidePrediction)
            {
                ApplyInput(_nextCommand, delta);
            }

            if (_predictionPlayerStates.IsFull)
            {
                Debug.LogWarning("Input is too fast for server. Prediction buffer is full, clearing.");
                _nextCommand.Id = (ushort)(_lastServerState.LastProcessedCommand+1);
                _predictionPlayerStates.FastClear();
                _clientPlayerStates.FastClear();
            }
            _predictionPlayerStates.Add(_nextCommand);
            

            _tickTime += delta;
            
            _sentPackets++;
            _packetsToSend++;            
            
            _updateCount++;
            if (_updateCount == 3)
            {
                _updateCount = 0;
                foreach (var t in _predictionPlayerStates)
                    _clientLogic.SendPacketSerializable(PacketType.Movement, t, DeliveryMethod.Unreliable);
            }
            
            base.Update(delta);
        }

        public void StateSnapshot()
        {
            PlayerState currentState = new PlayerState
            {
                Position = _view.Rb.position,
                Rotation = _view.Rb.rotation * Mathf.Deg2Rad,
                Velocity = _view.Rb.velocity,
                AngularVelocity = _view.Rb.angularVelocity,
                Tick = _lastServerState.Tick,
                Time = Time.fixedTime,
            };
            _clientPlayerStates.Add(currentState);
        }

        public override void FrameUpdate(float delta)
        {
            var vert = Input.GetAxis("Vertical");
            var horz = Input.GetAxis("Horizontal");
            var fire = Input.GetAxis("Fire1");
            var aim = Input.GetAxis("Fire2");
            var brake = Input.GetAxis("Jump");
            
            // apply aim to hardpoints, aim doesnt matter in the input  because it is not server authoritative
            
            Vector2 velocity = new Vector2(horz, vert);
            
            float torque = 0f;
            
            if (brake > 0f)
            {
                // counteract the velocity to brake using derivative control to reach angular velocity 0
                float angularVelocity = _view.Rb.angularVelocity;
                float kD = 0.1f;
                torque = -kD * angularVelocity;
            }
            else if (aim > 0f)
            {
                Vector2 mousePos = _clientLogic._camera.Camera.ScreenToWorldPoint(Input.mousePosition);
                Vector2 dir = mousePos - _view.Rb.position;
                float targetRotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // Calculate the angle difference
                float rotationDiff = GetAngleDifference(_view.Rb.rotation, targetRotation);

                // Proportional and Derivative Control
                float angularVelocity = _view.Rb.angularVelocity;
                float kP = 0.5f;
                float kD = 0.1f;

                torque = (kP * rotationDiff) - (kD * angularVelocity);
            }

            // Clamp the torque input to [-1, 1]
            torque = Mathf.Clamp(torque, -1f, 1f);

            _aimPosition = _clientLogic._camera.Camera.ScreenToWorldPoint(Input.mousePosition);
            
            SetInput(velocity, torque, fire > 0f);
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            ApplyInputToRigidbody(_view.Rb, command);
            // apply input to the view
            _view.ApplyThrust(command.Thrust * _speed, command.AngularThrust * _angularSpeed);
            // _rotation = command.Rotation;
            
            // deprecated
            // if ((command.Keys & MovementKeys.Fire) != 0)
            // {
            //     if (_shootTimer.IsTimeElapsed)
            //     {
            //         _shootTimer.Reset();
            //         // Shoot();
            //         // _view.OnShoot(_view.transform.right);
            //     }
            // }
            
            // apply aim to hardpoints view
            for (int i = 0; i < Hardpoints.Count; i++)
            {
                var slot = Hardpoints[i];
                _view.GetHardpointView(slot.Id, out var hardpointView);
                hardpointView?.AimAt(_aimPosition);
            }
            
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
        
        public void Die()
        {
            _view.Die();
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
        
    }
}