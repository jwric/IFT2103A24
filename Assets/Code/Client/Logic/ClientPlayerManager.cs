using System.Collections.Generic;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public struct PlayerHandler
    {
        public readonly BasePlayer Player;
        public readonly IPlayerView View;

        public PlayerHandler(BasePlayer player, IPlayerView view)
        {
            Player = player;
            View = view;
        }

        public void Update(float delta)
        {
            Player.Update(delta);
        }
        
        public void FrameUpdate(float delta)
        {
            Player.FrameUpdate(delta);
        }
    }

    public class ClientPlayerManager : BasePlayerManager
    {
        private readonly Dictionary<byte, PlayerHandler> _players;
        private readonly ClientLogic _clientLogic;
        private ClientPlayer _clientPlayer;

        public ClientPlayer OurPlayer => _clientPlayer;
        public override int Count => _players.Count;

        public ClientPlayerManager(ClientLogic clientLogic)
        {
            _clientLogic = clientLogic;
            _players = new Dictionary<byte, PlayerHandler>();
        }
        
        public override IEnumerator<BasePlayer> GetEnumerator()
        {
            foreach (var ph in _players)
                yield return ph.Value.Player;
        }

        public void ApplyServerState(ref ServerState serverState)
        {
            for (int i = 0; i < serverState.PlayerStatesCount; i++)
            {
                var state = serverState.PlayerStates[i];
                if(!_players.TryGetValue(state.Id, out var handler))
                    return;

                if (handler.Player == _clientPlayer)
                {
                    _clientPlayer.ReceiveServerState(serverState, state);
                }
                else
                {
                    var rp = (RemotePlayer)handler.Player;
                    rp.OnPlayerState(state);
                }
            }
        }

        public override void OnShoot(BasePlayer from, byte hardpointId, Vector2 to, BasePlayer hit, byte damage)
        {
            if (from == _clientPlayer)
            {
                var cp = (ClientPlayer)from;
                
                cp.ShootHardpoint(hardpointId, to, hit, damage);
            }
        }

        public override void OnHardpointAction(BasePlayer player, HardpointAction action)
        {
            // if (player == _clientPlayer)
            //     return;
            player.OnHardpointAction(action);
        }

        public override void OnPlayerDeath(BasePlayer player, BasePlayer killer)
        {
            if (player == _clientPlayer)
            {
                var cp = (ClientPlayer)player;
                cp.Die();
                _clientLogic.ShowDeathScreen(killer);
                Debug.Log($"[C] You died. Killer: {killer?.Name}");
            }
            else
            {
                var rp = (RemotePlayer)player;
                rp.Die();
                Debug.Log($"[C] Player {player.Name} died. Killer: {killer?.Name}");
            }
        }

        public BasePlayer GetById(byte id)
        {
            return _players.TryGetValue(id, out var ph) ? ph.Player : null;
        }

        public BasePlayer RemovePlayer(byte id)
        {
            if (_players.TryGetValue(id, out var handler))
            {
                _players.Remove(id);
                handler.View.Destroy();
            }
        
            return handler.Player;
        }

        public override void LogicUpdate()
        {
            foreach (var kv in _players)
                kv.Value.Update(LogicTimerClient.FixedDelta);
        }
        
        public void FrameUpdate(float delta)
        {
            foreach (var kv in _players)
                kv.Value.FrameUpdate(delta);
        }

        public void AddClientPlayer(ClientPlayer player, PlayerView view)
        {
            _clientPlayer = player;
            _clientPlayer.SetPlayerView(view);
            _players.Add(player.Id, new PlayerHandler(player, view));
        }
        
        public void AddPlayer(RemotePlayer player, PlayerView view)
        {
            player.SetPlayerView(view);
            _players.Add(player.Id, new PlayerHandler(player, view));
        }

        public void Clear()
        {
            foreach (var p in _players.Values)
                p.View.Destroy();
            _players.Clear();
            _clientPlayer = null;
        }
    }
}