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
        private ClientPlayerView _view;
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
        
        private Vector2 _positionError = Vector2.zero;
        private float _rotationError = 0f;
        
        private Rigidbody2D _rewindRb;  // Rigidbody for the rewind scene
        
        private readonly LiteRingBuffer<PlayerInputPacket> _packets;

        public Vector2 LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        public int StoredCommands => _predictionPlayerStates.Count;

        public Scene RewindScene { get; set; }
        public PhysicsScene2D RewindPhysicsScene { get; set; }
        
        Dictionary<int, Rigidbody2D> remoteRbs = new Dictionary<int, Rigidbody2D>();
        
        
        public ClientPlayer(ClientLogic clientLogic, ClientPlayerManager manager, string name, byte id) : base(manager, name, id)
        {
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
        
        public void SetPlayerView(ClientPlayerView view)
        {
            _view = view;
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
        
        private void RewindAndReapplyPredictions(PlayerState ourState)
        {
            // Vector2 prevPosition = _view.Rb.position + _positionError;
            // float prevRotation = _view.Rb.rotation + _rotationError;
            
            
            // SyncWithServerState(_view.Rb, ourState);
            SyncWithServerState(_rewindRb, ourState);

            // Fixes jittering, im not sure why
            RewindPhysicsScene.Simulate(Time.fixedDeltaTime);
            
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
                _view.Rb.MovePosition(_rewindRb.position);
                _view.Rb.MoveRotation(_rewindRb.rotation);
                _view.Rb.velocity = _rewindRb.velocity;
                _view.Rb.angularVelocity = _rewindRb.angularVelocity;
            }
            
            // if ((prevPosition - _rewindRb.position).sqrMagnitude >= 4.0f)
            // {
            //     _positionError = Vector2.zero;
            //     _rotationError = 0f;
            // }
            // else
            // {
            //     _positionError = _rewindRb.position - prevPosition;
            //     _rotationError = _rewindRb.rotation - prevRotation;
            // }
            
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
                    // remove predictions
                    return;
                }
                
                Vector2 positionError = ourState.Position - _clientPlayerStates.First.Position;
                float rotationError = ourState.Rotation - _clientPlayerStates.First.Rotation;
                if (positionError.sqrMagnitude > 0.1f || Mathf.Abs(rotationError) > 5f*Mathf.Deg2Rad)
                {
                    RewindAndReapplyPredictions(ourState);

                    // Debug.LogWarning($"[C] Position error: {positionError}");
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
            
            //Debug.Log($"[C] SetInput: {_nextCommand.Keys}");
            
            // UpdateLocal(dt);
        }

        public override void Update(float delta)
        {
            LastPosition = _view.Rb.position;
            LastRotation = _view.Rb.rotation;
            
            _position = _view.Rb.position;
            _velocity = _view.Rb.velocity;
            _rotation = _view.Rb.rotation * Mathf.Deg2Rad;
            _angularVelocity = _view.Rb.angularVelocity;
            
            // _positionError *= 0.9f;
            // _rotationError = Quaternion.Slerp(Quaternion.Euler(0f, 0f, _rotationError), Quaternion.identity, 0.1f).eulerAngles.z;
            
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

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            ApplyInputToRigidbody(_view.Rb, command);
            // _rotation = command.Rotation;
            
            if ((command.Keys & MovementKeys.Fire) != 0)
            {
                if (_shootTimer.IsTimeElapsed)
                {
                    _shootTimer.Reset();
                    Shoot();
                }
            }
        }
        
        public void Die()
        {
            _view.Die();
        }
        
    }
}