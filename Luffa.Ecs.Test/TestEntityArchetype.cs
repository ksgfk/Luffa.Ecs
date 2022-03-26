using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Luffa.Ecs.Test
{
    [TestClass]
    public class TestEntityArchetype
    {
        struct Move : IComponent { }
        struct Jump : IComponent { }
        struct Idle : IComponent { }

        struct A : IComponent { }
        struct B : IComponent { }
        struct C : IComponent { }
        struct D : IComponent { }
        class E : IComponent { }
        class F : IComponent { }
        class G : IComponent { }
        class H : IComponent { }

        [TestMethod]
        public void TestDefault()
        {
            var a = new EntityArchetype(new[] { TypeInfo.Get<Move>(), TypeInfo.Get<Jump>() });
            var b = new EntityArchetype(new[] { TypeInfo.Get<Move>(), TypeInfo.Get<Jump>() });
            Assert.AreEqual(a, b);

            var c = new EntityArchetype(new[] { TypeInfo.Get<Move>(), TypeInfo.Get<Jump>(), TypeInfo.Get<Idle>() });
            Assert.AreNotEqual(a, c);

            var d = new EntityArchetype(new[] { TypeInfo.Get<Jump>(), TypeInfo.Get<Idle>() });
            Assert.AreNotEqual(a, d);

            Assert.IsTrue(c.IsExist<Move>());
            Assert.IsTrue(a.IsNotExist<Idle>());
            Assert.IsFalse(a.IsNotExist<Move>());

            Assert.AreEqual(a.Attach(TypeInfo.Get<Idle>()), c);
            Assert.AreEqual(c.Detach(TypeInfo.Get<Idle>()), a);

            var e = d.Detach(TypeInfo.Get<Idle>()).Attach(TypeInfo.Get<Move>(), TypeInfo.Get<Idle>());
            Assert.AreEqual(e, c);

            var f = c.Detach(new[] { TypeInfo.Get<Jump>(), TypeInfo.Get<Idle>() });
            Assert.AreEqual(f, new EntityArchetype(new[] { TypeInfo.Get<Move>() }));

            var z = new EntityArchetype(new[] { TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>(), TypeInfo.Get<D>() });

            var zc = new EntityArchetype(new[] { TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>() });
            var noD = z.Detach(TypeInfo.Get<D>());
            Assert.AreEqual(noD, zc);

            var zd = new EntityArchetype(new[] { TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<D>() });
            var noC = z.Detach(TypeInfo.Get<C>());
            Assert.AreEqual(noC, zd);

            var za = new EntityArchetype(new[] { TypeInfo.Get<B>(), TypeInfo.Get<C>(), TypeInfo.Get<D>() });
            var noA = z.Detach(TypeInfo.Get<A>());
            Assert.AreEqual(noA, za);

            var zac = new EntityArchetype(new[] { TypeInfo.Get<B>(), TypeInfo.Get<D>() });
            var noAC = z.Detach(TypeInfo.Get<A>(), TypeInfo.Get<C>());
            Assert.AreEqual(zac, noAC);

            var zacd = new EntityArchetype(new[] { TypeInfo.Get<B>() });
            var noACD = z.Detach(TypeInfo.Get<A>(), TypeInfo.Get<C>(), TypeInfo.Get<D>());
            Assert.AreEqual(zacd, noACD);

            var zabcd = new EntityArchetype(Array.Empty<ComponentType>());
            var noABCD = z.Detach(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>(), TypeInfo.Get<D>());
            Assert.AreEqual(zabcd, noABCD);
        }

        [TestMethod]
        public void TestWorld()
        {
            var world = new World();
            var a = new EntityArchetype(new[] { TypeInfo.Get<Move>(), TypeInfo.Get<Jump>() });
            var b = new EntityArchetype(new[] { TypeInfo.Get<Move>(), TypeInfo.Get<Idle>() });
            var c = new EntityArchetype(new[] { TypeInfo.Get<Move>() });
            world.AddArchetype(a);
            Assert.ThrowsException<ArgumentException>(() => world.AddArchetype(a));
            Assert.AreEqual(b, world.Attach(c, TypeInfo.Get<Idle>()));
            Assert.AreEqual(c, world.Detach(a, TypeInfo.Get<Jump>()));
        }

        [TestMethod]
        public void TestType()
        {
            var zd = EntityArchetype.Get(TypeInfo.Get<A>(), TypeInfo.Get<B>(), TypeInfo.Get<C>(),
                TypeInfo.Get<E>(), TypeInfo.Get<F>(), TypeInfo.Get<G>());
            Assert.AreEqual(3, zd.UnmanagedTypes.Count);
            Assert.AreEqual(3, zd.ManagedTypes.Count);

            Assert.IsTrue(zd.IndexInManaged<H>() < 0);
            Assert.AreEqual(1, zd.IndexInUnmanaged<B>());
            Assert.AreEqual(0, zd.IndexInManaged<E>());
            Assert.IsTrue(zd.IndexInManaged<B>() < 0);
        }
    }
}
