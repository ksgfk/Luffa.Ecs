using System;
using System.Collections.Generic;

namespace Luffa.Ecs
{
    public class World
    {
        private class EntityInfo
        {
            public EntityMemory? Memory;
            public int InnerId;
            public int Version;
            public bool IsMarkDelete;
        }

        private class FilterMatch
        {
            public IEntityFilter Filter;
            public List<EntityArchetype> Matched;

            public FilterMatch(IEntityFilter filter)
            {
                Filter = filter ?? throw new ArgumentNullException(nameof(filter));
                Matched = new List<EntityArchetype>();
            }

            public void Deconstruct(out IEntityFilter filter, out List<EntityArchetype> matched)
            {
                filter = Filter;
                matched = Matched;
            }
        }

        private readonly Dictionary<EntityArchetype, EntityMemory> _component;

        private readonly List<EntityInfo> _entity;
        private readonly Queue<int> _entityEmptySlot;

        private readonly List<ISystem> _system;
        private readonly List<FilterMatch> _match;

        private readonly CmdBuffer _mainCmdBuffer;

        public int EntityCount => _entity.Count - _entityEmptySlot.Count;

        public World()
        {
            _component = new Dictionary<EntityArchetype, EntityMemory>();
            _entity = new List<EntityInfo>();
            _entityEmptySlot = new Queue<int>();
            _system = new List<ISystem>();
            _match = new List<FilterMatch>();
            _mainCmdBuffer = new CmdBuffer();
        }

        public void AddArchetype(EntityArchetype archetype)
        {
            GetOrCreateMemory(archetype);
        }

        public EntityArchetype Attach(EntityArchetype archetype, ComponentType type)
        {
            EntityArchetype find = archetype.Attach(type);
            GetOrCreateMemory(find);
            return find;
        }

        public EntityArchetype Detach(EntityArchetype archetype, ComponentType type)
        {
            EntityArchetype find = archetype.Detach(type);
            GetOrCreateMemory(find);
            return find;
        }

        private EntityMemory GetOrCreateMemory(EntityArchetype archetype)
        {
            if (!_component.TryGetValue(archetype, out EntityMemory memory))
            {
                memory = new EntityMemory(archetype);
                _component.Add(archetype, memory);
                // 对所有filter查询是否匹配
                foreach (var (filter, matched) in _match)
                {
                    if (filter.IsMatch(archetype))
                    {
                        matched.Add(archetype);
                        filter.MatchedArchetype.Add(archetype);
                    }
                }
            }
            return memory;
        }

        private int RequestUniqueId()
        {
            int id;
            if (_entityEmptySlot.Count == 0)
            {
                id = _entity.Count;
                _entity.Add(new EntityInfo());
            }
            else
            {
                id = _entityEmptySlot.Dequeue();
            }
            return id;
        }

        public EntityHandle CreateEntity(EntityArchetype archetype)
        {
            EntityMemory memory = GetOrCreateMemory(archetype);
            int uniqueId = RequestUniqueId();
            int innerId = memory.Allocate(uniqueId);
            EntityInfo entity = _entity[uniqueId];
            entity.Memory = memory;
            entity.InnerId = innerId;
            entity.Version++;
            return new EntityHandle(uniqueId, entity.Version);
        }

        public bool IsValidEntity(EntityHandle entity)
        {
            if (entity.Index < 0 || entity.Index >= _entity.Count) { return false; }
            EntityInfo info = _entity[entity.Index];
            return entity.Version == info.Version && info.Memory != null;
        }

        private void DestroyEntity(int index)
        {
            int destroyedIndex = index;
            EntityInfo destroyed = _entity[destroyedIndex];
            EntityMemory memory = destroyed.Memory!;
            int movedIndex = memory.Release(destroyed.InnerId);
            if (movedIndex != destroyedIndex) //被移动的实体不是被删除的实体
            {
                EntityInfo moved = _entity[movedIndex];
                moved.InnerId = destroyed.InnerId; //储存中会把被末尾的组件交换到被删除的组件位置上
                moved.Version++;
            }
            destroyed.Memory = null!;
            destroyed.InnerId = -1;
            destroyed.Version++;
            _entityEmptySlot.Enqueue(destroyedIndex);
        }

