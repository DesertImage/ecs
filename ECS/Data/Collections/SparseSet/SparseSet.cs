using System;

namespace DesertImage
{
    public class SparseSet<T> : SparseSetAbstract
    {
        public override int Count
        {
            get => _denseCount;
            protected set => _denseCount = value;
        }

        private T[] _dense;
        private int[] _sparse;
        private int[] _recycled;

        private int _recycledCount;
        private int _denseCount;

        public ref T this[int index] => ref _dense[index];

        public SparseSet(int denseCapacity, int sparseCapacity, int recycledCapacity = 100)
        {
            _dense = new T[denseCapacity];
            
            _sparse = new int[sparseCapacity];
            for (var i = 0; i < _sparse.Length; i++)
            {
                _sparse[i] = -1;
            }

            _recycled = new int[recycledCapacity];
        }

        public void Add(int index, in T value)
        {
            if (Contains(index))
            {
                _dense[_sparse[index]] = value;
                return;
            }

            var targetIndex = _recycledCount > 0 ? _recycled[--_recycledCount] : _denseCount;

            if (index >= _sparse.Length)
            {
                Array.Resize(ref _sparse, _sparse.Length << 1);
            }
            
            _sparse[index] = targetIndex;
            _dense[targetIndex] = value;

            _denseCount++;
        }

        public void Remove(int index)
        {
            var oldSparse = _sparse[index];

            _dense[_sparse[index]] = default;
            _sparse[index] = -1;

            _denseCount--;

            AddRecycled(oldSparse);
        }

        public ref T Get(int index) => ref _dense[_sparse[index]];

        private void AddRecycled(int oldSparse)
        {
            if (_recycledCount == _recycled.Length)
            {
                Array.Resize(ref _recycled, _recycledCount << 1);
            }

            _recycled[_recycledCount] = oldSparse;
            _recycledCount++;
        }

        public bool Contains(int index) => _sparse.Length > index && _sparse[index] != -1;
    }
}