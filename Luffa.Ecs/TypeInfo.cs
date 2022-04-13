using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Luffa.Ecs
{
    public static class TypeInfo
    {
        public class ComponentInfo
        {
            public readonly Type Type;
            public readonly bool IsUnmanaged;
            public readonly int Size;

            public static ComponentInfo Unmanaged(Type type, int size) { return new ComponentInfo(type, true, size); }
            public static ComponentInfo Managed(Type type) { return new ComponentInfo(type, false, 0); }

            private ComponentInfo(Type type, bool isUnmanaged, int size)
            {
                Type = type;
                IsUnmanaged = isUnmanaged;
                Size = size;
            }
        }

        internal static readonly List<ComponentInfo> ComInfo; //缓存组件是不是非托管类型

        static TypeInfo() { ComInfo = new List<ComponentInfo>(); }

        public static bool IsUnmanagedComponent(in ComponentType type)
        {
            return ComInfo[type.Id].IsUnmanaged;
        }

        public static int SizeOfComponent(in ComponentType type)
        {
            if (!ComInfo[type.Id].IsUnmanaged) { throw new InvalidOperationException(); }
            return ComInfo[type.Id].Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentType Get<T>() where T : IComponent
        {
            return new ComponentType(TypeInfo<T>.Id);
        }
    }

    // 利用 C# 泛型机制自动为组件分配唯一id
    // 由于id自增, 所以很容易缓存是不是非托管类型
    public class TypeInfo<T> where T : IComponent
    {
        public static Type Type { get; }
        public static int Id { get; }
        public static bool IsUnmanaged => !RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        static TypeInfo()
        {
            Type = typeof(T);
            Id = TypeInfo.ComInfo.Count;
            if (IsUnmanaged)
            {
                TypeInfo.ComInfo.Add(TypeInfo.ComponentInfo.Unmanaged(Type, Unsafe.SizeOf<T>()));
            }
            else
            {
                TypeInfo.ComInfo.Add(TypeInfo.ComponentInfo.Managed(Type));
            }
        }
    }

    [DebuggerDisplay("Id = {Id}, Type = {Type}")]
    public readonly struct ComponentType : IEquatable<ComponentType>, IComparable<ComponentType>
    {
        public Type Type => TypeInfo.ComInfo[Id].Type;
        public readonly int Id;

        internal ComponentType(int id)
        {
            Id = id;
        }

        public bool Equals(ComponentType other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            if (!(obj is ComponentType c)) { return false; }
            return Equals(c);
        }

        public override int GetHashCode() => HashCode.Combine(Id);

        public int CompareTo(ComponentType other)
        {
            int id = Id.CompareTo(other.Id);
            if (id == 0) { return 0; }
            int typeName = string.Compare(Type.FullName, other.Type.FullName);
            return typeName;
        }

        public static bool operator ==(ComponentType lhs, ComponentType rhs)
        {
            return lhs.Id == rhs.Id;
        }

        public static bool operator !=(ComponentType lhs, ComponentType rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return $"{{Id: {Id}, Type: {Type}}}";
        }
    }
}
