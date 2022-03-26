using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Luffa.Ecs
{
    public static class TypeInfo
    {
        public readonly struct ComponentInfo
        {
            public readonly bool IsUnmanaged;
            public readonly int Size;

            public static ComponentInfo Unmanaged(int size) { return new ComponentInfo(true, size); }
            public static ComponentInfo Managed() { return new ComponentInfo(false, 0); }

            private ComponentInfo(bool isUnmanaged, int size)
            {
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

        public static ComponentType Get<T>() where T : IComponent
        {
            return new ComponentType(TypeInfo<T>.Type, TypeInfo<T>.Id);
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
                TypeInfo.ComInfo.Add(TypeInfo.ComponentInfo.Unmanaged(Unsafe.SizeOf<T>()));
            }
            else
            {
                TypeInfo.ComInfo.Add(TypeInfo.ComponentInfo.Managed());
            }
        }
    }

    [DebuggerDisplay("Id = {Id}, Type = {Type}")]
    public readonly struct ComponentType : IEquatable<ComponentType>, IComparable<ComponentType>
    {
        public Type Type { get; }
        public int Id { get; }

        internal ComponentType(Type type, int id)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Id = id;
        }

        public bool Equals(ComponentType other)
        {
            return Id == other.Id && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            if (!(obj is ComponentType c)) { return false; }
            return Equals(c);
        }

        public override int GetHashCode() => HashCode.Combine(Type, Id);

        public int CompareTo(ComponentType other)
        {
            int id = Id.CompareTo(other.Id);
            if (id == 0) { return 0; }
            int typeName = string.Compare(Type.FullName, other.Type.FullName);
            return typeName;
        }

        public static bool operator ==(ComponentType lhs, ComponentType rhs)
        {
            return lhs.Equals(rhs);
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