        public void DestroyEntity(EntityHandle entity)
        {
            if (!IsValidEntity(entity)) { throw new InvalidOperationException("invalid entity"); }
            DestroyEntity(entity.Index);
        }

        public ref T GetUnmanagedComponent<T>(EntityHandle entity) where T : unmanaged, IComponent
        {
            if (!IsValidEntity(entity)) { throw new InvalidOperationException("invalid entity"); }
            EntityInfo info = _entity[entity.Index];
            return ref info.Memory!.GetUnmanagedComponent<T>(info.InnerId);
        }

        public ref T GetManagedComponent<T>(EntityHandle entity) where T : IComponent
        {
            if (!IsValidEntity(entity)) { throw new InvalidOperationException("invalid entity"); }
            EntityInfo info = _entity[entity.Index];
            return ref info.Memory!.GetManagedComponent<T>(info.InnerId);
        }

        public EntityHandle GetEntityUnsafe(int index)
        {
            return new EntityHandle(index, _entity[index].Version);
        }

        public EntityMemory GetEntityMemoryUnsafe(EntityHandle entity)
        {
            if (!IsValidEntity(entity)) { throw new InvalidOperationException("invalid entity"); }
            return _entity[entity.Index].Memory!;
        }

        public EntityMemory GetEntityMemory(EntityArchetype archetype)
        {
            return _component[archetype];
        }

        public bool TryGetEntityMemory(EntityArchetype archetype, out EntityMemory memory)
        {
            bool isFind = _component.TryGetValue(archetype, out memory);
            return isFind;
        }

        public bool HasComponent(EntityHandle entity, ComponentType type)
        {
            if (!IsValidEntity(entity)) { throw new InvalidOperationException("invalid entity"); }
            return _entity[entity.Index].Memory!.Archetype.IndexOf(type) >= 0;
        }

        public bool HasComponent<T>(EntityHandle entity) where T : IComponent
        {
            return HasComponent(entity, TypeInfo.Get<T>());
        }

        private void MoveComponent(int index, EntityMemory newMemory)
        {
            EntityInfo info = _entity[index];
            EntityMemory oldMemory = info.Memory!;
            int oldInnerId = info.InnerId;
            var (newIndex, movedUniqueId) = oldMemory.MoveTo(info.InnerId, newMemory);
            info.Memory = newMemory;
            info.InnerId = newIndex;
            if (movedUniqueId != index)
            {
                EntityInfo moved = _entity[movedUniqueId];
                moved.InnerId = oldInnerId;
            }
        }

        private EntityMemory InternalAddComponent(int index, ComponentType type)
        {
            EntityArchetype newArch = Attach(_entity[index].Memory!.Archetype, type);
            EntityMemory newMemory = GetOrCreateMemory(newArch);
            MoveComponent(index, newMemory);
            return newMemory;
        }

        private EntityMemory InternalAddComponent(EntityHandle entity, ComponentType type)
        {
            if (!IsValidEntity(entity)) { throw new InvalidOperationException("invalid entity"); }
            return InternalAddComponent(entity.Index, type);
        }

        public void AddComponent(EntityHandle entity, ComponentType type)
        {
            InternalAddComponent(entity, type);
        }

        public ref T AddUnmanagedComponent<T>(EntityHandle entity) where T : unmanaged, IComponent
        {
            EntityMemory newMemory = InternalAddComponent(entity, TypeInfo.Get<T>());
            EntityInfo info = _entity[entity.Index];
            return ref newMemory.GetUnmanagedComponent<T>(info.InnerId);
        }

