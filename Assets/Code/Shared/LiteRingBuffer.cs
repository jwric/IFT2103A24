using System;
using System.Collections;
using System.Collections.Generic;

namespace Code.Shared
{
    public class LiteRingBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _elements;
        private int _start;
        private int _end;
        private int _count;
        private readonly int _capacity;
        
        public ref T this[int i] => ref _elements[(_start + i) % _capacity];
        public LiteRingBuffer(int count)
        {
            _elements = new T[count];
            _capacity = count;
        }

        public void Add(T element)
        {
            if(_count == _capacity)
                throw new ArgumentException();
            _elements[_end] = element;
            _end = (_end + 1) % _capacity;
            _count++;
        }

        public void FastClear()
        {
            _start = 0;
            _end = 0;
            _count = 0;
        }

        public int Count => _count;
        public T First => _elements[_start];
        public T Last => _elements[(_start+_count-1)%_capacity];
        public bool IsFull => _count == _capacity;

        public void RemoveFromStart(int count)
        {
            if(count > _capacity || count > _count)
                throw new ArgumentException();
            _start = (_start + count) % _capacity;
            _count -= count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int counter = _start;
            while (counter != _end)
            {
                yield return _elements[counter];
                counter = (counter + 1) % _capacity;
            }           
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}