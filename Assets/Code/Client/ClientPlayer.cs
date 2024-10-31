using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Client
{ 
    public class ClientPlayer : BasePlayer
    {
        private ClientPlayerView _view;
        private PlayerInputPacket _nextCommand;
        private readonly ClientLogic _clientLogic;
        private readonly ClientPlayerManager _playerManager;
        private readonly LiteRingBuffer<PlayerInputPacket> _predictionPlayerStates;
        private ServerState _lastServerState;
        private const int MaxStoredCommands = 60;
        private bool _firstStateReceived;
        private int _updateCount;
        private float _tickTime;
        private int _sentPackets;
        private float _lastRecvTime;
        private int _packetsToSend;
        
        private readonly LiteRingBuffer<PlayerInputPacket> _packets;

        public Vector2 LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        public int StoredCommands => _predictionPlayerStates.Count;

        public ClientPlayer(ClientLogic clientLogic, ClientPlayerManager manager, string name, byte id) : base(manager, name, id)
        {
            _playerManager = manager;
            _predictionPlayerStates = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _packets = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _clientLogic = clientLogic;
        }
        
        public void SetPlayerView(ClientPlayerView view)
        {
            _view = view;
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
            
            float timeToRecv = Time.time - _lastRecvTime;
            _lastRecvTime = Time.time;

            // Debug.Log($"Recv server state: tickTime: {_tickTime}, sent: {_sentPackets}, timeToRecv: {timeToRecv}");
            _tickTime = 0f;
            _sentPackets = 0;
            _lastServerState = serverState;

            var lastPos = _view.Rb.position;
            var lastVel = _view.Rb.velocity;
            
            var newPos = ourState.Position;
            var newVel = ourState.Velocity;
            
            // check if we rolled back
            // calculate distance dotting with velocity
            float dist = Vector2.Dot(newPos - lastPos, newVel.normalized);
            if (dist < 0)
            {
                Debug.Log($"[C] Player rolled back: {dist}");
                // _predictionPlayerStates.FastClear();
                // _nextCommand.Id = serverState.LastProcessedCommand;
            }
            
            
            //sync
            _position = ourState.Position;
            _rotation = ourState.Rotation;
            
            // log prediction pos vs server pos
            Debug.Log($"[C] Player pos diff: {Vector2.Distance(ourState.Position, _view.Rb.position)}");
            Rigidbody2D rb = _view.Rb;
            rb.MovePosition(ourState.Position);
            // rb.MoveRotation(ourState.Rotation * Mathf.Rad2Deg);
            // _rotation = ourState.Rotation;
            rb.velocity = ourState.Velocity;
            
            if (_predictionPlayerStates.Count == 0)
                return;

            ushort lastProcessedCommand = serverState.LastProcessedCommand;
            int diff = NetworkGeneral.SeqDiff(lastProcessedCommand,_predictionPlayerStates.First.Id);

            //apply prediction
            if (diff >= 0 && diff < _predictionPlayerStates.Count)
            {
                // Debug.Log($"Received server state: {serverState.LastProcessedCommand}, OUR: {_predictionPlayerStates.First.Id}, DF:{diff}, total: {_predictionPlayerStates.Count}, rolledPos: {_position}");

                // Debug.Log($"[OK]  SP: {serverState.LastProcessedCommand}, OUR: {_predictionPlayerStates.First.Id}, DF:{diff}");
                _predictionPlayerStates.RemoveFromStart(diff+1);

                foreach (var state in _predictionPlayerStates)
                    ApplyInput(state, state.Delta);
                // Debug.Log($"After Received server total: {_predictionPlayerStates.Count}, rolledPos: {_position}");
            }
            else if(diff >= _predictionPlayerStates.Count)
            {
                Debug.Log($"[C] Player input lag st: {_predictionPlayerStates.First.Id} ls:{lastProcessedCommand} df:{diff}");
                //lag
                _predictionPlayerStates.FastClear();
                _nextCommand.Id = lastProcessedCommand;
            }
            else
            {
                Debug.Log($"[ERR] SP: {serverState.LastProcessedCommand}, OUR: {_predictionPlayerStates.First.Id}, DF:{diff}, STORED: {StoredCommands}");
            }
        }

        public override void Spawn(Vector2 position)
        {
            base.Spawn(position);
        }

        public void SetInput(Vector2 velocity, float rotation, bool fire, float dt)
        {
            _nextCommand.Keys = 0;
            if(fire)
                _nextCommand.Keys |= MovementKeys.Fire;
            
            if (velocity.x < -0.5f)
                _nextCommand.Keys |= MovementKeys.Left;
            if (velocity.x > 0.5f)
                _nextCommand.Keys |= MovementKeys.Right;
            if (velocity.y < -0.5f)
                _nextCommand.Keys |= MovementKeys.Up;
            if (velocity.y > 0.5f)
                _nextCommand.Keys |= MovementKeys.Down;

            _nextCommand.Rotation = rotation;
            
            //Debug.Log($"[C] SetInput: {_nextCommand.Keys}");
            
            UpdateLocal(dt);
        }

        public override void Update(float delta)
        {
            LastPosition = _view.Rb.position;
            LastRotation = _view.Rb.rotation;

            // _updateCount++;
            // // if (_updateCount == 3)
            // {
            //     _updateCount = 0;
            //     foreach (var t in _predictionPlayerStates)
            //         _clientLogic.SendPacketSerializable(PacketType.Movement, t, DeliveryMethod.Unreliable);
            // }
            //
            // base.Update(delta);
            
            
            foreach (var t in _packets)
                _clientLogic.SendPacketSerializable(PacketType.Movement, t, DeliveryMethod.Unreliable);
            _packets.FastClear();
            
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            Vector2 velocity = Vector2.zero;
            
            if ((command.Keys & MovementKeys.Up) != 0)
                velocity.y = -1f;
            if ((command.Keys & MovementKeys.Down) != 0)
                velocity.y = 1f;
            
            if ((command.Keys & MovementKeys.Left) != 0)
                velocity.x = -1f;
            if ((command.Keys & MovementKeys.Right) != 0)
                velocity.x = 1f;     
            
            Rigidbody2D rb = _view.Rb;
            
            _view.Move(velocity.normalized * (_speed * delta));
            _rotation = command.Rotation;

            if ((command.Keys & MovementKeys.Fire) != 0)
            {
                if (_shootTimer.IsTimeElapsed)
                {
                    _shootTimer.Reset();
                    Shoot();
                }
            }

        }

        public void UpdateLocal(float delta)
        {
            _nextCommand.Id = (ushort)((_nextCommand.Id + 1) % NetworkGeneral.MaxGameSequence);
            _nextCommand.ServerTick = _lastServerState.Tick;
            _nextCommand.Delta = delta;
            _nextCommand.Time = Time.time;
            ApplyInput(_nextCommand, delta);
            if (_predictionPlayerStates.IsFull)
            {
                Debug.LogWarning("Input is too fast for server. Prediction buffer is full, clearing.");
                _nextCommand.Id = (ushort)(_lastServerState.LastProcessedCommand+1);
                _predictionPlayerStates.FastClear();
            }
            _predictionPlayerStates.Add(_nextCommand);

            
            _tickTime += delta;
            
            // if (_tickTime > LogicTimerClient.FixedDelta*2)
            // {
            //     Debug.LogWarning($"Player tick time exceeded: {_tickTime} (max {LogicTimerClient.FixedDelta}), after {_sentPackets} updates");
            //     return;
            // }
            _sentPackets++;
            _packetsToSend++;            
            
            _packets.Add(_nextCommand);
            
            // _clientLogic.SendPacketSerializable(PacketType.Movement, _nextCommand, DeliveryMethod.Unreliable);
            
            base.Update(delta);
        }
    }
}