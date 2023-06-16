﻿using System;

namespace DesertImage
{
    public class SparseSet<T>
    {
        private readonly T[] _dense;
        private readonly int[] _sparse;
        private int[] _recycled;

        private int _recycledCount;
        private int _denseCount = 1;

        public T this[int index] => _dense[_sparse[index]];
        
        public SparseSet(int denseCapacity, int sparseCapacity, int recycledCapacity = 100)
        {
            _dense = new T[denseCapacity];
            _sparse = new int[sparseCapacity];
            _recycled = new int[recycledCapacity];
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
            _sparse[index] = 0;

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

        public bool Contains(int index) => _sparse[index] != 0;
    }
}