﻿using System;
using System.Collections.Generic;
using Code.Client.Logic;
using UnityEngine;
using Code.Shared;

namespace Code.Client
{
    [CreateAssetMenu(fileName = "PlayerViewPrefabs", menuName = "PlayerViewPrefabs")]
    public class PlayerViewPrefabs : ScriptableObject
    {
        public PlayerView PlayerPrefab;

        [Serializable]
        public struct HardpointPrefabEntry
        {
            public HardpointType Type;
            [Tooltip("Prefab must have a component implementing IHardpointView")]
            public GameObject Prefab;
        }

        [Tooltip("Assign prefabs for each HardpointType here")]
        public List<HardpointPrefabEntry> HardpointEntries = new();

        private Dictionary<HardpointType, GameObject> _hardpointPrefabs;

        private void OnEnable()
        {
            // Build the dictionary from the list
            _hardpointPrefabs = new Dictionary<HardpointType, GameObject>();
            foreach (var entry in HardpointEntries)
            {
                if (entry.Prefab == null || entry.Prefab.GetComponent<IHardpointView>() == null)
                {
                    Debug.LogError($"Prefab for {entry.Type} must have a component implementing IHardpointView.");
                    continue;
                }
                _hardpointPrefabs[entry.Type] = entry.Prefab;
            }
        }

        /// <summary>
        /// Instantiates a prefab and retrieves its IHardpointView interface.
        /// </summary>
        public IHardpointView InstantiateHardpointPrefab(HardpointType type, Transform parent, Vector2Int position)
        {
            if (!_hardpointPrefabs.TryGetValue(type, out var prefab) || prefab == null)
            {
                Debug.LogError($"No prefab found for HardpointType: {type}");
                return null;
            }

            var instance = Instantiate(prefab, parent);
            var hardpointView = instance.GetComponent<IHardpointView>();

            if (hardpointView == null)
            {
                Debug.LogError($"Prefab for {type} does not implement IHardpointView.");
                Destroy(instance);
                return null;
            }

            // Call Initialize on the IHardpointView implementation
            hardpointView.Initialize(parent, position);
            return hardpointView;
        }
    }
}