        public ref T AddManagedComponent<T>(EntityHandle entity) where T : IComponent
        {
            EntityMemory newMemory = InternalAddComponent(entity, TypeInfo.Get<T>());
            EntityInfo info = _entity[entity.Index];
            return ref newMemory.GetManagedComponent<T>(info.InnerId);
        }

        private EntityMemory InternalRemoveComponent(int index, ComponentType type)
        {
            EntityArchetype newArch = Detach(_entity[index].Memory!.Archetype, type);
            EntityMemory newMemory = GetOrCreateMemory(newArch);
            MoveComponent(index, newMemory);
            return newMemory;
        }

        private EntityMemory InternalRemoveComponent(EntityHandle entity, ComponentType type)
        {
            if (!IsValidEntity(entity)) { throw new InvalidOperationException("invalid entity"); }
            return InternalRemoveComponent(entity.Index, type);
        }

        public void RemoveComponent(EntityHandle entity, ComponentType type)
        {
            InternalRemoveComponent(entity, type);
        }

        public void RemoveComponent<T>(EntityHandle entity) where T : IComponent
        {
            InternalRemoveComponent(entity, TypeInfo.Get<T>());
        }

        public void AddFilter(IEntityFilter filter)
        {
            var filterMatch = new FilterMatch(filter);
            var matched = filterMatch.Matched;
            _match.Add(filterMatch);
            // 查询匹配的原型
            foreach (var arch in _component.Keys)
            {
                if (filter.IsMatch(arch))
                {
                    matched.Add(arch);
                    filter.MatchedArchetype.Add(arch);
                }
            }
        }

        public void AddSystem<T>() where T : ISystem, new()
        {
            var newSystem = new T();
            _system.Add(newSystem);
            AddFilter(newSystem.Filter);
        }

        private void MarkCmdDestroyEntity(CmdBuffer buffer)
        {
            foreach (CmdEntry cmd in _mainCmdBuffer.Buffer)
            {
                if (cmd.Cmd == WorldCmd.Destroy)
                {
                    if (IsValidEntity(cmd.Target))
                    {
                        _entity[cmd.Target.Index].IsMarkDelete = true;
                    }
                    else
                    {
                        Console.Error.WriteLine($"{cmd.Target} is not a valid entity. ignore");
                    }
                }
            }
        }

        private void ExecuteCmdBufferDestroyEntity()
        {
            for (int i = 0; i < _entity.Count; i++)
            {
                if (_entity[i].IsMarkDelete)
                {
                    DestroyEntity(i);
                    _entity[i].IsMarkDelete = false;
                }
            }
        }

        private void ExecuteCmdBuffer(CmdBuffer buffer)
        {
            foreach (CmdEntry cmd in _mainCmdBuffer.Buffer)
            {
                switch (cmd.Cmd)
                {
                    case WorldCmd.Create:
                        CreateEntity(cmd.CreateType!);
                        break;
                    case WorldCmd.AddComponent:
                        InternalAddComponent(cmd.Target.Index, cmd.AddType);
                        break;
                    case WorldCmd.RemoveComponent:
                        InternalRemoveComponent(cmd.Target.Index, cmd.RemoveType);
                        break;
                    default:
                        break;
                }
            }
            _mainCmdBuffer.Clear();
        }

        public void OnUpdate()
        {
            foreach (var system in _system)
            {
                system.OnUpdate(this, _mainCmdBuffer);
            }

            MarkCmdDestroyEntity(_mainCmdBuffer);
            ExecuteCmdBufferDestroyEntity();
            ExecuteCmdBuffer(_mainCmdBuffer);
        }

        public void FilterEntity(IEntityFilter filter)
        {
            foreach (var arch in _component.Keys)
            {
                if (filter.IsMatch(arch))
                {
                    filter.MatchedArchetype.Add(arch);
                }
            }
        }
    }
}
