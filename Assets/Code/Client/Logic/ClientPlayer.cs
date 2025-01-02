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
        private PlayerState _lastState;
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
        
        private Queue<(ServerState, PlayerState)> _serverStateBuffer = new();
        private const int MaxServerStateBufferSize = 5;

        public ClientPlayer(ClientLogic clientLogic, ClientPlayerManager manager, PlayerInitialInfo initialInfo) : base(manager, initialInfo.UserName, initialInfo.Id)
        {
            // // Add hardpoints
            // for (var index = 0; index < initialInfo.Hardpoints.Length; index++)
            // {
            //     var slot = initialInfo.Hardpoints[index];
            //     
            //     Hardpoints.Add(new HardpointSlot(slot.Id, HardpointFactory.CreateHardpoint(slot.Type), new Vector2Int(slot.X, slot.Y)));
            // }
            
            // create ship
            _ship = ShipFactory.CreateShip(initialInfo.ShipType);
            
            _playerManager = manager;
            _predictionPlayerStates = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _packets = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _clientPlayerStates = new LiteRingBuffer<PlayerState>(MaxStoredCommands);
            _clientLogic = clientLogic;
            
            // Create a copy eof the Rigidbody for use in the rewind scene
            var hi = Object.Instantiate(_clientLogic.RewindGO);
            _rewindRb = hi.GetComponent<Rigidbody2D>();
            
            // Grab colors from settings and apply them to the player model
            var primaryColor = GameManager.Instance.Settings.PrimaryColor;
            var secondaryColor = GameManager.Instance.Settings.SecondaryColor;
            
            // Set the colors of the player model
            _primaryColor = primaryColor;
            _secondaryColor = secondaryColor;
            
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
            
            // todo do this in a better way
            _view.SetColors(_primaryColor, _secondaryColor);
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
        
        public void ShootHardpoint(byte hardpointId, Vector2 to, BasePlayer hit, byte damage)
        {
            _view.GetHardpointView(hardpointId, out var hardpointView);
            hardpointView?.SpawnFire(to);
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
            var thrust = _ship.CalculateDirThrustForce(command.Thrust);
            var angularThrust = _ship.CalculateAngularThrustTorque(command.AngularThrust);
            var rotatedThrust = Quaternion.Euler(0, 0, rb.rotation) * thrust;
            rb.AddForce(rotatedThrust, ForceMode2D.Force);
            rb.AddTorque(angularThrust, ForceMode2D.Force);
        }

        private void SyncWithServerState(Rigidbody2D rb, PlayerState ourState)
        {
            rb.position = ourState.Position;
            rb.rotation = ourState.Rotation * Mathf.Rad2Deg;
            rb.velocity = ourState.Velocity;
            rb.angularVelocity = ourState.AngularVelocity;
        }
        
        private Vector2 _previousPositionError = Vector2.zero;
        private float _previousRotationError = 0f;
        private const float ErrorMagnitudeThreshold = 0.5f;
        private const float ErrorDeltaThreshold = 0.1f; // Change in error between frames

        private bool IsFalseReconciliation(Vector2 currentPositionError, float currentRotationError)
        {
            float positionDelta = (currentPositionError - _previousPositionError).sqrMagnitude;
            float rotationDelta = Mathf.Abs(currentRotationError - _previousRotationError);

            bool isErratic = positionDelta > ErrorDeltaThreshold || rotationDelta > ErrorDeltaThreshold;
            bool isLargeDeviation = currentPositionError.sqrMagnitude > ErrorMagnitudeThreshold 
                                    || Mathf.Abs(currentRotationError) > ErrorMagnitudeThreshold;

            // Store previous errors for future comparisons
            _previousPositionError = currentPositionError;
            _previousRotationError = currentRotationError;

            // A false reconciliation is detected if the error change is erratic but not persistently large
            return isErratic && !isLargeDeviation;
        }
        
        private int _jitterFrameCount = 0;
        private int _maxJitterFrames = 5; // Tunable parameter

        private readonly Queue<Vector2> _positionErrorHistory = new Queue<Vector2>();
        private readonly Queue<float> _rotationErrorHistory = new Queue<float>();
        private const int ErrorHistorySize = 10; // Tunable parameter

        
        private Vector2 CalculateMean(Queue<Vector2> errors)
        {
            if (errors.Count == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            foreach (var error in errors) sum += error;
            return sum / errors.Count;
        }

        private float CalculateMean(Queue<float> errors)
        {
            if (errors.Count == 0) return 0f;
            float sum = 0f;
            foreach (var error in errors) sum += error;
            return sum / errors.Count;
        }

        private float CalculateVariance(Queue<Vector2> errors, Vector2 mean)
        {
            if (errors.Count == 0) return 0f;
            float variance = 0f;
            foreach (var error in errors) variance += (error - mean).sqrMagnitude;
            return variance / errors.Count;
        }

        private float CalculateVariance(Queue<float> errors, float mean)
        {
            if (errors.Count == 0) return 0f;
            float variance = 0f;
            foreach (var error in errors) variance += Mathf.Pow(error - mean, 2);
            return variance / errors.Count;
        }

        
        private bool IsJitter(Vector2 currentPositionError, float currentRotationError)
        {
            Vector2 meanPositionError = CalculateMean(_positionErrorHistory);
            float meanRotationError = CalculateMean(_rotationErrorHistory);

            float positionVariance = CalculateVariance(_positionErrorHistory, meanPositionError);
            float rotationVariance = CalculateVariance(_rotationErrorHistory, meanRotationError);

            float positionStdDev = Mathf.Sqrt(positionVariance);
            float rotationStdDev = Mathf.Sqrt(rotationVariance);

            float positionZScore = ((currentPositionError - meanPositionError).magnitude) / positionStdDev;
            float rotationZScore = Mathf.Abs(currentRotationError - meanRotationError) / rotationStdDev;

            bool isPositionOutlier = positionZScore > 2f; // Tunable z-score threshold
            bool isRotationOutlier = rotationZScore > 2f;

            // Update history
            if (_positionErrorHistory.Count >= ErrorHistorySize) _positionErrorHistory.Dequeue();
            _positionErrorHistory.Enqueue(currentPositionError);

            if (_rotationErrorHistory.Count >= ErrorHistorySize) _rotationErrorHistory.Dequeue();
            _rotationErrorHistory.Enqueue(currentRotationError);

            return isPositionOutlier && isRotationOutlier;
        }

        
        private bool IsAdaptiveJitter(Vector2 currentPositionError, float currentRotationError)
        {
            if (IsJitter(currentPositionError, currentRotationError))
            {
                _jitterFrameCount++;
                return _jitterFrameCount <= _maxJitterFrames;
            }
            _jitterFrameCount = 0; // Reset on no jitter
            return false;
        }


        private PlayerState RewindAndReapplyPredictions(PlayerState ourState, out Vector2 positionError, out float rotationError)
        {
            Vector2 prevPosition = _view.Rb.position + _positionError;
            float prevRotation = _view.Rb.rotation + _rotationError;
            
            
            SyncWithServerState(_rewindRb, ourState);

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
            
            // Reconciliation check
            positionError = prevPosition - _rewindRb.position;
            rotationError = prevRotation - _rewindRb.rotation;

            _positionError = positionError;
            _rotationError = rotationError;

            return new PlayerState
            {
                Position = _rewindRb.position,
                Rotation = _rewindRb.rotation * Mathf.Deg2Rad,
                Velocity = _rewindRb.velocity,
                AngularVelocity = _rewindRb.angularVelocity,
                Tick = _lastServerState.Tick
            };
        }
        
        public void ReceiveServerState(ServerState serverState, PlayerState ourState)
        {
            if (!_firstStateReceived)
            {
                if (serverState.LastProcessedCommand == 0)
                    return;
                _nextCommand.Id = serverState.Tick;
                _firstStateReceived = true;
            }
            if (serverState.Tick == _lastServerState.Tick || 
                serverState.LastProcessedCommand == _lastServerState.LastProcessedCommand)
                return;

            StatesReceivedThisClientTick++;
            var tickGap = ClientTicksWithoutServerState;
            ClientTicksWithoutServerState = 0;

            // Add the new state to the buffer
            _serverStateBuffer.Enqueue((serverState, ourState));
            if (_serverStateBuffer.Count > MaxServerStateBufferSize)
            {
                _serverStateBuffer.Dequeue();
            }
            
            
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
            _lastState = ourState;

            ApplyLastServerState();
        }

        public void ApplyLastServerState()
        {
            if (_predictionPlayerStates.Count == 0)
            {
                Debug.Log($"[C] No predictions to apply");
                return;
            }

            if (!_firstStateReceived)
            {
                Debug.Log($"[C] No server state received yet");
                return;
            }

            PlayerState ourState = _lastState;
            ServerState serverState = _lastServerState;

            ushort lastProcessedCommand = serverState.LastProcessedCommand;
            ushort lastServerTick = serverState.Tick;
            int diff = NetworkGeneral.SeqDiff(lastProcessedCommand, _predictionPlayerStates.First.Id);
            // find the state that has the same tick as the last processed command
            // Debug.Log($"[C] Diff: {diff}");

            //apply prediction
            if (diff >= 0 && diff < _predictionPlayerStates.Count)
            {
                // remove all states that are older than the last processed command
                _predictionPlayerStates.RemoveFromStart(diff + 1);
                _clientPlayerStates.RemoveFromStart(diff + 1);
      
                if (!GameManager.Instance.Settings.ClientSidePrediction)
                {
                    SyncWithServerState(_view.Rb, ourState);
                    return;
                }
                
                Vector2 positionError = ourState.Position - _clientPlayerStates.First.Position;
                float rotationError = ourState.Rotation - _clientPlayerStates.First.Rotation;
                if (positionError.sqrMagnitude > 0.0001f || Mathf.Abs(rotationError) > 0.1f * Mathf.Deg2Rad)
                {
                    // maybe we could buffer the corrected states and check for jittering to avoid false corrections
                    PlayerState newState = RewindAndReapplyPredictions(ourState, out positionError, out rotationError);
                    // if (IsFalseReconciliation(positionError, rotationError))
                    // {
                    //     Debug.LogWarning($"[C] False reconciliation detected");
                    // }
                    // else
                    // {
                    //     if (IsAdaptiveJitter(positionError, rotationError) || (Input.GetKey(KeyCode.R) && diff > 3))
                    //     {
                    //         Debug.LogWarning($"[C] Jitter detected");
                    //     }
                    //     else
                    //     {
                    // Debug.Log($"[C] Applying correction: {positionError} {rotationError}");
                            SyncWithServerState(_view.Rb, newState);
                        // }
                    // }
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
            
            // state snapshot
            PlayerState currentState = StateSnapshot();

            
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
            _clientPlayerStates.Add(currentState);


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

        public PlayerState StateSnapshot()
        {
            PlayerState currentState = new PlayerState
            {
                Position = _view.Rb.position,
                Rotation = _view.Rb.rotation * Mathf.Deg2Rad,
                Velocity = _view.Rb.velocity,
                AngularVelocity = _view.Rb.angularVelocity,
                Tick = _lastServerState.Tick
            };
            return currentState;
        }

        public override void FrameUpdate(float delta)
        {
            var vert = Input.GetAxis("Vertical");
            var horz = Input.GetAxis("Horizontal");
            var fire = Input.GetAxis("Fire1");
            var aim = Input.GetAxis("Fire2");
            var brake = Input.GetAxis("Jump");
            
            // apply aim to hardpoints, aim doesnt matter in the input  because it is not server authoritative
            
            // local thrust
            Vector2 thrust = new Vector2(horz, vert);
            // from world thrust to local thrust
            thrust = _ship.CalculateInverseThrustPercents(thrust, _view.Rb.rotation, new Vector2(Mathf.Abs(1), Mathf.Abs(1)));
            
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
            
            SetInput(thrust, torque, fire > 0f);
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            ApplyInputToRigidbody(_view.Rb, command);
            // apply input to the view
            _view.ApplyThrust(command.Thrust, command.AngularThrust);
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
                hardpointView?.AimAt(_aimPosition, delta);
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