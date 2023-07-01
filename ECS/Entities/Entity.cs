using System;

namespace DesertImage.ECS
{
    [Serializable]
    public struct Entity
    {
        public readonly uint Id;

        public Entity(uint id) => Id = id;
    }
}