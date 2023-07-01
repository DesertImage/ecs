using System.Collections.Generic;
using Unity.Collections;

namespace DesertImage.ECS
{
    public struct GroupsManager
    {
        private readonly NativeHashMap<uint, NativeList<uint>> _entityGroups;
        private readonly NativeHashMap<uint, int> _matcherGroups;

        private readonly NativeHashMap<uint, Matcher> _groupMatchers;
        private readonly NativeHashMap<uint, NativeList<uint>> _componentGroups;
        private readonly NativeHashMap<uint, NativeList<uint>> _noneOfComponentGroups;

        private readonly NativeList<EntitiesGroup> _groups;

        private readonly EntitiesManager _entitiesManager;

        private static uint _groupsIdCounter;

        public GroupsManager(EntitiesManager entitiesManager)
        {
            _entitiesManager = entitiesManager;

            _entityGroups = new NativeHashMap<uint, NativeList<uint>>();
            _matcherGroups = new NativeHashMap<uint, uint>();

            _groupMatchers = new NativeHashMap<uint, Matcher>();
            _componentGroups = new NativeHashMap<uint, NativeList<uint>>();
            _noneOfComponentGroups = new NativeHashMap<uint, NativeList<uint>>();

            _groups = new NativeList<EntitiesGroup>(20);

            _groupsIdCounter = 0;
        }

        public readonly EntitiesGroup GetGroup(Matcher matcher)
        {
            return _matcherGroups.TryGetValue(matcher.Id, out var group) ? _groups[group - 1] : GetNewGroup(matcher);
        }

        private readonly EntitiesGroup GetNewGroup()
        {
            var newGroup = new EntitiesGroup(++_groupsIdCounter);
            _groups.Add(newGroup);
            return newGroup;
        }

        private readonly EntitiesGroup GetNewGroup(Matcher matcher)
        {
            var newGroup = GetNewGroup();

            var newGroupId = newGroup.Id;

            _groupMatchers.Add(newGroupId, matcher);
            _matcherGroups.Add(matcher.Id, newGroupId);

            foreach (var componentId in matcher.Components)
            {
                if (_componentGroups.TryGetValue(componentId, out var groups))
                {
                    groups.Add(newGroupId);
                }
                else
                {
                    _componentGroups.Add(componentId, new List<uint> { newGroupId });
                }
            }

            foreach (var componentId in matcher.NoneOfComponents)
            {
                if (_noneOfComponentGroups.TryGetValue(componentId, out var groups))
                {
                    groups.Add(newGroupId);
                }
                else
                {
                    _noneOfComponentGroups.Add(componentId, new List<uint> { newGroupId });
                }
            }

            return newGroup;
        }

        private void AddToGroup(EntitiesGroup group, uint entityId)
        {
            var groupId = group.Id;

            group.Add(entityId);

            if (_entityGroups.TryGetValue(entityId, out var groupsList))
            {
                groupsList.Add(groupId);
            }
            else
            {
                _entityGroups.Add(entityId, new List<uint> { groupId });
            }
        }

        private void RemoveFromGroup(EntitiesGroup group, uint entityId)
        {
            group.Remove(entityId);
            _entityGroups[entityId].Remove(group.Id);
        }

        public void OnEntityCreated(uint entityId) => _entityGroups.Add(entityId, new List<uint>());

        public void OnEntityComponentAdded(uint entityId, uint componentId)
        {
            var groups = _entityGroups[entityId];

            for (var i = groups.Count - 1; i >= 0; i--)
            {
                var groupId = groups[i];

                var matcher = _groupMatchers[groupId];
                var components = _entitiesManager.GetComponents(entityId);

                if (matcher.Check(components)) continue;

                RemoveFromGroup(_groups[groupId], entityId);
            }

            if (_componentGroups.TryGetValue(componentId, out var componentGroups))
            {
                ValidateEntityAdd(entityId, componentGroups);
            }

            if (_noneOfComponentGroups.TryGetValue(componentId, out componentGroups))
            {
                ValidateEntityRemove(entityId, componentGroups);
            }
        }

        private void ValidateEntityAdd(uint entityId, IReadOnlyList<uint> groups)
        {
            for (var i = groups.Count - 1; i >= 0; i--)
            {
                var groupId = groups[i];
                var group = _groups[groupId];

                if (group.Contains(entityId)) continue;

                var matcher = _groupMatchers[groupId];
                var components = _entitiesManager.GetComponents(entityId);

                if (!matcher.Check(components)) continue;

                AddToGroup(group, entityId);
            }
        }

        //TODO:refactor
        private void ValidateEntityRemove(uint entityId, IReadOnlyList<uint> groups)
        {
            for (var i = groups.Count - 1; i >= 0; i--)
            {
                var groupId = groups[i];
                var group = _groups[groupId];

                if (!group.Contains(entityId)) continue;

                var matcher = _groupMatchers[groupId];
                var components = _entitiesManager.GetComponents(entityId);

                if (matcher.Check(components)) continue;

                RemoveFromGroup(group, entityId);
            }
        }

        public void OnEntityComponentRemoved(uint entityId, uint componentId)
        {
            var groups = _entityGroups[entityId];

            for (var i = groups.Count - 1; i >= 0; i--)
            {
                var groupId = groups[i];

                var matcher = _groupMatchers[groupId];
                var components = _entitiesManager.GetComponents(entityId);

                if (matcher.Check(components)) continue;

                RemoveFromGroup(_groups[groupId], entityId);
            }

            if (!_noneOfComponentGroups.TryGetValue(componentId, out var componentGroups)) return;

            for (var i = componentGroups.Count - 1; i >= 0; i--)
            {
                var groupId = componentGroups[i];
                var group = _groups[groupId];

                var matcher = _groupMatchers[groupId];
                var components = _entitiesManager.GetComponents(entityId);

                if (!matcher.Check(components)) continue;

                AddToGroup(group, entityId);
            }
        }
    }
}