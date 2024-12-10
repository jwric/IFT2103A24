using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Code.Server
{
    public class ServerLogic : MonoBehaviour, INetEventListener
    {
        [SerializeField] private ServerPlayerView _serverPlayerViewPrefab;
        [SerializeField] private Text _debugText;

        private NetManager _netManager;
        private NetPacketProcessor _packetProcessor;

        public const int MaxPlayers = 64;
        private LogicTimerServer _logicTimer;
        private readonly NetDataWriter _cachedWriter = new NetDataWriter();
        private ushort _serverTick;
        private ServerPlayerManager _playerManager;
        
        private PlayerInputPacket _cachedCommand = new PlayerInputPacket();
        private ServerState _serverState;
        public ushort Tick => _serverTick;

        public void StartServer(IPAddress address, int port)
        {
            Physics2D.simulationMode = SimulationMode2D.Script;

            if (_netManager.IsRunning)
                return;
            _netManager.Start(address, IPAddress.IPv6Any, port);
            _logicTimer.Start();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _logicTimer = new LogicTimerServer(() => {});
            _packetProcessor = new NetPacketProcessor();
            _playerManager = new ServerPlayerManager(this);
            
            //register auto serializable vector2
            _packetProcessor.RegisterNestedType((w, v) => w.Put(v), r => r.GetVector2());
           
            //register auto serializable PlayerState
            _packetProcessor.RegisterNestedType<PlayerState>();
            _packetProcessor.RegisterNestedType<PlayerInitialInfo>();
            
            _packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                // SimulateLatency = true,
                // SimulationMaxLatency = 25+10,
                // SimulationMinLatency = 25,
                // SimulatePacketLoss = true,
                // SimulationPacketLossChance = 2
            };
            
            
        }

        private void OnDestroy()
        {
            _netManager.Stop();
            _logicTimer.Stop();
        }
        private const int SimulatedLagMs = 20; // Change to desired lag in milliseconds
        private async Task SimulateLag()
        {
            await Task.Delay(SimulatedLagMs); // Delay to simulate lag
        }

        private void AddBot(Vector2 position)
        {
            var player = new AIPlayer(_playerManager, "Bot " + _playerManager.Count, (byte)_playerManager.Count, _serverTick - 1);
            var playerView = ServerPlayerView.Create(_serverPlayerViewPrefab, player);
            _playerManager.AddBot(player, playerView);

            // send player join packet
            var pj = new PlayerJoinedPacket
            {
                NewPlayer = true,
                InitialInfo = player.GetInitialInfo(),
                InitialPlayerState = player.NetworkState,
                ServerTick = _serverTick
            };
            _netManager.SendToAll(WritePacket(pj), DeliveryMethod.ReliableOrdered);
            
            // send spawn packet
            var sp = new SpawnPacket { PlayerId = player.Id, Position = position };
            _netManager.SendToAll(WriteSerializable(PacketType.Spawn, sp), DeliveryMethod.ReliableOrdered);
            
            player.Spawn(position);
        }
        
        private void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                AddBot(mousePos);
            }
            
            if (Input.GetMouseButton(0))
            {
                // apply force effect around mouse position
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                // apply force to all players
                foreach (var basePlayer in _playerManager)
                {
                    if (basePlayer is ServerPlayer player)
                    {
                        // the closer the player is to the mouse, the stronger the force
                        var dir = (player.Position - mousePos).normalized;
                        var distance = Vector2.Distance(player.Position, mousePos);
                        var maxDistance = 5f;
                        var maxForce = 10f;
                        var force = Mathf.Lerp(maxForce, 0, distance / maxDistance);
                        player.ApplyForce(dir * force);
                    }
                }
            }
            
            OnLogicUpdate();
        }

        private void OnLogicUpdate()
        {
            // dont update if server is not running
            if (!_netManager.IsRunning)
                return;
            
            Physics2D.Simulate(Time.fixedDeltaTime);
            // Debug.Log("Server tick: " + _serverTick);
            _serverTick = (ushort)((_serverTick + 1) % NetworkGeneral.MaxGameSequence);
            
            // await SimulateLag();
            
            _playerManager.LogicUpdate();
            if (_serverTick % 3 == 0)
            {
                _serverState.Tick = _serverTick;
                _serverState.PlayerStates = _playerManager.PlayerStates;
                int pCount = _playerManager.Count;
                
                foreach(var basePlayer in _playerManager)
                {
                    if (!(basePlayer is ServerPlayer player))
                        continue;

                    byte playerNumHardpoints = (byte)player.Hardpoints.Count;

                    int statesMax = player.AssociatedPeer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) - ServerState.HeaderSize;
                    statesMax /= PlayerState.CalculateSize(playerNumHardpoints);
                
                    for (int s = 0; s < (pCount-1)/statesMax + 1; s++)
                    {
                        //TODO: divide
                        _serverState.LastProcessedCommand = player.LastProcessedCommandId;
                        _serverState.PlayerStatesCount = pCount;
                        _serverState.StartState = s * statesMax;
                        // await SimulateLag();
                        player.AssociatedPeer.Send(WriteSerializable(PacketType.ServerState, _serverState), DeliveryMethod.Unreliable);
                    }
                }
            }
        }

        private void Update()
        {
            _netManager.PollEvents();
            _logicTimer.Update();
            
            // update debug text
            string debugText = $"Server tick: {_serverTick}\n";
            
            foreach (var basePlayer in _playerManager)
            {
                if (basePlayer is ServerPlayer p)
                {
                    debugText += p.GetDebugInfo();
                }
            }
            _debugText.text = debugText;
        }
        
        private NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte) type);
            packet.Serialize(_cachedWriter);
            return _cachedWriter;
        }

        private NetDataWriter WritePacket<T>(T packet) where T : class, new()
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte) PacketType.Serialized);
            _packetProcessor.Write(_cachedWriter, packet);
            return _cachedWriter;
        }

        private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
        {
            Debug.Log("[S] Join packet received: " + joinPacket.UserName);
            var player = new ServerPlayer(_playerManager, joinPacket.UserName, peer);
            var playerView = ServerPlayerView.Create(_serverPlayerViewPrefab, player);
            _playerManager.AddPlayer(player, playerView);

            
            player.Spawn(new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)));

            //Send join accept
            var playerInitialInfo = player.GetInitialInfo();
            var ja = new JoinAcceptPacket { OwnPlayerInfo = playerInitialInfo, ServerTick = _serverTick };
            peer.Send(WritePacket(ja), DeliveryMethod.ReliableOrdered);

            //Send to old players info about new player
            var pj = new PlayerJoinedPacket
            {
                NewPlayer = true,
                InitialInfo = player.GetInitialInfo(),
                InitialPlayerState = player.NetworkState,
                ServerTick = _serverTick
            };
            _netManager.SendToAll(WritePacket(pj), DeliveryMethod.ReliableOrdered, peer);
            
            //Send to new player info about old players
            pj.NewPlayer = false;
            foreach(var basePlayer in _playerManager)
            {
                if (basePlayer is ServerPlayer otherPlayer)
                {
                    if (otherPlayer == player)
                        continue;
                    var info = pj.InitialInfo;
                    info.UserName = otherPlayer.Name;
                    pj.InitialInfo = info;
                    pj.InitialPlayerState = otherPlayer.NetworkState;
                }
                else if (basePlayer is AIPlayer aiPlayer)
                {
                    var info = pj.InitialInfo;
                    info.UserName = aiPlayer.Name;
                    pj.InitialInfo = info;
                    pj.InitialPlayerState = aiPlayer.NetworkState;
                }
                peer.Send(WritePacket(pj), DeliveryMethod.ReliableOrdered);
            }
            
            // Send spawn packet
            var sp = new SpawnPacket { PlayerId = player.Id, Position = player.Position };
            _netManager.SendToAll(WriteSerializable(PacketType.Spawn, sp), DeliveryMethod.ReliableOrdered);
        }

        private void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null)
                return;
            _cachedCommand.Deserialize(reader);
            var player = (ServerPlayer) peer.Tag;

            if (!player.IsAlive)
                return;
            
            if (NetworkGeneral.SeqDiff(_serverTick, _cachedCommand.ServerTick) < 0)
            {
                Debug.LogWarning($"Player {player.Id} sent a command from the future: {_cachedCommand.ServerTick} vs actual {_serverTick}");
                return;
            }
            
            player.ApplyInput(_cachedCommand, _cachedCommand.Delta);
        }

        public void SendShoot(ref ShootPacket sp)
        {
            _netManager.SendToAll(WriteSerializable(PacketType.Shoot, sp), DeliveryMethod.ReliableUnordered);
        }

        public void SendHardpointAction(ref HardpointActionPacket hap)
        {
            _netManager.SendToAll(WriteSerializable(PacketType.HardpointAction, hap), DeliveryMethod.ReliableOrdered);
        }
        
        public void SendPlayerDeath(byte playerId, byte killerId)
        {
            var pd = new PlayerDeathPacket { Id = playerId, KilledBy = killerId, ServerTick = _serverTick };
            _netManager.SendToAll(WriteSerializable(PacketType.PlayerDeath, pd), DeliveryMethod.ReliableOrdered);
        }
        
        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[S] Player connected: " + peer);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

            if (peer.Tag != null)
            {
                byte playerId = (byte)peer.Id;
                if (_playerManager.RemovePlayer(playerId))
                {
                    var plp = new PlayerLeavedPacket { Id = (byte)peer.Id };
                    _netManager.SendToAll(WritePacket(plp), DeliveryMethod.ReliableOrdered);
                }
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[S] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType) packetType;
            switch (pt)
            {
                case PacketType.Movement:
                    OnInputReceived(reader, peer);
                    break;
                case PacketType.Serialized:
                    _packetProcessor.ReadAllPackets(reader, peer);
                    break;
                default:
                    Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {

        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (peer.Tag != null)
            {
                var p = (ServerPlayer) peer.Tag;
                p.Ping = latency;
            }
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("ExampleGame");
        }

        private void OnDrawGizmos()
        {
            if (_playerManager == null)
                return;
            foreach (var basePlayer in _playerManager)
            {
                if (basePlayer is ServerPlayer sp)
                    sp.DrawGizmos();
                if (basePlayer is AIPlayer aiPlayer)
                    aiPlayer.DrawGizmos();
            }
        }
    }
}