using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Code.Shared
{
    public class ObjectPoolManager : IDisposable
    {
        private readonly Dictionary<string, GameObjectPool<SpawnableObject>> _pools = new();

        public void AddPool<T>(string name, T prefab, int size) where T : SpawnableObject
        {
            var pool = new GameObjectPool<SpawnableObject>(() =>
            {
                var obj = Object.Instantiate(prefab);
                obj.Init(e => PutObject(name, e));
                return obj;
            }, size);
            _pools.Add(name, pool);
        }
        
        public T GetObject<T>(string name) where T : SpawnableObject
        {
            if (_pools.TryGetValue(name, out var pool))
            {
                return (T) pool.Get();
            }

            return null;
        }

        private void PutObject(string name, SpawnableObject obj)
        {
            if (_pools.TryGetValue(name, out var pool))
            {
                pool.Put(obj);
            }
        }

        public void Dispose()
        {
            foreach (var pool in _pools)
            {
                pool.Value.Dispose();
            }
            
            _pools.Clear();
        }
    }
}