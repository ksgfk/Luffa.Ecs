using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Luffa.Ecs
{
    public class EntityArchetype : IEquatable<EntityArchetype>, IReadOnlyList<ComponentType>
    {
        private readonly ComponentType[] _sortedTypes;
        private readonly ComponentType[] _managedTypes;
        private readonly ComponentType[] _unmanagedTypes;
        private readonly int _hash;

        public int Count => _sortedTypes.Length;
        public IReadOnlyList<ComponentType> ManagedTypes => _managedTypes;
        public IReadOnlyList<ComponentType> UnmanagedTypes => _unmanagedTypes;
        public ComponentType this[int index] => _sortedTypes[index];
        public ComponentType[] RawArray => _sortedTypes;

        public static EntityArchetype Get(params ComponentType[] types)
        {
            return new EntityArchetype(types);
        }

        public EntityArchetype(ComponentType[] comArray)
        {
            _sortedTypes = comArray;
            if (!IsValid(_sortedTypes))
            {
                throw new ArgumentException($"duplicate type");
            }
            _hash = Hash(_sortedTypes);
            _unmanagedTypes = _sortedTypes.Where(t => TypeInfo.IsUnmanagedComponent(in t)).ToArray();
            _managedTypes = _sortedTypes.Where(t => !TypeInfo.IsUnmanagedComponent(in t)).ToArray();
        }

        public static int Hash(ComponentType[] types)
        {
            HashCode hash = new HashCode();
            foreach (var type in types)
            {
                hash.Add(type.GetHashCode());
            }
            return hash.ToHashCode();
        }

        public static void Sort(ComponentType[] types)
        {
            Array.Sort(types);
        }

        public static int BinarySearch(ComponentType[] types, ComponentType which)
        {
            return Array.BinarySearch(types, which);
        }

        public static bool IsValid(ComponentType[] types)
        {
            Sort(types);
            for (int i = 1; i < types.Length; i++)
            {
                if (types[i - 1] == types[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int IndexOf(ComponentType which) => BinarySearch(_sortedTypes, which);

        public int IndexInManaged(ComponentType which) => BinarySearch(_managedTypes, which);

        public int IndexInUnmanaged(ComponentType which) => BinarySearch(_unmanagedTypes, which);

        public bool IsExist(ComponentType which) => IndexOf(which) >= 0;

        public bool IsNotExist(ComponentType which) => IndexOf(which) < 0;

        public int IndexOf<T>() where T : IComponent => BinarySearch(_sortedTypes, TypeInfo.Get<T>());

        public int IndexInManaged<T>() where T : IComponent => IndexInManaged(TypeInfo.Get<T>());

        public int IndexInUnmanaged<T>() where T : unmanaged, IComponent => IndexInUnmanaged(TypeInfo.Get<T>());

        public bool IsExist<T>() where T : IComponent => IndexOf(TypeInfo.Get<T>()) >= 0;

        public bool IsNotExist<T>() where T : IComponent => IndexOf(TypeInfo.Get<T>()) < 0;

        public EntityArchetype Attach(ComponentType type)
        {
            int index = IndexOf(type);
            if (index >= 0) { throw new ArgumentException($"duplicate type {type}"); }
            ComponentType[] newType = new ComponentType[Count + 1];
            _sortedTypes.CopyTo(newType, 0);
            newType[^1] = type;
            return new EntityArchetype(newType);
        }

        public EntityArchetype Attach(params ComponentType[] types)
        {
            foreach (var type in types)
            {
                if (IsExist(type)) { throw new ArgumentException($"duplicate type {type}"); }
            }
            ComponentType[] newType = new ComponentType[_sortedTypes.Length + types.Length];
            _sortedTypes.CopyTo(newType, 0);
            types.CopyTo(newType, _sortedTypes.Length);
            return new EntityArchetype(newType);
        }

        public void Attach(ComponentType[] result, ComponentType type)
        {
            if (result.Length < Count + 1)
            {
                throw new ArgumentException("array too short");
            }
            _sortedTypes.CopyTo(result, 0);
            result[^1] = type;
            if (!IsValid(result))
            {
                throw new ArgumentException($"duplicate type");
            }
        }

        public void Attach(ComponentType[] result, ComponentType[] types)
        {
            if (result.Length < Count + types.Length)
            {
                throw new ArgumentException("array too short");
            }
            _sortedTypes.CopyTo(result, 0);
            types.CopyTo(result, _sortedTypes.Length);
            if (!IsValid(result))
            {
                throw new ArgumentException($"duplicate type");
            }
        }

        public EntityArchetype Detach(ComponentType type)
        {
            int index = IndexOf(type);
            if (index < 0) { throw new ArgumentException($"type {type} dose not exist"); }
            ComponentType[] newType = new ComponentType[_sortedTypes.Length - 1];
            Array.Copy(_sortedTypes, newType, index);
            if (index != _sortedTypes.Length - 1)
            {
                Array.Copy(_sortedTypes, index + 1, newType, index, _sortedTypes.Length - index - 1);
            }
            return new EntityArchetype(newType);
        }

        public EntityArchetype Detach(params ComponentType[] types)
        {
            if (_sortedTypes.Length < types.Length)
            {
                throw new ArgumentException($"{nameof(types)} is not a subset of this archetype");
            }
            foreach (var type in types)
            {
                if (IsNotExist(type)) { throw new ArgumentException($"type {type} dose not exist"); }
            }
            ComponentType[] newArr = new ComponentType[_sortedTypes.Length - types.Length];
            for (int i = 0, j = 0; i < _sortedTypes.Length; i++)
            {
                if (Array.IndexOf(types, _sortedTypes[i]) >= 0)
                {
                    continue;
                }
                newArr[j++] = _sortedTypes[i];
            }
            return new EntityArchetype(newArr);
        }

        public void Detach(ComponentType[] result, int index)
        {
            if (result.Length < Count - 1)
            {
                throw new ArgumentException("array too short");
            }
            Array.Copy(_sortedTypes, result, index);
            if (index != _sortedTypes.Length - 1)
            {
                Array.Copy(_sortedTypes, index + 1, result, index, _sortedTypes.Length - index - 1);
            }
            if (!IsValid(result))
            {
                throw new ArgumentException($"duplicate type");
            }
        }

        public bool IsProperSubsetOf(EntityArchetype other)
        {
            if (Count >= other.Count) { return false; }
            foreach (var type in _sortedTypes)
            {
                if (other.IndexOf(type) < 0) { return false; }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) { return false; }
            if (ReferenceEquals(this, obj)) { return true; }
            if (obj.GetType() != GetType()) { return false; }
            return Equals((EntityArchetype)obj);
        }

        public bool Equals(EntityArchetype other)
        {
            if (_hash != other._hash) { return false; }
            if (_sortedTypes.Length != other._sortedTypes.Length) { return false; }
            int count = _sortedTypes.Length;
            for (int i = 0; i < count; i++)
            {
                if (_sortedTypes[i] != other._sortedTypes[i]) { return false; }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public ArraySegment<ComponentType>.Enumerator GetEnumerator() => new ArraySegment<ComponentType>(_sortedTypes).GetEnumerator();

        IEnumerator<ComponentType> IEnumerable<ComponentType>.GetEnumerator() { return GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public ReadOnlySpan<ComponentType> ToSpan()
        {
            return new ReadOnlySpan<ComponentType>(_sortedTypes);
        }
    }
}
