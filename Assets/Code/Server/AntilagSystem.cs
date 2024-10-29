using System.Collections.Generic;
using UnityEngine;

namespace Code.Server
{
    public struct StateInfo
    {
        public Vector2 Position;
    }

    public class AntilagSystem
    {
        private readonly Dictionary<int, StateInfo>[] _storedPositions;
        private readonly Dictionary<int, StateInfo> _savedStates;
        private int _currentArrayPos;
        private ushort _lastTick;
        private readonly int _maxTicks;

        public AntilagSystem(int maxTicks, int maxPlayers)
        {
            int dictSize = (maxPlayers + 1) * 3;

            _maxTicks = maxTicks;
            _storedPositions = new Dictionary<int, StateInfo>[maxTicks];
            _savedStates = new Dictionary<int, StateInfo>(dictSize);

            for (int i = 0; i < _storedPositions.Length; i++)
            {
                _storedPositions[i] = new Dictionary<int, StateInfo>(dictSize);
            }
        }

        // Retrieve the stored states for a given tick
        private Dictionary<int, StateInfo> GetStates(ushort tick)
        {
            if (tick < _lastTick - _maxTicks || _lastTick < _maxTicks)
                return null;
            return _storedPositions[(_currentArrayPos - _lastTick + tick - 1 + _maxTicks) % _maxTicks];
        }

        // Store the current positions of all active players at the given server tick
        public void StorePositions(ushort serverTick, ServerPlayer[] players)
        {
            var currentDict = _storedPositions[_currentArrayPos];
            currentDict.Clear();

            foreach (var p in players)
            {
                if (!p.IsAlive)
                    continue;

                StateInfo si = new StateInfo
                {
                    Position = p.Position
                };
                currentDict[p.AssociatedPeer.Id] = si;
            }

            _lastTick = serverTick;
            _currentArrayPos = (_currentArrayPos + 1) % _maxTicks;
        }

        // Apply anti-lag for the specified tick, excluding the player with exceptId
        public bool TryApplyAntilag(ServerPlayer[] players, ushort tick, int exceptId)
        {
            var antilagStates = GetStates(tick);
            if (antilagStates == null)
                return false;

            _savedStates.Clear();

            foreach (var p in players)
            {
                int id = p.AssociatedPeer.Id;
                if (id == exceptId)
                    continue;

                // Save current state
                StateInfo state = new StateInfo
                {
                    Position = p.Position
                };
                _savedStates[id] = state;

                // Apply anti-lag state
                if (antilagStates.TryGetValue(id, out var antilagState))
                {
                    // Change the state of the player to the stored (rewound) state
                    // p.ChangeState(antilagState.Position, true);
                }
            }

            return true;
        }

        // Revert players to their saved states after anti-lag calculations
        public void RevertAntilag(ServerPlayer[] players)
        {
            foreach (var p in players)
            {
                if (_savedStates.TryGetValue(p.AssociatedPeer.Id, out var state))
                {
                    // Restore the original saved state for the player
                    // p.ChangeState(state.Position, true);
                }
            }
        }
    }
}
