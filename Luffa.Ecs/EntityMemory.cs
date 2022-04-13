using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Luffa.Ecs
{
    /// <summary>
    /// WARNING | WARNING | WARNING<br/>
    /// 该Enumerator没有任何版本检查<br/>
    /// 如果在遍历实体期间, 构造该查看器的<see cref="EntityMemory"/>出现任何结构变化
    /// (也就是调用<see cref="EntityMemory.Allocate(int)"/>或<see cref="EntityMemory.Release(int)"/>)
    /// 都极有可能Enumerator失效<br/>
    /// WARNING | WARNING | WARNING
    /// </summary>
    public readonly ref struct ComponentViewer
    {
        private readonly ComponentChunk _chunk;
        internal ComponentViewer(ComponentChunk chunk) { _chunk = chunk; }

        public Enumerator GetEnumerator() { return new Enumerator(_chunk); }

        public ref struct Indexer
        {
            internal byte[] _data;
            internal int _inChunkIndex;
            /// <summary>
            /// 实体在该<see cref="EntityMemory"/>内的索引
            /// </summary>
            public int EntityIndex;
        }

        public ref struct Enumerator //有没有更好的写法
        {
            private readonly List<Chunk> _chunk;
            private readonly int _entityCountPerChunk;
            private readonly int _entityCount;
            private int _chunkIndex;
            private Indexer _indexer;

            public Indexer Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _indexer;
            }

            internal Enumerator(ComponentChunk chunk)
            {
                _chunk = chunk.Chunks;
                _entityCountPerChunk = chunk._entityCountPerChunk;
                _entityCount = chunk.Count;
                _chunkIndex = 0;
                _indexer._data = _chunk[0].Data; //Chunk默认一定有一个
                _indexer._inChunkIndex = -1;
                _indexer.EntityIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                ref var idx = ref _indexer;
                idx._inChunkIndex++;
                idx.EntityIndex++;
                if (idx._inChunkIndex < _entityCountPerChunk)
                {
                    return idx.EntityIndex < _entityCount;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                List<Chunk> local = _chunk;
                _chunkIndex++;
                if (_chunkIndex < local.Count)
                {
                    _indexer._inChunkIndex = 0;
                    _indexer._data = local[_chunkIndex].Data;
                    return true;
                }
                _chunkIndex = _chunk.Count + 1;
                _indexer = default;
                return false;
            }
        }
    }

    public class EntityMemory
    {
        public struct Info : IComponent
        {
            public int UniqueId;
        }

        public const int DefaultSize = 16384; //16KB
        public const int DefaultCache = 4;

        private readonly EntityArchetype _archetype;
        private readonly ComponentChunk _unmanaged;
        private readonly IComponentList[] _managed;
        private readonly int _unComCnt;
        private readonly int _maComCnt;
        private int _count;

        public int Count => _count;
        public EntityArchetype Archetype => _archetype;

        public EntityMemory(EntityArchetype archetype)
        {
            _archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            _unComCnt = archetype.UnmanagedTypes.Count;
            _maComCnt = archetype.ManagedTypes.Count;

            ComponentType[] layout = _archetype.UnmanagedTypes.Append(TypeInfo.Get<Info>()).ToArray();
            _unmanaged = new ComponentChunk(layout, DefaultSize, DefaultCache);

            _managed = new IComponentList[_archetype.ManagedTypes.Count];
            for (int i = 0; i < _archetype.ManagedTypes.Count; i++)
            {
                ComponentType managed = _archetype.ManagedTypes[i];
                _managed[i] = (IComponentList)Activator.CreateInstance(typeof(ComponentList<>).MakeGenericType(managed.Type));
            }
        }

        private int GetUniqueId(int index)
        {
            return _unmanaged.Get<Info>(index, _unComCnt).UniqueId;
        }

        public int Allocate(int uniqueId)
        {
            int index = _count;
            int checkUn = _unmanaged.Allocate();
            Debug.Assert(index == checkUn);
            _unmanaged.Get<Info>(index, _unComCnt).UniqueId = uniqueId;
            for (int i = 0; i < _maComCnt; i++)
            {
                int checkMa = _managed[i].Allocate();
                Debug.Assert(index == checkMa);
            }
            _count++;
            return index;
        }

        public int Release(int index)
        {
            Debug.Assert(index >= 0 && index < _count);
            int uniqueId = GetUniqueId(_count - 1);
            _unmanaged.Release(index);
            Debug.Assert(_count - 1 == _unmanaged.Count);
            for (int i = 0; i < _maComCnt; i++)
            {
                _managed[i].Release(index);
                Debug.Assert(_count - 1 == _managed[i].Count);
            }
            _count--;
            return uniqueId;
        }

        public (int, int) MoveTo(int index, EntityMemory target)
        {
            Debug.Assert(index >= 0 && index < _count);
            int uniqueId = GetUniqueId(index);
            int oldIndex = index;
            int newIndex = target.Allocate(uniqueId); //先在目标内存分配空间
            for (int srcComIdx = 0; srcComIdx < _unComCnt; srcComIdx++) //复制 unmanaged 组件
            {
                int targetComIdx = target.Archetype.IndexInUnmanaged(_archetype.UnmanagedTypes[srcComIdx]);
                if (targetComIdx < 0) { continue; } //目标没有相同类型, 直接丢弃完事233
                Span<byte> oldCom = _unmanaged.Get(oldIndex, srcComIdx);
                Span<byte> newCom = target._unmanaged.Get(newIndex, targetComIdx);
                Debug.Assert(oldCom.Length == newCom.Length);
                oldCom.CopyTo(newCom);
            }
            for (int srcComIdx = 0; srcComIdx < _maComCnt; srcComIdx++) //复制 managed 组件
            {
                int targetComIdx = target.Archetype.IndexInManaged(_archetype.ManagedTypes[srcComIdx]);
                if (targetComIdx < 0) { continue; } //丢弃丢弃
                _managed[srcComIdx].CopyElementTo(oldIndex, target._managed[targetComIdx], newIndex);
            }
            int movedUniqueId = Release(oldIndex); //最后在本内存删掉实体
            return (newIndex, movedUniqueId);
        }

        public ComponentViewer GetViewer()
        {
            return new ComponentViewer(_unmanaged);
        }

        public UnmanagedComponentLocator<T> GetUnmanagedComponentLocator<T>() where T : unmanaged, IComponent
        {
            int comIndex = _archetype.IndexInUnmanaged(TypeInfo.Get<T>());
            Debug.Assert(typeof(T) == _archetype.UnmanagedTypes[comIndex].Type);
            return new UnmanagedComponentLocator<T>(_unmanaged, comIndex);
        }

        public UnmanagedComponentLocator<Info> GetEntityLocator()
        {
            return new UnmanagedComponentLocator<Info>(_unmanaged, _unComCnt);
        }

        /// <summary>
        /// 不安全的方法! 你必须保证在使用引用期间该<see cref="EntityMemory"/>没有任何结构变化, 否则可能出现悬垂引用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetUnmanagedComponent<T>(int index) where T : unmanaged, IComponent
        {
            int comIndex = _archetype.IndexInUnmanaged(TypeInfo.Get<T>());
            Debug.Assert(typeof(T) == _archetype.UnmanagedTypes[comIndex].Type);
            if (comIndex < 0) { throw new ArgumentOutOfRangeException(nameof(T)); }
            return ref _unmanaged.Get<T>(index, comIndex);
        }

        /// <summary>
        /// 不安全的方法! 你必须保证在使用引用期间该<see cref="EntityMemory"/>没有任何结构变化, 否则可能出现悬垂引用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetUnmanagedComponent<T>(int index, int comIndex) where T : unmanaged, IComponent
        {
            Debug.Assert(typeof(T) == _archetype.UnmanagedTypes[comIndex].Type);
            if (comIndex >= _unComCnt) { throw new ArgumentOutOfRangeException(nameof(index)); }
            return ref _unmanaged.Get<T>(index, comIndex);
        }

        public ManagedComponentLocator<T> GetManagedComponentLocator<T>() where T : IComponent
        {
            int comIndex = _archetype.IndexOf(TypeInfo.Get<T>());
            Debug.Assert(typeof(T) == _archetype.UnmanagedTypes[comIndex].Type);
            return new ManagedComponentLocator<T>(_managed[comIndex]);
        }

        /// <summary>
        /// 不安全的方法! 你必须保证在使用引用期间该<see cref="EntityMemory"/>没有任何结构变化, 否则可能出现悬垂引用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetManagedComponent<T>(int index) where T : IComponent
        {
            int comIndex = _archetype.IndexInManaged(TypeInfo.Get<T>());
            Debug.Assert(typeof(T) == _archetype.ManagedTypes[comIndex].Type);
            return ref ((ComponentList<T>)_managed[comIndex]).GetReference(index);
        }

        /// <summary>
        /// 不安全的方法! 你必须保证在使用引用期间该<see cref="EntityMemory"/>没有任何结构变化, 否则可能出现悬垂引用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetManagedComponent<T>(int index, int comIndex) where T : IComponent
        {
            Debug.Assert(typeof(T) == _archetype.UnmanagedTypes[comIndex].Type);
            return ref ((ComponentList<T>)_managed[comIndex]).GetReference(index);
        }
    }
}
