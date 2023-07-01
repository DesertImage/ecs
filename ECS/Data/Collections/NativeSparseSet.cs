using System;
using Unity.Collections;

namespace DesertImage
{
    public struct NativeSparseSet<T> : IDisposable where T : unmanaged
    {
        public int Count => _denseCount;

        private NativeList<T> _dense;
        private NativeList<int> _sparse;
        private NativeList<int> _recycled;

        private int _recycledCount;
        private int _denseCount;

        public T this[int index] => _dense[index + 1];

        public NativeSparseSet(int denseCapacity, int sparseCapacity, int recycledCapacity = 100)
        {
            _dense = new NativeList<T>(denseCapacity, Allocator.Persistent);
            _sparse = new NativeList<int>(sparseCapacity, Allocator.Persistent);
            _recycled = new NativeList<int>(recycledCapacity, Allocator.Persistent);
            _recycledCount = 0;
            _denseCount = 0;
        }

        public void Add(int index, T value)
        {
            if (Contains(index))
            {
                _dense[_sparse[index]] = value;
                return;
            }

            var targetIndex = _recycledCount > 0 ? _recycled[--_recycledCount] : _denseCount;

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

        public T Get(int index) => _dense[_sparse[index]];

        private void AddRecycled(int oldSparse)
        {
            if (_recycledCount == _recycled.Length)
            {
                _recycled.Add(default);
            }

            _recycled[_recycledCount] = oldSparse;
            _recycledCount++;
        }

        public bool Contains(int index) => _sparse[index] != 0;

        public void Dispose()
        {
            _dense.Dispose();
            _sparse.Dispose();
            _recycled.Dispose();
        }
    }
}