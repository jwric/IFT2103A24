using System;

namespace Code.Shared
{
    public class GameObjectPool<T> : IDisposable where T : class
    {
        private readonly T[] _pool;
        private readonly Func<T> _creator;
        private int _count;

        public GameObjectPool(Func<T> creator) : this(creator, 8)
        {

        }

        public GameObjectPool(Func<T> creator, int capacity)
        {
            _pool = new T[capacity];
            _creator = creator;
        }

        public T Get()
        {
            if (_count > 0)
            {
                _count--;
                var result = _pool[_count];
                _pool[_count] = default;
                return result;
            }
            return _creator();
        }

        public void Put(T gameObject)
        {
            _pool[_count] = gameObject;
            _count++;
        }
        
        public void Dispose()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_pool[i] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}