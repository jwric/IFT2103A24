using System.Collections.Generic;
using Code.Client;
using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class PlayerHandler
    {
        public readonly ServerPlayer Player;
        public readonly IPlayerView View;

        public PlayerHandler(ServerPlayer player, ServerPlayerView view)
        {
            Player = player;
            View = view;
        }

        public void Update(float delta)
        {
            Player.Update(delta);
        }
    }
    
    public class ServerPlayerManager : BasePlayerManager
    {
        private readonly ServerLogic _serverLogic;
        private readonly PlayerHandler[] _players;
        private readonly AntilagSystem _antilagSystem;
        
        public readonly PlayerState[] PlayerStates;
        private int _playersCount;
        
        
        public override int Count => _playersCount;

        public ServerPlayerManager(ServerLogic serverLogic)
        {
            _serverLogic = serverLogic;
            _antilagSystem = new AntilagSystem(60, ServerLogic.MaxPlayers);
            _players = new PlayerHandler[ServerLogic.MaxPlayers];
            PlayerStates = new PlayerState[ServerLogic.MaxPlayers];
        }

        public bool EnableAntilag(ServerPlayer forPlayer)
        {
            // return _antilagSystem.TryApplyAntilag(_players, _serverLogic.Tick, forPlayer.AssociatedPeer.Id);
            return false;
        }

        public void DisableAntilag()
        {
            // _antilagSystem.RevertAntilag(_players);            
        }

        public override IEnumerator<BasePlayer> GetEnumerator()
        {
            int i = 0;
            while (i < _playersCount)
            {
                yield return _players[i].Player;
                i++;
            }
        }
        
        public override void OnShoot(BasePlayer from, Vector2 to, BasePlayer hit)
        {
            var serverPlayer = (ServerPlayer) from;
            ShootPacket sp = new ShootPacket
            {
                FromPlayer = serverPlayer.Id,
                CommandId = serverPlayer.LastProcessedCommandId,
                ServerTick = _serverLogic.Tick,
                Hit = to
            };
            _serverLogic.SendShoot(ref sp);
        }

        public void AddPlayer(ServerPlayer player, ServerPlayerView view)
        {
            PlayerHandler ph = new PlayerHandler(player, view);
            player.SetPlayerView(view);
            for (int i = 0; i < _playersCount; i++)
            {
                if (_players[i].Player.Id == player.Id)
                {
                    _players[i] = ph;
                    return;
                }
            }

            _players[_playersCount] = ph;
            _playersCount++;
        }

        public override void LogicUpdate()
        {
            for (int i = 0; i < _playersCount; i++)
            {
                var p = _players[i];
                p.Update(LogicTimerServer.FixedDelta);
                PlayerStates[i] = p.Player.NetworkState;
            }
        }

        public bool RemovePlayer(byte playerId)
        {
            for (int i = 0; i < _playersCount; i++)
            {
                if (_players[i].Player.Id == playerId)
                {
                    _players[i].View.Destroy();
                    _playersCount--;
                    _players[i] = _players[_playersCount];
                    _players[_playersCount] = null;
                    return true;
                }
            }
            return false;
        }
    }
}
