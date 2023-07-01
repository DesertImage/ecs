using System;
using Unity.Collections;

namespace DesertImage.ECS
{
    public struct World : IDisposable
    {
        public uint Id { get; private set; }

        public Entity SharedEntity { get; }

        internal WorldState State { get; private set; }

        private EntitiesManager EntitiesManager { get; }
        private GroupsManager GroupsManager { get; }
        private SystemsManager SystemsManager { get; }

        public World(uint id)
        {
            Id = id;

            State = new WorldState
            (
                new NativeHashMap<uint, Entity>(ECSSettings.EntitiesCapacity, Allocator.Persistent),
                new NativeHashMap<uint, NativeHashSet<uint>>(100, Allocator.Persistent)
            );

            EntitiesManager = new EntitiesManager(State);
            GroupsManager = new GroupsManager(EntitiesManager);
            SystemsManager = new SystemsManager(EntitiesManager, GroupsManager);

            SharedEntity = EntitiesManager.GetNewEntity();
            GroupsManager.OnEntityCreated(SharedEntity.Id);
        }

        public void ReplaceComponent<T>(uint entityId, T component) where T : struct
        {
            EntitiesManager.ReplaceComponent(entityId, component);
            GroupsManager.OnEntityComponentAdded(entityId, ComponentTools.GetComponentId<T>());
        }

        public void RemoveComponent<T>(uint entityId) where T : struct
        {
            EntitiesManager.RemoveComponent<T>(entityId);
            GroupsManager.OnEntityComponentRemoved(entityId, ComponentTools.GetComponentId<T>());
        }

        public bool HasComponent<T>(uint entityId) where T : struct => EntitiesManager.HasComponent<T>(entityId);

        public ref T GetComponent<T>(uint entityId) where T : struct => ref EntitiesManager.GetComponent<T>(entityId);

        public void Add<T>() where T : class, ISystem, new() => SystemsManager.Add<T>();

        public Entity GetEntityById(uint id) => EntitiesManager.GetEntityById(id);

        public Entity GetNewEntity()
        {
            var newEntity = EntitiesManager.GetNewEntity();
            GroupsManager.OnEntityCreated(newEntity.Id);
            return newEntity;
        }

        public NativeHashSet<uint> GetEntityComponents(uint id) => EntitiesManager.GetComponents(id);
        public bool IsEntityAlive(uint entityId) => EntitiesManager.IsAlive(entityId);
        public void DestroyEntity(uint entityId) => EntitiesManager.DestroyEntity(entityId);

        public EntitiesGroup GetGroup(Matcher matcher) => GroupsManager.GetGroup(matcher);

        public void Tick(float deltaTime) => SystemsManager.Tick(deltaTime);

        public void Dispose()
        {
            State.Dispose();
            EntitiesManager.Dispose();
            SystemsManager.Dispose();
        }
    }
}