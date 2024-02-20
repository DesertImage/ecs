using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DesertImage.ECS;
using Unity.Collections;

namespace DesertImage.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(UnsafeHashSetDebugView<>))]
    public unsafe struct UnsafeHashSet<T> : IDisposable, IEnumerable<T> where T : unmanaged
    {
        internal struct Entry
        {
            public int HashCode;
            public T Value;
        }

        public bool IsNotNull { get; }

        public int Count { get; private set; }

        internal UnsafeArray<int>* _buckets;
        internal UnsafeArray<Entry>* _entries;
        internal UnsafeArray<int>* _lockIndexes;

        private int _entriesCapacity;

        public UnsafeHashSet(int capacity, Allocator allocator) : this()
        {
            _entriesCapacity = 5;

            _buckets = MemoryUtility.Allocate(new UnsafeArray<int>(capacity, allocator, -1));
            _entries = MemoryUtility.Allocate(new UnsafeArray<Entry>(capacity * _entriesCapacity, allocator, default));
            _lockIndexes = MemoryUtility.Allocate(new UnsafeArray<int>(capacity, allocator, default));

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

            _lockIndexes->Get(bucketNumber).Lock();
            {
                var buckets = *_buckets;
                var entryNumber = buckets[bucketNumber];

                (*_entries)[entryNumber] = default;
                buckets[bucketNumber] = -1;

                Count--;
            }
            _lockIndexes->Get(bucketNumber).Unlock();
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
            var oldSize = _buckets->Length;

            var bucketNew = MemoryUtility.Allocate(new UnsafeArray<int>(newSize, Allocator.Persistent));
            var entriesNew = MemoryUtility.Allocate(new UnsafeArray<Entry>(newSize, Allocator.Persistent));
            var lockIndexesNew = MemoryUtility.Allocate(new UnsafeArray<int>(newSize, Allocator.Persistent));

            for (var i = 0; i < oldSize; i++)
            {
                var entryNumber = (*_buckets)[i];

                if (entryNumber < 0) continue;

                var entry = (*_entries)[entryNumber];

                var hashCode = entry.HashCode;

                var newBucketNumber = GetBucketNumber(hashCode);
                var newEntryNumber = GetFreeEntryIndex(newBucketNumber);

                (*bucketNew)[newBucketNumber] = newEntryNumber;
                (*lockIndexesNew)[newBucketNumber] = (*_lockIndexes)[newBucketNumber];
                (*_entries)[newEntryNumber] = entry;
            }

            _buckets->Dispose();
            _entries->Dispose();
            _lockIndexes->Dispose();

            _buckets = bucketNew;
            _entries = entriesNew;
            _lockIndexes = lockIndexesNew;
        }

        public void Dispose()
        {
            _buckets->Dispose();
            _entries->Dispose();
            _lockIndexes->Dispose();

            _buckets = null;
            _entries = null;
            _lockIndexes = null;
        }

        private void Insert(T value)
        {
#if DEBUG
            if (!IsNotNull) throw new Exception("Dictionary is null");
#endif
            var hashCode = value.GetHashCode();
            var bucketNumber = (hashCode & int.MaxValue) % _buckets->Length;

            _lockIndexes->Get(bucketNumber).Lock();
            {
                var freeEntryIndex = GetFreeEntryIndex(bucketNumber);

                (*_buckets)[bucketNumber] = freeEntryIndex;
                (*_entries)[freeEntryIndex] = new Entry
                {
                    HashCode = hashCode,
                    Value = value,
                };

                Count++;
            }
            _lockIndexes->Get(bucketNumber).Unlock();
        }

        private int GetFreeEntryIndex(int bucketNumber)
        {
            var entriesNumber = bucketNumber * _entriesCapacity;
            var lastBucketEntryNumber = entriesNumber + _entriesCapacity - 1;

            var entries = *_entries;
            for (var i = entriesNumber; i < lastBucketEntryNumber; i++)
            {
                var entry = entries[i];

                if (entry.HashCode == 0) return i;
            }

            throw new Exception("no free entries");
            // return -1;
        }

        private int GetEntry(int bucketNumber, T key, int hashCode)
        {
            var entriesNumber = bucketNumber * _entriesCapacity;
            var lastBucketEntryNumber = entriesNumber + _entriesCapacity - 1;

            var entries = *_entries;
            for (var i = entriesNumber; i < lastBucketEntryNumber; i++)
            {
                var entry = entries[i];

                if (entry.HashCode == hashCode && key.Equals(entry.Value)) return i;
            }

            return -1;
        }

        private int GetBucketNumber(T key) => (key.GetHashCode() & int.MaxValue) % _buckets->Length;
        private int GetBucketNumber(int hashCode) => (hashCode & int.MaxValue) % _buckets->Length;

        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnsafeHashSet<T> _data;

            public T Current => (*_data._entries)[_counter - 1].Value;
            object IEnumerator.Current => Current;

            private int _counter;
            private int _lastEntry;

            public Enumerator(UnsafeHashSet<T> data) : this()
            {
                _data = data;
                _counter = 0;
            }

            public bool MoveNext()
            {
                var foundNext = false;

                var entries = *_data._entries;
                for (var i = _counter; i < entries.Length; i++)
                {
                    if (entries[i].HashCode == 0) continue;
                    foundNext = true;
                    _counter = i + 1;
                    break;
                }

                return foundNext && _counter - 1 < entries.Length;
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

    internal sealed unsafe class UnsafeHashSetDebugView<T> where T : unmanaged
    {
        private readonly UnsafeHashSet<T> _data;

        public UnsafeHashSetDebugView(UnsafeHashSet<T> array) => _data = array;

        public int[] Buckets => _data._buckets->ToArray();
        public UnsafeHashSet<T>.Entry[] Entries => _data._entries->ToArray();
    }
}