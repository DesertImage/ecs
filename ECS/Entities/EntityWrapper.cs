using UnityEngine;

namespace DesertImage.ECS
{
    public class EntityWrapper : MonoBehaviour, IPoolable
    {
        public Entity Entity { get; private set; }

        private IEntityLinkable[] _entityLinkables;

        public void OnCreate()
        {
            Entity = Worlds.Current.GetNewEntity();

            _entityLinkables ??= GetComponents<IEntityLinkable>();
            foreach (var linkable in _entityLinkables)
            {
                linkable.Link(Entity);
            }
        }

        public void ReturnToPool()
        {
            Entity = default;
            Worlds.Current.DestroyEntity(Entity.Id);
        }

        public static explicit operator Entity(EntityWrapper wrapper) => wrapper.Entity;
    }
}