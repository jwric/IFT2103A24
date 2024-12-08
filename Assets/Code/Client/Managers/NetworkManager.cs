using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Code.Client.Managers
{
    public class NetworkManager : INetEventListener
    {
        private readonly GameManager _gameManager;
        
        private readonly NetManager _client;
        private readonly NetDataWriter _writer;
        private readonly NetPacketProcessor _packetProcessor;
        
        private NetPeer _server;
        
        // Actions
        private Action<NetPeer> _onPeerConnected;
        private Action<DisconnectInfo> _onDisconnect;
        
        // packet listeners
        public event Action<DisconnectInfo> OnDisconnect;
        public event Action<SpawnPacket> OnSpawn;
        public event Action<ServerState> OnServerStateReceived;
        public event Action<PlayerJoinedPacket> OnPlayerJoined;
        public event Action<JoinAcceptPacket> OnJoinAccept;
        public event Action<PlayerLeavedPacket> OnPlayerLeaved;
        public event Action<ShootPacket> OnShoot;
        public event Action<PlayerDeathPacket> OnPlayerDeath;
        
        public int Ping { get; private set; }
        
        public NetworkManager(GameManager gameManager)
        {
            _gameManager = gameManager;
            _client = new NetManager(this)
            {
                AutoRecycle = true,
                // SimulateLatency = true,
                // SimulationMaxLatency = 25 + 10,
                // SimulationMinLatency = 25,
                // SimulatePacketLoss = true,
                // SimulationPacketLossChance = 2
            };
            _writer = new NetDataWriter();
            _packetProcessor = new NetPacketProcessor();
            _packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
            _packetProcessor.RegisterNestedType<PlayerState>();
            _packetProcessor.SubscribeReusable<PlayerJoinedPacket>(OnPlayerJoinedPacket);
            _packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAcceptPacket);
            _packetProcessor.SubscribeReusable<PlayerLeavedPacket>(OnPlayerLeavedPacket);
        }
        
        public void OnPlayerJoinedPacket(PlayerJoinedPacket packet)
        {
            OnPlayerJoined?.Invoke(packet);
        }
        
        public void OnJoinAcceptPacket(JoinAcceptPacket packet)
        {
            OnJoinAccept?.Invoke(packet);
        }
        
        public void OnPlayerLeavedPacket(PlayerLeavedPacket packet)
        {
            OnPlayerLeaved?.Invoke(packet);
        }
        
        public void Start()
        {
            _client.Start();
        }
        
        public IEnumerator ConnectAsync(string ip, int port, Action<bool, string> onResult)
        {
            bool isConnected = false;
            string message = "Connecting...";
            bool isComplete = false;

            // Set up callbacks for success and failure
            _onPeerConnected = peer =>
            {
                isConnected = true;
                message = "Connected successfully!";
                isComplete = true;
            };

            _onDisconnect = disconnectInfo =>
            {
                isConnected = false;
                message = $"Failed to connect: {disconnectInfo.Reason}";
                isComplete = true;
            };

            // Initiate the connection
            _server = _client.Connect(ip, port, "ExampleGame");

            // Wait until connection completes
            while (!isComplete)
            {
                Poll(); // Poll for network events
                yield return null;
            }
            
            _onPeerConnected = null;
            _onDisconnect = null;

            // Invoke the result callback
            onResult?.Invoke(isConnected, message);
        }
        
        public void Connect(string ip, int port, Action<NetPeer> onPeerConnected, Action<DisconnectInfo> onDisconnect)
        {
            _server = null;
            _onPeerConnected = onPeerConnected;
            _onDisconnect = onDisconnect;
            _server = _client.Connect(ip, port, "ExampleGame");
        }
        
        public void Disconnect()
        {
            if (_server == null)
                return;
            _server.Disconnect();
            _server = null;
        }

        public void Poll()
        {
            _client.PollEvents();
        }
        
        public void Stop()
        {
            _client.Stop();
        }

        public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _server.Send(_writer, deliveryMethod);
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte) PacketType.Serialized);
            _packetProcessor.Write(_writer, packet);
            _server.Send(_writer, deliveryMethod);
        }
        
        public void OnPeerConnected(NetPeer peer)
        {
            _onPeerConnected?.Invoke(peer);
            _onPeerConnected = null;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _onDisconnect?.Invoke(disconnectInfo);
            _onDisconnect = null;
            
            OnDisconnect?.Invoke(disconnectInfo);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log($"Error: {socketError}");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType) packetType;
            switch (pt)
            {
                case PacketType.Spawn:
                    SpawnPacket spawnPacket = new SpawnPacket();
                    spawnPacket.Deserialize(reader);
                    OnSpawn?.Invoke(spawnPacket);
                    break;
                case PacketType.ServerState:
                    // invoke the handler
                    ServerState state = new ServerState();
                    state.Deserialize(reader);
                    OnServerStateReceived?.Invoke(state);
                    break;
                case PacketType.Serialized:
                    _packetProcessor.ReadAllPackets(reader);
                    break;
                case PacketType.Shoot:
                    ShootPacket shootPacket = new ShootPacket();
                    shootPacket.Deserialize(reader);
                    OnShoot?.Invoke(shootPacket);
                    break;
                case PacketType.PlayerDeath:
                    PlayerDeathPacket deathPacket = new PlayerDeathPacket();
                    deathPacket.Deserialize(reader);
                    OnPlayerDeath?.Invoke(deathPacket);
                    break;
                default:
                    Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            Ping = latency;
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }
}