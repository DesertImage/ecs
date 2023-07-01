using System;

namespace DesertImage.ECS
{
    [Serializable]
    public struct EntitiesGroup
    {
        public readonly uint Id;

        public SparseSet<uint> Entities;

        public EntitiesGroup(uint id)
        {
            Id = id;
            Entities = new SparseSet<uint>(ECSSettings.ComponentsDenseCapacity, ECSSettings.ComponentsSparseCapacity);
        }

        public void Add(uint entityId) => Entities.Add(entityId, entityId);
        public void Remove(uint entityId) => Entities.Remove(entityId);
        public bool Contains(uint entityId) => Entities.Contains(entityId);
    }
}