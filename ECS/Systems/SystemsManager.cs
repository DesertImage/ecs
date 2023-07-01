using System;
using System.Collections.Generic;

namespace DesertImage.ECS
{
    public readonly struct SystemsManager : IDisposable
    {
        private readonly EntitiesManager _entitiesManager;
        private readonly GroupsManager _groupsManager;

        private readonly HashSet<ISystem> _systems;

        private readonly List<IExecuteSystem> _executeSystems;
        private readonly List<IEndSystem> _endSystems;

        private readonly Dictionary<ISystem, EntitiesGroup> _systemGroups;

        public SystemsManager(EntitiesManager entitiesManager, GroupsManager groupsManager)
        {
            _entitiesManager = entitiesManager;
            _groupsManager = groupsManager;

            _systems = new HashSet<ISystem>();

            _executeSystems = new List<IExecuteSystem>();
            _endSystems = new List<IEndSystem>();

            _systemGroups = new Dictionary<ISystem, EntitiesGroup>();
        }

        public void Add<T>() where T : class, ISystem, new() => Add(new T());

        private void Add<T>(T system) where T : class, ISystem
        {
#if DEBUG
            if (_systems.Contains(system)) throw new Exception($"System already added {typeof(T)}");
#endif
            system.Activate();

            if (system is IExecuteSystem executeSystem)
            {
                _systemGroups.Add(system, _groupsManager.GetGroup(executeSystem.Matcher));

                _systems.Add(executeSystem);
                _executeSystems.Add(executeSystem);
            }

            if (system is InitSystem initSystem) initSystem.Execute();
            if (system is IEndSystem endSystem) _endSystems.Add(endSystem);
        }

        public void Tick(float delta)
        {
            for (var i = 0; i < _executeSystems.Count; i++)
            {
                var system = _executeSystems[i];
                var group = _systemGroups[system];

                for (uint j = 0; j < group.Entities.Count; j++)
                {
                    system.Execute(_entitiesManager.GetEntityById(group.Entities[j]), delta);
                }
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < _endSystems.Count; i++)
            {
                _endSystems[i].ExecuteEnd();
            }
        }
    }
}