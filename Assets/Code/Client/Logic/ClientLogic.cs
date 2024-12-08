using System;
using Code.Client.Managers;
using Code.Client.UI;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Code.Client.Logic
{
    public class ClientLogic
    {
        
        private string _username;
        
        private ServerState _cachedServerState;
        private ShootPacket _cachedShootPacket;
        
        private ushort _lastServerTick;
        
        private ClientPlayerManager _playerManager;
        
        public static LogicTimerClient LogicTimer { get; private set; }
        private ObjectPoolManager _objectPoolManager;

        public CameraFollow _camera;
        
        private Scene _rewindScene;
        
        
        private PlayerView _playerViewPrefab;
        public GameObject RewindGO;

        private GameHUDController _gameHUD;
        

        public ClientLogic()
        {
            LogicTimer = new LogicTimerClient(() => { });
            _cachedServerState = new ServerState();
            _cachedShootPacket = new ShootPacket();
            _playerManager = new ClientPlayerManager(this);
            _objectPoolManager = new ObjectPoolManager();
        }
        
        public void Init(CameraFollow camera, PlayerView playerViewPrefab, ShootEffect shootEffectPrefab, PooledParticleSystem hitParticles, GameObject rewindGO, GameHUDController gameHUD)
        {
            // create object pools
            _objectPoolManager.AddPool("shoot", shootEffectPrefab, 10);
            _objectPoolManager.AddPool("hit", hitParticles, 50);
            
            _camera = camera;
            _playerViewPrefab = playerViewPrefab;
            RewindGO = rewindGO;
            
            _gameHUD = gameHUD;
            
            _username = GameManager.Instance.Settings.Name;
            
            LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics2D);
            var rewindScene = SceneManager.LoadScene("RewindScene", parameters);
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                Debug.Log($"Scene loaded: {scene.name}");
                if (scene.name == "RewindScene")
                {
                    _rewindScene = scene;
                }
            };
            
            // packet handling
            var networkManager = GameManager.Instance.NetworkManager;
            networkManager.OnServerStateReceived += OnServerState;
            networkManager.OnPlayerJoined += OnPlayerJoined;
            networkManager.OnJoinAccept += OnJoinAccept;
            networkManager.OnPlayerLeaved += OnPlayerLeaved;
            networkManager.OnShoot += OnShoot;
            networkManager.OnPlayerDeath += OnPlayerDeath;
            networkManager.OnSpawn += OnSpawn;
            
            // send join request
            SendJoinRequest();
        }
        
        private void SendJoinRequest()
        {
            Debug.Log("[C] Connected to server");
            SendPacket(new JoinPacket {UserName = _username}, DeliveryMethod.ReliableOrdered);
        }
        
        private void OnLogicUpdate()
        {
            _playerManager.LogicUpdate();
        }
        
        public void Update()
        {
            LogicTimer.Update();
            _playerManager.FrameUpdate(Time.deltaTime);
        }
        
        public void FixedUpdate()
        {
            // simulating before logic update has bugs
            // Physics2D.Simulate(Time.fixedDeltaTime);
            OnLogicUpdate();
            Physics2D.Simulate(Time.fixedDeltaTime);
        }
        
        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            Debug.Log($"[C] Player joined: {packet.UserName}");
            var remotePlayer = new RemotePlayer(_playerManager, packet.UserName, packet);
            var view = PlayerView.Create(_playerViewPrefab, remotePlayer);
            _playerManager.AddPlayer(remotePlayer, view);
        }

        private void OnServerState(ServerState packet)
        {
            _cachedServerState = packet;
            //skip duplicate or old because we received that packet unreliably
            if (NetworkGeneral.SeqDiff(_cachedServerState.Tick, _lastServerTick) <= 0)
                return;
            _lastServerTick = _cachedServerState.Tick;
            _playerManager.ApplyServerState(ref _cachedServerState);

            if (_playerManager.OurPlayer != null)
            {
                _gameHUD.UpdateHealth(_playerManager.OurPlayer.Health);
            }
        }

        private void OnShoot(ShootPacket packet)
        {
            _cachedShootPacket = packet;
            var p = _playerManager.GetById(_cachedShootPacket.FromPlayer);
            if (p != null && p != _playerManager.OurPlayer)
            {
                if (p is RemotePlayer rp)
                {
                    rp.OnShoot(_cachedShootPacket.Hit);
                }
                SpawnShoot(p.Position, _cachedShootPacket.Hit);
            }
            
            if (!_cachedShootPacket.AnyHit)
                return;
            
            var pHit = _playerManager.GetById(_cachedShootPacket.PlayerHit);

            var hitInfo = new HitInfo
            {
                Damager = p,
                Damage = 10,
                Position = _cachedShootPacket.Hit
            };
            pHit.NotifyHit(hitInfo);
        }
        
        private void OnPlayerDeath(PlayerDeathPacket packet)
        {
            var player = _playerManager.GetById(packet.Id);
            var killer = _playerManager.GetById(packet.KilledBy);
            if (player == null)
                return;

            if (player == _playerManager.OurPlayer)
            {
                var serverPlayerKiller = killer as RemotePlayer;
                _camera.target = serverPlayerKiller?.Transform;
            }
            
            _playerManager.OnPlayerDeath(player, killer);
            _playerManager.RemovePlayer(packet.Id);
        }
        
        private void OnSpawn(SpawnPacket packet)
        {
            var player = _playerManager.GetById(packet.PlayerId);
            if (player == null)
                return;
            player.Spawn(packet.Position);
        }

        public void SpawnShoot(Vector2 from, Vector2 to)
        {
            var particles = _objectPoolManager.GetObject<PooledParticleSystem>("hit");
            var effDir = (to - from).normalized;
            var effPos = from + effDir * 0.5f;
            var effAngle = Mathf.Atan2(effDir.y, effDir.x) * Mathf.Rad2Deg;
            particles.Spawn(effPos, effAngle);
            
            var eff = _objectPoolManager.GetObject<ShootEffect>("shoot");
            eff.Spawn(from, to);
        }

        private void OnPlayerLeaved(PlayerLeavedPacket packet)
        {
            var player = _playerManager.RemovePlayer(packet.Id);
            if(player != null)
                Debug.Log($"[C] Player leaved: {player.Name}");
        }

        private void OnJoinAccept(JoinAcceptPacket packet)
        {
            Debug.Log("[C] Join accept. Received player id: " + packet.Id);
            _lastServerTick = packet.ServerTick;
            var clientPlayer = new ClientPlayer(this, _playerManager, _username, packet.Id)
            {
                RewindScene = _rewindScene,
                RewindPhysicsScene = _rewindScene.GetPhysicsScene2D()
            };
            var view = PlayerView.Create(_playerViewPrefab, clientPlayer);
            _camera.target = view.transform;
            _playerManager.AddClientPlayer(clientPlayer, view);
        }
        
        public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            var networkManager = GameManager.Instance.NetworkManager;
            networkManager.SendPacketSerializable(type, packet, deliveryMethod);
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            var networkManager = GameManager.Instance.NetworkManager;
            networkManager.SendPacket(packet, deliveryMethod);
        }

        public void ShowDeathScreen(BasePlayer killer)
        {
            _gameHUD.ShowDeathScreen($"Killed by {killer.Name}");
        }
        
        public void Destroy()
        {
            _objectPoolManager.Dispose();
            
            _playerManager.Clear();
            
            // unload rewind scene
            SceneManager.UnloadSceneAsync(_rewindScene);
            
            // packet handling
            var networkManager = GameManager.Instance.NetworkManager;
            networkManager.OnServerStateReceived -= OnServerState;
            networkManager.OnPlayerJoined -= OnPlayerJoined;
            networkManager.OnJoinAccept -= OnJoinAccept;
            networkManager.OnPlayerLeaved -= OnPlayerLeaved;
            networkManager.OnShoot -= OnShoot;
            networkManager.OnPlayerDeath -= OnPlayerDeath;
            networkManager.OnSpawn -= OnSpawn;
        }
    }
}