using System;
using System.Collections.Generic;

namespace Luffa.Ecs
{
    public interface IEntityFilter
    {
        List<EntityMemory> MatchedEntity { get; }

        bool IsMatch(EntityArchetype archetype);
    }

    public class EntityFilter : IEntityFilter
    {
        public class Builder
        {
            private readonly List<ComponentType> _require;
            private readonly List<ComponentType> _exclude;

            public Builder() { _require = new List<ComponentType>(); _exclude = new List<ComponentType>(); }

            public Builder Require<T>() where T : IComponent { _require.Add(TypeInfo.Get<T>()); return this; }

            public Builder Exclude<T>() where T : IComponent { _exclude.Add(TypeInfo.Get<T>()); return this; }

            public EntityFilter Build() { return new EntityFilter(_require.ToArray(), _exclude.ToArray()); }
        }

        private readonly ComponentType[] _require;
        private readonly ComponentType[] _exclude;
        private readonly List<EntityMemory> _target;

        public IReadOnlyList<ComponentType> Require => _require;
        public IReadOnlyList<ComponentType> Exclude => _exclude;
        public int MatchedCount => _target.Count;
        public List<EntityMemory> MatchedEntity => _target;

        public static EntityFilter FromRequire(params ComponentType[] require)
        {
            return new EntityFilter(require, Array.Empty<ComponentType>());
        }

        public EntityFilter(ComponentType[] require, ComponentType[] exclude)
        {
            _require = require;
            _exclude = exclude;
            _target = new List<EntityMemory>();
        }

        public bool IsMatch(EntityArchetype archetype)
        {
            foreach (var req in _require)
            {
                if (archetype.IsNotExist(req))
                {
                    return false;
                }
            }
            foreach (var exc in _exclude)
            {
                if (archetype.IsExist(exc))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
