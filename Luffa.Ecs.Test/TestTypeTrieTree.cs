using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luffa.Ecs.Test
{
    [TestClass]
    public class TestTypeTrieTree
    {
        struct A : IComponent { int Foo; }
        struct B : IComponent { double Foo; }
        struct C : IComponent { int Foo; }
        struct D : IComponent { int Foo; }

        [TestMethod]
        public void TestAdd()
        {
            var abcd = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>(), TypeInfo.Get<D>());
            var abc = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>());
            var abd = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<D>());
            var bcd = EntityArchetype.Get(TypeInfo.Get<B>(), TypeInfo.Get<C>(), TypeInfo.Get<D>());

            var tree = new TypeTrieTree();
            tree.Add(abcd);
            tree.Add(abd);
            tree.Add(bcd);

            Assert.AreEqual(null, tree.Find(abc.ToSpan()));

            Assert.IsTrue(tree.TryAdd(abc.ToSpan(), out var n));
            Assert.AreEqual(abc, n);
            Assert.IsFalse(tree.TryAdd(abc.ToSpan(), out _));

            Assert.AreEqual(abc, tree.Find(abc.ToSpan()));
        }

        [TestMethod]
        public void TestTryAddSingle()
        {
            var a = EntityArchetype.Get(TypeInfo.Get<A>());

            var tree = new TypeTrieTree();

            Assert.IsTrue(tree.TryAdd(a.ToSpan(), out var taa));
            Assert.AreEqual(a, taa);
            Assert.AreEqual(a, tree.Find(a.ToSpan()));
        }

        [TestMethod]
        public void TestTryAddMany()
        {
            var abc = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>());
            var a = EntityArchetype.Get(TypeInfo.Get<A>());

            var tree = new TypeTrieTree();

            Assert.IsTrue(tree.TryAdd(a.ToSpan(), out var taa));
            Assert.AreEqual(a, taa);
            Assert.AreEqual(a, tree.Find(a.ToSpan()));

            Assert.IsTrue(tree.TryAdd(abc.ToSpan(), out var taabc));
            Assert.AreEqual(abc, taabc);
            Assert.AreEqual(abc, tree.Find(abc.ToSpan()));
        }

        [TestMethod]
        public void TestTryAddManyMany()
        {
            var abc = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>());
            var ab = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>());
            var bcd = EntityArchetype.Get(TypeInfo.Get<B>(), TypeInfo.Get<C>(), TypeInfo.Get<D>());

            var tree = new TypeTrieTree();

            Assert.IsTrue(tree.TryAdd(ab.ToSpan(), out var taab));
            Assert.AreEqual(ab, taab);
            Assert.AreEqual(ab, tree.Find(taab.ToSpan()));

            Assert.IsTrue(tree.TryAdd(abc.ToSpan(), out var taabc));
            Assert.AreEqual(abc, taabc);
            Assert.AreEqual(abc, tree.Find(abc.ToSpan()));

            Assert.IsTrue(tree.TryAdd(bcd.ToSpan(), out var tabcd));
            Assert.AreEqual(bcd, tabcd);
            Assert.AreEqual(bcd, tree.Find(tabcd.ToSpan()));
        }

        [TestMethod]
        public void TestTryAddV()
        {
            var abc = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>());
            var ab = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>());

            var tree = new TypeTrieTree();

            Assert.IsTrue(tree.TryAdd(abc.ToSpan(), out var taabc));
            Assert.AreEqual(abc, taabc);
            Assert.AreEqual(abc, tree.Find(abc.ToSpan()));

            Assert.IsTrue(tree.TryAdd(ab.ToSpan(), out var taab));
            Assert.AreEqual(ab, taab);
            Assert.AreEqual(ab, tree.Find(taab.ToSpan()));
        }

        [TestMethod]
        public void TestTryAddEmpty()
        {
            var e = EntityArchetype.Get();
            var b = EntityArchetype.Get(TypeInfo.Get<B>());
            var a = EntityArchetype.Get(TypeInfo.Get<A>());


            var tree = new TypeTrieTree();

            Assert.IsTrue(tree.TryAdd(e.ToSpan(), out var tae));
            Assert.AreEqual(e, tae);
            Assert.AreEqual(e, tree.Find(e.ToSpan()));

            Assert.IsTrue(tree.TryAdd(b.ToSpan(), out var tab));
            Assert.AreEqual(b, tab);
            Assert.AreEqual(b, tree.Find(b.ToSpan()));

            Assert.IsTrue(tree.TryAdd(a.ToSpan(), out var taa));
            Assert.AreEqual(a, taa);
            Assert.AreEqual(a, tree.Find(a.ToSpan()));
        }

        [TestMethod]
        public void TestTryAddSame()
        {
            var abc = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>());
            var abd = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<D>());

            var tree = new TypeTrieTree();

            Assert.IsTrue(tree.TryAdd(abc.ToSpan(), out var taabc));
            Assert.AreEqual(abc, taabc);
            Assert.AreEqual(abc, tree.Find(abc.ToSpan()));

            Assert.IsTrue(tree.TryAdd(abd.ToSpan(), out var taabd));
            Assert.AreEqual(abd, taabd);
            Assert.AreEqual(abd, tree.Find(abd.ToSpan()));
        }
    }
}
