using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Luffa.Ecs.Test
{
    [TestClass]
    public class TestEntityMemory
    {
        struct Move : IComponent { public float Velocity; }
        struct Jump : IComponent { public double CanMove; }
        class UnityObjectWrapper : IComponent { public int Number; } //233
        class UnrealActor : IComponent { public int Yahoo; }

        [TestMethod]
        public void UniqueId()
        {
            var a = EntityArchetype.Get();
            var e = new EntityMemory(a);

            int[] idx = new int[8192];
            for (int i = 0; i < 8192; i++)
            {
                idx[i] = Random.Shared.Next();
                e.Allocate(idx[i]);
            }

            {
                int i = 0;
                var viewer = e.GetViewer();
                var entity = e.GetEntityLocator();
                foreach (var index in viewer)
                {
                    if (i == 4095)
                    {
                        Console.WriteLine(i);
                    }
                    Assert.AreEqual(idx[i], entity.Locate(in index).UniqueId);
                    Assert.AreEqual(index.EntityIndex, i);
                    i++;
                }
                Assert.AreEqual(idx.Length, i);
            }

            Assert.AreEqual(idx[8191], e.Release(6));
            idx[6] = idx[8191];
            {
                int i = 0;
                var viewer = e.GetViewer();
                var entity = e.GetEntityLocator();
                foreach (var index in viewer)
                {
                    Assert.AreEqual(idx[i], entity.Locate(in index).UniqueId);
                    Assert.AreEqual(index.EntityIndex, i);
                    i++;
                }
                Assert.AreNotEqual(idx.Length, i);
                Assert.AreEqual(idx.Length - 1, i);
            }
        }

        [TestMethod]
        public void Default()
        {
            var a = EntityArchetype.Get(TypeInfo.Get<Move>(), TypeInfo.Get<Jump>());
            var e = new EntityMemory(a);

            const int cnt = 2048;
            Move[] mv = new Move[cnt];
            for (int i = 0; i < cnt; i++)
            {
                mv[i].Velocity = (float)Random.Shared.NextDouble();
                e.Allocate(i);
                e.GetUnmanagedComponent<Move>(i) = mv[i];
            }
            {
                int i = 0;
                var viewer = e.GetViewer();
                var entity = e.GetUnmanagedComponentLocator<Move>();
                foreach (var index in viewer)
                {
                    Assert.AreEqual(mv[i].Velocity, entity.Locate(in index).Velocity);
                    Assert.AreEqual(index.EntityIndex, i);
                    i++;
                }
            }

            Assert.AreEqual(cnt - 1, e.Release(6));
            mv[6] = mv[cnt - 1];

            {
                int i = 0;
                var viewer = e.GetViewer();
                var entity = e.GetUnmanagedComponentLocator<Move>();
                foreach (var index in viewer)
                {
                    Assert.AreEqual(mv[i].Velocity, entity.Locate(in index).Velocity);
                    Assert.AreEqual(index.EntityIndex, i);
                    i++;
                }
                Assert.AreNotEqual(mv.Length, i);
                Assert.AreEqual(mv.Length - 1, i);
            }
        }

        [TestMethod]
        public void ReferenceComponent()
        {
            var a = EntityArchetype.Get(TypeInfo.Get<UnityObjectWrapper>(), TypeInfo.Get<UnrealActor>());
            var e = new EntityMemory(a);

            for (int i = 0; i < 16; i++)
            {
                e.Allocate(i);
            }

            var wrap = e.GetManagedComponent<UnityObjectWrapper>(15);

            e.Release(7);

            var slot = e.GetManagedComponent<UnityObjectWrapper>(7);

            Assert.AreEqual(wrap, slot);
        }
    }
}
