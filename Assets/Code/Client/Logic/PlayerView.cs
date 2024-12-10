﻿using System;
using System.Collections;
using System.Collections.Generic;
using Code.Client.Managers;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    [RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
    public class PlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private TextMesh _name;
        
        private Rigidbody2D _rb;
        private Collider2D _collider;

        public Rigidbody2D Rb => _rb;

        private readonly Dictionary<byte, IHardpointView> _hardpoints = new();
        
        // [SerializeField]
        // private HardpointView _hardpointView;
        
        
        public static PlayerView Create(PlayerView prefab, BasePlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, player.Position, rot);
            obj._name.text = player.Name;
            obj.SetHardpoints(player.Hardpoints);
            return obj;
        }

        private void SetHardpoints(List<HardpointSlot> hardpoints)
        {
            ClearHardpoints();
            GameManager gm = GameManager.Instance;
            var prefabDb = gm.PlayerViewPrefabs;
            foreach (var slot in hardpoints)
            {
                var hardpointView = prefabDb.InstantiateHardpointPrefab(slot.Hardpoint.Type, transform, slot.Position);
                _hardpoints.Add(slot.Id, hardpointView);
            }
        }
        
        public void GetHardpointView(byte id, out IHardpointView hardpointView)
        {
            _hardpoints.TryGetValue(id, out hardpointView);
        }
        
        public IHardpointView GetHardpointView(byte id)
        {
            _hardpoints.TryGetValue(id, out var hardpointView);
            return hardpointView;
        }
        
        private void ClearHardpoints()
        {
            foreach (var hardpoint in _hardpoints.Values)
            {
                hardpoint.Destroy();
            }
            _hardpoints.Clear();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }
        
        public void OnHit(HitInfo hitInfo)
        {
            // set sprite red for a moment
            StartCoroutine(HitEffect());
        }
        
        private IEnumerator HitEffect()
        {
            var spriteRenderer = _name;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.cyan;
        }
        
        private void Update()
        {
        }

        public void Spawn(Vector2 position, float rotation)
        {
            _rb.position = position;
            _rb.rotation = rotation;
            enabled = true;
        }

        public void Die()
        {
            enabled = false;            
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

    }
}