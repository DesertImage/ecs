using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace DesertImage.ECS
{
    public struct Matcher
    {
        public readonly uint Id;

        public SortedSet<uint> Components { get; }
        public SortedSet<uint> NoneOfComponents { get; }

        private readonly HashSet<uint> _allOf;
        private readonly HashSet<uint> _noneOf;
        private readonly HashSet<uint> _anyOf;

        public Matcher(uint id, HashSet<uint> allOf, HashSet<uint> noneOf, HashSet<uint> anyOf)
        {
            Id = id;

            _allOf = allOf;
            _noneOf = noneOf;
            _anyOf = anyOf;

            Components = new SortedSet<uint>(allOf.Concat(anyOf).Except(_noneOf).ToArray());
            NoneOfComponents = new SortedSet<uint>(noneOf);
        }

        public bool Check(NativeHashSet<uint> componentIds)
        {
            return HasNot(componentIds) && HasAll(componentIds) && HasAnyOf(componentIds);
        }

        private bool HasNot(NativeHashSet<uint> componentIds)
        {
            foreach (var id in _noneOf)
            {
                if (componentIds.Contains(id)) return false;
            }

            return true;
        }

        private bool HasAll(NativeHashSet<uint> componentIds)
        {
            foreach (var id in _allOf)
            {
                if (!componentIds.Contains(id)) return false;
            }

            return true;
        }

        private bool HasAnyOf(NativeHashSet<uint> componentIds)
        {
            if (_anyOf.Count == 0) return true;
            
            foreach (var id in _anyOf)
            {
                if (componentIds.Contains(id)) return true;
            }

            return false;
        }
    }
}