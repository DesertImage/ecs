using System;
using System.Collections.Generic;
using Unity.Collections;

namespace DesertImage.ECS
{
    public struct EntitiesManager : IDisposable
    {
        private readonly Stack<Entity> _entitiesPool;

        private ComponentsStorageBase[] _componentsStorages;

        private static uint _idCounter;

        private readonly Stack<NativeHashSet<uint>> _componentsPool;

        private readonly WorldState _state;

        public EntitiesManager(WorldState state)
        {
            _state = state;

            _componentsStorages = new ComponentsStorageBase[ECSSettings.ComponentsDenseCapacity];
            _componentsPool = new Stack<NativeHashSet<uint>>();

            _entitiesPool = new Stack<Entity>();
        }

        public readonly Entity GetEntityById(uint id)
        {
            return _state.Entities.TryGetValue(id, out var entity) ? entity : GetNewEntity();
        }

        public readonly Entity GetNewEntity()
        {
            var newEntity = _entitiesPool.Count > 0 ? _entitiesPool.Pop() : new Entity(++_idCounter);

            var id = newEntity.Id;

            _state.Entities.Add(id, newEntity);
            _state.Components.Add
            (
                id,
                _componentsPool.Count > 0 ? _componentsPool.Pop() : new NativeHashSet<uint>(50, Allocator.Persistent)
            );

            return newEntity;
        }

        public readonly NativeHashSet<uint> GetComponents(uint id) => _state.Components[id];

        public void ReplaceComponent<T>(uint entityId, T component) where T : struct
        {
#if DEBUG
            if (!IsAlive(entityId)) throw new Exception($"Entity {entityId} is not alive!");
#endif
            var componentId = ComponentTools.GetComponentId<T>();

            if (componentId >= _componentsStorages.Length)
            {
                Array.Resize(ref _componentsStorages, componentId << 1);
            }

            var storage = GetStorage<T>(componentId);

            storage.Data.Add((int)entityId, component);
            _state.Components[entityId].Add(componentId);
        }

        public void RemoveComponent<T>(uint entityId) where T : struct
        {
#if DEBUG
            if (!IsAlive(entityId)) throw new Exception($"Entity {entityId} is not alive!");
            if (!HasComponent<T>(entityId)) throw new Exception($"Entity {entityId} has not {typeof(T).Name}");
#else
            if (!HasComponent<T>(entityId)) return;
#endif
            var componentId = ComponentTools.GetComponentId<T>();

            var storage = GetStorage<T>(componentId);

            storage.Data.Remove((int)entityId);
            _state.Components[entityId].Remove(componentId);
        }

        public bool HasComponent<T>(uint entityId) where T : struct
        {
#if DEBUG
            if (!IsAlive(entityId)) throw new Exception($"Entity {entityId} is not alive!");
#endif
            var componentId = ComponentTools.GetComponentId<T>();

            if (componentId >= _componentsStorages.Length)
            {
#if DEBUG
                throw new Exception("out of ComponentStorages");
#else
                return false;
#endif
            }

            var storage = GetStorage<T>(componentId);

            return storage.Data.Contains(entityId);
        }

        public ref T GetComponent<T>(uint entityId) where T : struct
        {
#if DEBUG
            if (!IsAlive(entityId)) throw new Exception($"Entity {entityId} is not alive!");
            if (!HasComponent<T>(entityId)) throw new Exception($"Entity {entityId} has not {typeof(T).Name}");
#endif
            var componentId = ComponentTools.GetComponentId<T>();
            var storage = (ComponentsStorage<T>)_componentsStorages[componentId];

            return ref storage.Data.Get(entityId);
        }

        public bool IsAlive(uint entityId) => _state.Entities.ContainsKey(entityId);

        public void DestroyEntity(uint entityId)
        {
            var components = GetComponents(entityId);
            components.Clear();

            var entity = GetEntityById(entityId);

            _state.Components.Remove(entityId);
            _state.Entities.Remove(entityId);

            _componentsPool.Push(components);

            _entitiesPool.Push(entity);
        }

        private ComponentsStorage<T> GetStorage<T>(uint componentId)
        {
            var storage = (ComponentsStorage<T>)_componentsStorages[componentId];

            if (storage != null) return storage;

            var newInstance = new ComponentsStorage<T>
            (
                ECSSettings.ComponentsDenseCapacity,
                ECSSettings.ComponentsSparseCapacity
            );

            _componentsStorages[componentId] = newInstance;
            storage = newInstance;

            return storage;
        }

        public void Dispose()
        {
            while (_componentsPool.Count > 0)
            {
                _componentsPool.Pop().Dispose();
            }
        }
    }
}