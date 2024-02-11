using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace DesertImage.ECS
{
    public unsafe struct UnsafeHashSet<T> : IDisposable, IEnumerable<T> where T : unmanaged
    {
        private struct Entry
        {
            public int HashCode;
            public T Value;
        }

        public bool IsNotNull { get; }

        public int Count { get; private set; }

        private UnsafeArray<int> _buckets;
        private UnsafeArray<Entry> _entries;
        private UnsafeArray<int> _lockIndexes;

        private int _entriesCapacity;

        public UnsafeHashSet(int capacity, Allocator allocator) : this()
        {
            _entriesCapacity = 5;

            _buckets = new UnsafeArray<int>(capacity, allocator, -1);
            _entries = new UnsafeArray<Entry>(capacity * _entriesCapacity, allocator, default);
            _lockIndexes = new UnsafeArray<int>(capacity, allocator, default);

            IsNotNull = true;

            Count = 0;
        }

        public void Add(T key) => Insert(key);

        public void Remove(T key)
        {
#if DEBUG
            if (!IsNotNull) throw new Exception("Dictionary is null");
#endif

            if (!Contains(key))
            {
#if DEBUG
                throw new Exception($"missing {key}");
#endif
                return;
            }

            var bucketNumber = GetBucketNumber(key);

            _lockIndexes.Get(bucketNumber).Lock();
            {
                var entryNumber = _buckets[bucketNumber];

                _entries[entryNumber] = default;
                _buckets[bucketNumber] = -1;

                Count--;
            }
            _lockIndexes.Get(bucketNumber).Unlock();
        }

        public bool Contains(T key)
        {
#if DEBUG
            if (!IsNotNull) throw new Exception("Dictionary is null");
#endif
            if (Count == 0) return false;

            var hashCode = key.GetHashCode();
            var bucketNumber = GetBucketNumber(hashCode);

            return GetEntry(bucketNumber, key, hashCode) >= 0;
        }

        public void Resize(int newSize)
        {
            var oldSize = _buckets.Length;

            var bucketNew = new UnsafeArray<int>(newSize, Allocator.Persistent);
            var entriesNew = new UnsafeArray<Entry>(newSize, Allocator.Persistent);
            var lockIndexesNew = new UnsafeArray<int>(newSize, Allocator.Persistent);

            for (var i = 0; i < oldSize; i++)
            {
                var entryNumber = _buckets[i];

                if (entryNumber < 0) continue;

                var entry = _entries[entryNumber];

                var hashCode = entry.HashCode;

                var newBucketNumber = GetBucketNumber(hashCode);
                var newEntryNumber = GetFreeEntryIndex(newBucketNumber);

                bucketNew[newBucketNumber] = newEntryNumber;
                lockIndexesNew[newBucketNumber] = _lockIndexes[newBucketNumber];
                _entries[newEntryNumber] = entry;
            }

            _buckets.Dispose();
            _entries.Dispose();
            _lockIndexes.Dispose();

            _buckets = bucketNew;
            _entries = entriesNew;
            _lockIndexes = lockIndexesNew;
        }

        public void Dispose()
        {
            _buckets.Dispose();
            _entries.Dispose();
            _lockIndexes.Dispose();
        }

        private void Insert(T value)
        {
#if DEBUG
            if (!IsNotNull) throw new Exception("Dictionary is null");
#endif
            var hashCode = value.GetHashCode();
            var bucketNumber = (hashCode & int.MaxValue) % _buckets.Length;

            _lockIndexes.Get(bucketNumber).Lock();
            {
                var freeEntryIndex = GetFreeEntryIndex(bucketNumber);

                _buckets[bucketNumber] = freeEntryIndex;
                _entries[freeEntryIndex] = new Entry
                {
                    HashCode = hashCode,
                    Value = value,
                };

                Count++;
            }
            _lockIndexes.Get(bucketNumber).Unlock();
        }

        private int GetFreeEntryIndex(int bucketNumber)
        {
            var entriesNumber = bucketNumber * _entriesCapacity;
            var lastBucketEntryNumber = entriesNumber + _entriesCapacity - 1;

            for (var i = entriesNumber; i < lastBucketEntryNumber; i++)
            {
                var entry = _entries[i];

                if (entry.HashCode == 0) return i;
            }

            throw new Exception("no free entries");
            return -1;
        }

        private int GetEntry(int bucketNumber, T key, int hashCode)
        {
            var entriesNumber = bucketNumber * _entriesCapacity;
            var lastBucketEntryNumber = entriesNumber + _entriesCapacity - 1;

            for (var i = entriesNumber; i < lastBucketEntryNumber; i++)
            {
                var entry = _entries[i];

                if (entry.HashCode == hashCode && key.Equals(entry.Value)) return i;
            }

            return -1;
        }

        private int GetBucketNumber(T key) => (key.GetHashCode() & int.MaxValue) % _buckets.Length;
        private int GetBucketNumber(int hashCode) => (hashCode & int.MaxValue) % _buckets.Length;

        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnsafeHashSet<T> _data;

            public T Current { get; }
            object IEnumerator.Current => Current;

            private int _counter;

            public Enumerator(UnsafeHashSet<T> data) : this()
            {
                _data = data;
                _counter = -1;
            }

            public bool MoveNext()
            {
                throw new NotImplementedException();

                ++_counter;
                return _counter < _data.Count;
            }

            public void Reset() => _counter = 0;

            public void Dispose()
            {
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}