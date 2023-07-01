using System;
using Unity.Collections;

namespace DesertImage.ECS
{
    [Serializable]
    public struct WorldState : IDisposable
    {
        public readonly NativeHashMap<uint, Entity> Entities;
        public readonly NativeHashMap<uint, NativeHashSet<uint>> Components;

        public WorldState(NativeHashMap<uint, Entity> entities, NativeHashMap<uint, NativeHashSet<uint>> components)
        {
            Entities = entities;
            Components = components;
        }

        public void Dispose()
        {
            Entities.Dispose();
            Components.Dispose();
        }
    }
}