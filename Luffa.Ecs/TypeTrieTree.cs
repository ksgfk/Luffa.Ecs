using System;
using System.Collections.Generic;

namespace Luffa.Ecs
{
    public class TypeTrieTree //前缀...树? 好像没啥用(
    {
        public class Node
        {
            public List<Node> Child;
            public EntityArchetype? Leaf;

            internal Node()
            {
                Leaf = default!;
                Child = new List<Node>();
            }
        }

        private readonly Node _root;

        public TypeTrieTree()
        {
            _root = new Node();
        }

        public void Add(EntityArchetype arch)
        {
            if (arch.Count == 0)
            {
                if (_root.Leaf != null) { throw new ArgumentException("duplicate add archetype"); }
                _root.Leaf = arch;
                return;
            }
            InternalAdd(_root, arch, 0);
        }

        private void InternalAdd(Node node, EntityArchetype arch, int idx)
        {
            List<Node> child = node.Child;
            ComponentType thisType = arch[idx];
            int typeId = thisType.Id;
            if (child.Count <= thisType.Id)
            {
                int nowCnt = child.Count;
                for (int i = 0; i < typeId - nowCnt + 1; i++)
                {
                    child.Add(null!);
                }
            }
            Node next = child[typeId] ??= new Node();
            if (idx == arch.Count - 1)
            {
                if (next.Leaf != null) { throw new ArgumentException("duplicate add archetype"); }
                next.Leaf = arch;
                return;
            }
            InternalAdd(next, arch, idx + 1);
        }

        public EntityArchetype? Find(ComponentType[] arr)
        {
            return Find(new ReadOnlySpan<ComponentType>(arr));
        }

        public EntityArchetype? Find(ReadOnlySpan<ComponentType> span)
        {
            Node node = _root;
            int i = 0;
            for (; i < span.Length; i++)
            {
                ComponentType now = span[i];
                if (node == null) { break; }
                int id = now.Id;
                if (node.Child.Count <= id) { break; }
                node = node.Child[id];
            }
            if (i == span.Length && node != null && node.Leaf != null)
            {
                return node.Leaf;
            }
            return null;
        }

        public bool TryAdd(ComponentType[] arr, out EntityArchetype result)
        {
            return TryAdd(new ReadOnlySpan<ComponentType>(arr), out result);
        }

        public bool TryAdd(ReadOnlySpan<ComponentType> span, out EntityArchetype result)
        {
            if (span.Length == 0) //特判根节点放空原型
            {
                if (_root.Leaf == null)
                {
                    _root.Leaf = new EntityArchetype(span.ToArray());
                    result = _root.Leaf;
                    return true;
                }
                else
                {
                    result = _root.Leaf;
                    return false;
                }
            }
            Node node = _root;
            int i = 0;
            while (true)
            {
                ComponentType now = span[i];
                int id = now.Id;
                if (node.Child.Count <= id || node.Child[id] == null)
                {
                    var newArch = new EntityArchetype(span.ToArray());
                    InternalAdd(node, newArch, i);
                    result = newArch;
                    return true;
                }
                node = node.Child[id];
                if (i == span.Length - 1)
                {
                    if (node.Leaf == null)
                    {
                        node.Leaf = new EntityArchetype(span.ToArray());
                        result = node.Leaf;
                        return true;
                    }
                    else
                    {
                        result = node.Leaf;
                        return false;
                    }
                }
                i++;
            }
        }
    }
}
