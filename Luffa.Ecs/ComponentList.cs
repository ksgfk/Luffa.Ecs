using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Luffa.Ecs
{
    public readonly ref struct ManagedComponentLocator<T> where T : IComponent
    {
        private readonly ComponentList<T> _list;

        internal ManagedComponentLocator(IComponentList list)
        {
            if (!(list is ComponentList<T> typed)) { throw new InvalidOperationException("maybe its a bug"); }
            _list = typed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Locate(in ComponentViewer.Indexer indexer)
        {
            return ref _list.GetReference(indexer.EntityIndex);
        }
    }

    public interface IComponentList : IList
    {
        int Allocate();

        void Release(int index);

        void CopyElementTo(int from, IComponentList target, int to);
    }

    public interface IComponentList<T> : IComponentList, IList<T> where T : IComponent
    {
        /// <summary>
        /// 不安全的方法! 你必须保证在使用引用期间没有任何结构变化, 否则可能出现悬垂引用
        /// </summary>
        ref T GetReference(int index);
    }

    public sealed class ComponentList<T> : IComponentList<T> where T : IComponent // 从标准库复制的，删了些不需要的部分
    {
        private const int DefaultCapacity = 4;

        internal T[] _items;
        internal int _size;
        private int _version;

        public ComponentList() { _items = Array.Empty<T>(); }

        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < _size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, newItems, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        _items = Array.Empty<T>();
                    }
                }
            }
        }

        public int Count => _size;

        public bool IsReadOnly => false;

        bool IList.IsFixedSize => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        object IList.this[int index] { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return _items[index];
            }
            set
            {
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                _items[index] = value;
                _version++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            _version++;
            T[] array = _items;
            int size = _size;
            if ((uint)size < (uint)array.Length)
            {
                _size = size + 1;
                array[size] = item;
            }
            else
            {
                AddWithResize(item);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item)
        {
            Debug.Assert(_size == _items.Length);
            int size = _size;
            Grow(size + 1);
            _size = size + 1;
            _items[size] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _version++;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                int size = _size;
                _size = 0;
                if (size > 0)
                {
                    Array.Clear(_items, 0, size);
                }
            }
            else
            {
                _size = 0;
            }
        }

        public bool Contains(T item)
        {
            return _size != 0 && IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array) => CopyTo(array, 0);

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        private void Grow(int capacity)
        {
            Debug.Assert(_items.Length < capacity);
            int newcapacity = _items.Length == 0 ? DefaultCapacity : 2 * _items.Length;
            if ((uint)newcapacity > int.MaxValue) newcapacity = int.MaxValue;
            if (newcapacity < capacity) newcapacity = capacity;
            Capacity = newcapacity;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public int IndexOf(T item) => Array.IndexOf(_items, item, 0, _size);

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (_size == _items.Length) Grow(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException();
            }
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _items[_size] = default!;
            }
            _version++;
        }

        public T[] ToArray()
        {
            if (_size == 0)
            {
                return Array.Empty<T>();
            }
            T[] array = new T[_size];
            Array.Copy(_items, array, _size);
            return array;
        }

        public void TrimExcess()
        {
            int threshold = (int)(_items.Length * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }

        int IList.Add(object value) => throw new InvalidOperationException();

        bool IList.Contains(object value) => throw new InvalidOperationException();

        int IList.IndexOf(object value) => throw new InvalidOperationException();

        void IList.Insert(int index, object value) => throw new InvalidOperationException();

        void IList.Remove(object value) => throw new InvalidOperationException();

        void ICollection.CopyTo(Array array, int index) => throw new InvalidOperationException();

        public int Allocate()
        {
            int index = Count;
            Add(Activator.CreateInstance<T>());
            return index;
        }

        public void Release(int index)
        {
            int lastIndex = Count - 1;
            this[index] = this[lastIndex];
            RemoveAt(lastIndex);
        }

        public ref T GetReference(int index)
        {
            return ref _items[index];
        }

        public void CopyElementTo(int from, IComponentList target, int to)
        {
            ComponentList<T> t = (ComponentList<T>)target;
            t[to] = this[from];
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ComponentList<T> _list;
            private int _index;
            private readonly int _version;
            private T _current;

            internal Enumerator(ComponentList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list._version;
                _current = default!;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                ComponentList<T> localList = _list;
                if (_version == localList._version && ((uint)_index < (uint)localList._size))
                {
                    _current = localList._items[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException();
                }
                _index = _list._size + 1;
                _current = default!;
                return false;
            }

            public T Current => _current!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list._size + 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException();
                }
                _index = 0;
                _current = default!;
            }
        }
    }
}
