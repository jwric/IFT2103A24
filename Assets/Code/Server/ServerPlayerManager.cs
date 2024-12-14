using System.Collections.Generic;
using Code.Client;
using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class PlayerHandler
    {
        public readonly BasePlayer Player;
        public readonly ServerPlayerView View;

        public PlayerHandler(BasePlayer player, ServerPlayerView view)
        {
            Player = player;
            View = view;
        }

        public void Update(float delta)
        {
            if (Player is ServerPlayer serverPlayer)
            {
                serverPlayer.Update(delta);
            }
            else if (Player is AIPlayer aiPlayer)
            {
                aiPlayer.Update(delta);
            }
        }
    }
    
    public class ServerPlayerManager : BasePlayerManager
    {
        private readonly ServerLogic _serverLogic;
        private readonly PlayerHandler[] _players;
        
        public readonly PlayerState[] PlayerStates;
        private int _playersCount;
        
        
        public override int Count => _playersCount;

        public ServerPlayerManager(ServerLogic serverLogic)
        {
            _serverLogic = serverLogic;
            _players = new PlayerHandler[ServerLogic.MaxPlayers];
            PlayerStates = new PlayerState[ServerLogic.MaxPlayers];
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
        
        public BasePlayer GetPlayer(byte id)
        {
            for (int i = 0; i < _playersCount; i++)
            {
                if (_players[i].Player.Id == id)
                {
                    return _players[i].Player;
                }
            }
            return null;
        }
        
        public override void OnShoot(BasePlayer from, byte hardpointId, Vector2 to, BasePlayer hit, byte damage)
        {
            if (from is ServerPlayer serverPlayer)
            {
                ShootPacket sp = new ShootPacket
                {
                    FromPlayer = serverPlayer.Id,
                    HardpointId = hardpointId,
                    CommandId = serverPlayer.LastProcessedCommandId,
                    ServerTick = _serverLogic.Tick,
                    Hit = to,
                    AnyHit = hit != null,
                    PlayerHit = hit != null ? hit.Id : (byte)0
                };
                _serverLogic.SendShoot(ref sp);
            }
            else if (from is AIPlayer aiPlayer)
            {
                ShootPacket sp = new ShootPacket
                {
                    FromPlayer = aiPlayer.Id,
                    HardpointId = hardpointId,
                    CommandId = aiPlayer.LastProcessedCommandId,
                    ServerTick = _serverLogic.Tick,
                    Hit = to,
                    AnyHit = hit != null,
                    PlayerHit = hit != null ? hit.Id : (byte)0
                };
                _serverLogic.SendShoot(ref sp);
            }
            
            if (hit != null)
            {
                var serverHit = hit;
                serverHit.OnHit(damage, from);
            }
        }

        public override void OnHardpointAction(BasePlayer player, HardpointAction action)
        {
            // send hardpoint action packets to every client except the player
            HardpointActionPacket hap = new HardpointActionPacket
            {
                PlayerId = player.Id,
                HardpointId = action.SlotId,
                ActionCode = action.ActionCode,
                ServerTick = _serverLogic.Tick,
            };
            Debug.Log($"[S] Player {player.Id} action {action.ActionCode} on hardpoint {action.SlotId}");
            _serverLogic.SendHardpointAction(ref hap);
        }

        public override void OnPlayerDeath(BasePlayer player, BasePlayer killer)
        {
            _serverLogic.SendPlayerDeath(player.Id, killer?.Id ?? 0);
            if (player is ServerPlayer serverPlayer)
            {
                serverPlayer.Die();
            }
            else if (player is AIPlayer aiPlayer)
            {
                aiPlayer.Die();
            }
            RemovePlayer(player.Id);
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
        
        public void AddBot(AIPlayer bot, ServerPlayerView view)
        {
            PlayerHandler ph = new PlayerHandler(bot, view);
            bot.SetPlayerView(view);
            _players[_playersCount] = ph;
            _playersCount++;
        }

        public override void LogicUpdate()
        {
            for (int i = 0; i < _playersCount; i++)
            {
                var p = _players[i];
                p.Update(LogicTimerServer.FixedDelta);
                if (p.Player is ServerPlayer serverPlayer)
                {
                    PlayerStates[i] = serverPlayer.NetworkState;
                }
                else if (p.Player is AIPlayer aiPlayer)
                {
                    PlayerStates[i] = aiPlayer.NetworkState;
                }
            }
        }

        public void PreUpdate()
        {
            for (int i = 0; i < _playersCount; i++)
            {
                var p = _players[i];
                if (p.Player is ServerPlayer serverPlayer)
                {
                    serverPlayer.PreUpdate();
                }
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
