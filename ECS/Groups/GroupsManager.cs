using System.Collections.Generic;

namespace DesertImage.ECS
{
    public struct GroupsManager
    {
        private readonly Dictionary<int, List<int>> _entityGroups;
        private readonly Dictionary<int, int> _matcherGroups;

        private readonly Dictionary<int, Matcher> _groupMatchers;
        private readonly Dictionary<int, List<int>> _componentGroups;
        private readonly Dictionary<int, List<int>> _noneOfComponentGroups;

        private readonly List<EntitiesGroup> _groups;

        private readonly EntitiesManager _entitiesManager;

        private static int _groupsIdCounter = -1;

        public GroupsManager(EntitiesManager entitiesManager)
        {
            _entitiesManager = entitiesManager;

            _entityGroups = new Dictionary<int, List<int>>();
            _matcherGroups = new Dictionary<int, int>();

            _groupMatchers = new Dictionary<int, Matcher>();
            _componentGroups = new Dictionary<int, List<int>>();
            _noneOfComponentGroups = new Dictionary<int, List<int>>();

            _groups = new List<EntitiesGroup>(20);

            _groupsIdCounter = -1;
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
                    _componentGroups.Add(componentId, new List<int> { newGroupId });
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
                    _noneOfComponentGroups.Add(componentId, new List<int> { newGroupId });
                }
            }

            return newGroup;
        }

        private void AddToGroup(EntitiesGroup group, int entityId)
        {
            var groupId = group.Id;

            group.Add(entityId);

            if (_entityGroups.TryGetValue(entityId, out var groupsList))
            {
                groupsList.Add(groupId);
            }
            else
            {
                _entityGroups.Add(entityId, new List<int> { groupId });
            }
        }

        private void RemoveFromGroup(EntitiesGroup group, int entityId)
        {
            group.Remove(entityId);
            _entityGroups[entityId].Remove(group.Id);
        }

        public void OnEntityCreated(int entityId) => _entityGroups.Add(entityId, new List<int>());

        public void OnEntityComponentAdded(int entityId, int componentId)
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

        private void ValidateEntityAdd(int entityId, IReadOnlyList<int> groups)
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
        private void ValidateEntityRemove(int entityId, IReadOnlyList<int> groups)
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

        public void OnEntityComponentRemoved(int entityId, int componentId)
        {
            var groups = _entityGroups[entityId];

            var isAlive = _entitiesManager.IsAlive(entityId);

            if (!isAlive)
            {
                for (var i = groups.Count - 1; i >= 0; i--)
                {
                    var groupId = groups[i];
                    RemoveFromGroup(_groups[groupId], entityId);
                }
            }
            else
            {
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
}