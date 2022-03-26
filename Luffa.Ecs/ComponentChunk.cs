using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Luffa.Ecs
{
    public readonly ref struct UnmanagedComponentLocator<T> where T : unmanaged, IComponent
    {
        private readonly int _startIndexInChunk;

        unsafe internal UnmanagedComponentLocator(ComponentChunk chunk, int comIndex)
        {
            int comSize = chunk._comSize[comIndex];
            if (comSize != sizeof(T)) { throw new InvalidOperationException("maybe its a bug"); }
            _startIndexInChunk = chunk._comStartIdx[comIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T Locate(in ComponentViewer.Indexer indexer)
        {
            Debug.Assert(sizeof(T) * indexer._inChunkIndex < indexer._data.Length);
            fixed (byte* p = &indexer._data[_startIndexInChunk])
            {
                return ref Unsafe.AsRef<T>(p + sizeof(T) * indexer._inChunkIndex);
            }
        }
    }

    public readonly struct Chunk
    {
        public readonly byte[] Data;

        public Chunk(int size) { Data = new byte[size]; }
    }

    public class ComponentChunk
    {
        private readonly ComponentType[] _layout;
        private readonly List<Chunk> _chunks;       //区块表
        private readonly Queue<Chunk> _empty;       //空闲的区块, 缓存一下
        internal readonly int[] _comSize;           //组件占用字节
        internal readonly int[] _comStartIdx;       //组件在单个区块内起始下标
        private readonly int _chunkSize;            //单个区块占用字节
        private readonly int _entitySize;           //一个实体拥有的非托管组件总字节
        internal readonly int _entityCountPerChunk; //单个区块可以存多少个实体
        private readonly int _maxCacheChunk;        //最大缓存的区块数量
        private int _nowCount;                      //当前实体数量

        public int Count => _nowCount;
        public List<Chunk> Chunks => _chunks;

        public ComponentChunk(ComponentType[] layout, int chunkSize, int maxCacheChunk)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _chunkSize = chunkSize;
            _comSize = new int[_layout.Length];
            _comStartIdx = new int[_layout.Length];
            _chunks = new List<Chunk>
            {
                new Chunk(_chunkSize)
            };
            _empty = new Queue<Chunk>();
            _nowCount = 0;
            _maxCacheChunk = maxCacheChunk;

            for (int i = 0; i < _layout.Length; i++)
            {
                _comSize[i] = TypeInfo.SizeOfComponent(_layout[i]);
            }
            _entitySize = _comSize.Sum();
            _entityCountPerChunk = _chunkSize / _entitySize;
            if (_entityCountPerChunk <= 0) { throw new ArgumentException("chunk size too small"); }

            int currentIndex = 0;
            for (int i = 0; i < _layout.Length; i++)
            {
                _comStartIdx[i] = currentIndex;
                currentIndex += _comSize[i] * _entityCountPerChunk;
            }
        }

        public int Allocate()
        {
            int index = _nowCount;
            if (_nowCount >= _chunks.Count * _entityCountPerChunk)
            {
                if (_empty.Count == 0)
                {
                    _chunks.Add(new Chunk(_chunkSize));
                }
                else
                {
                    _chunks.Add(_empty.Dequeue());
                }
            }
            _nowCount++;
            return index;
        }

        public void Release(int index)
        {
            Debug.Assert(index >= 0 && index < _nowCount);
            int lastIndex = _nowCount - 1;
            if (index == lastIndex)
            {
                _nowCount--;
                return;
            }
            int targetInnerIndex = index % _entityCountPerChunk;
            byte[] targetChunk = _chunks[index / _entityCountPerChunk].Data;
            int lastInnerIndex = lastIndex % _entityCountPerChunk;
            byte[] lastChunk = _chunks[lastIndex / _entityCountPerChunk].Data;
            for (int i = 0; i < _layout.Length; i++)
            {
                int targetComIndex = _comStartIdx[i] + _comSize[i] * targetInnerIndex;
                int lastComIndex = _comStartIdx[i] + _comSize[i] * lastInnerIndex;
                Span<byte> target = targetChunk.AsSpan(targetComIndex, _comSize[i]);
                Span<byte> last = lastChunk.AsSpan(lastComIndex, _comSize[i]);
                last.CopyTo(target);
            }
            _nowCount--;
            if (_nowCount < (_chunks.Count - 1) * _entityCountPerChunk)
            {
                if (_chunks.Count > 1)
                {
                    if (_empty.Count < _maxCacheChunk)
                    {
                        _empty.Enqueue(_chunks[^1]);
                    }
                    _chunks.RemoveAt(_chunks.Count - 1);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Get(int index, int comIndex)
        {
            int idx = index % _entityCountPerChunk;
            byte[] chunk = _chunks[index / _entityCountPerChunk].Data;
            int comInChunk = _comStartIdx[comIndex] + _comSize[comIndex] * idx;
            return chunk.AsSpan(comInChunk, _comSize[comIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>(int index, int comIndex) where T : unmanaged, IComponent
        {
            Debug.Assert(typeof(T) == _layout[comIndex].Type);
            int idx = index % _entityCountPerChunk;
            byte[] chunk = _chunks[index / _entityCountPerChunk].Data;
            int comInChunk = _comStartIdx[comIndex] + _comSize[comIndex] * idx;
            return ref Unsafe.As<byte, T>(ref chunk[comInChunk]);
        }
    }
}
