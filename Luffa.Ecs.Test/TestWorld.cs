using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Luffa.Ecs.Test
{
    struct Move : IComponent { public float Velocity; }
    struct Jump : IComponent { public double CanMove; }
    class GameObject : IComponent { public int Number; }

    [TestClass]
    public class TestWorld
    {
        [TestMethod]
        public void Simple()
        {
            var world = new World();
            var arch = EntityArchetype.Get(TypeInfo.Get<Move>(), TypeInfo.Get<Jump>());

            const int cnt = 16;

            for (int i = 0; i < cnt; i++)
            {
                world.CreateEntity(arch);
            }

            Assert.AreEqual(cnt, world.EntityCount);

            {
                int i = 0;
                var e = world.GetEntityMemory(world.GetEntityUnsafe(0));
                var viewer = e.GetViewer();
                var entity = e.GetEntityLocator();
                foreach (var index in viewer)
                {
                    Assert.AreEqual(i, entity.Locate(in index).UniqueId);
                    i++;
                }
                Assert.AreEqual(cnt, i);
            }

            int destroyed = 7; //现在要删掉7
            int moved = world.EntityCount - 1; //按照规则将末尾的实体15交换到7
            world.DestroyEntity(world.GetEntityUnsafe(destroyed));
            Assert.AreEqual(moved, world.EntityCount);//当前实体数量=15

            {
                int i = 0;
                var e = world.GetEntityMemory(world.GetEntityUnsafe(0));
                var viewer = e.GetViewer();
                var entity = e.GetEntityLocator();
                foreach (var index in viewer)
                {
                    if (i == destroyed) //所以迭代到7的时候, 现在是15
                    {
                        Assert.AreEqual(moved, entity.Locate(in index).UniqueId);
                    }
                    else
                    {   //其他时候照旧
                        Assert.AreEqual(i, entity.Locate(in index).UniqueId);
                    }

                    i++;
                }
                Assert.AreEqual(moved, i);
            }
        }

        [TestMethod]
        public void AddCom()
        {
            var world = new World();
            var arch = EntityArchetype.Get(TypeInfo.Get<Move>());

            var a = world.CreateEntity(arch);
            var b = world.CreateEntity(arch);

            float v = 12.41525f;

            world.GetUnmanagedComponent<Move>(a).Velocity = v;

            world.AddComponent(a, TypeInfo.Get<Jump>());
            Assert.IsTrue(world.HasComponent<Jump>(a));

            Assert.AreEqual(v, world.GetUnmanagedComponent<Move>(a).Velocity);

            world.AddComponent(b, TypeInfo.Get<GameObject>());
            Assert.IsTrue(world.HasComponent<GameObject>(b));
            Assert.IsTrue(world.HasComponent<Move>(b));
            Assert.IsFalse(world.HasComponent<Jump>(b));
        }

        [TestMethod]
        public void AddComSingle()
        {
            var world = new World();
            var arch = EntityArchetype.Get(TypeInfo.Get<Move>());

            var a = world.CreateEntity(arch);

            float v = 12.41525f;

            world.GetUnmanagedComponent<Move>(a).Velocity = v;

            world.AddComponent(a, TypeInfo.Get<Jump>());
            Assert.IsTrue(world.HasComponent<Jump>(a));

            Assert.AreEqual(v, world.GetUnmanagedComponent<Move>(a).Velocity);

            Assert.ThrowsException<ArgumentException>(() => world.AddComponent(a, TypeInfo.Get<Jump>()));
        }

        [TestMethod]
        public void RemoveComSingle()
        {
            var world = new World();
            var arch = EntityArchetype.Get(TypeInfo.Get<Move>(), TypeInfo.Get<Jump>());

            var a = world.CreateEntity(arch);
            Assert.IsTrue(world.HasComponent<Move>(a));
            Assert.IsTrue(world.HasComponent<Jump>(a));

            world.RemoveComponent(a, TypeInfo.Get<Jump>());
            Assert.IsTrue(world.HasComponent<Move>(a));
            Assert.IsFalse(world.HasComponent<Jump>(a));
        }

        [TestMethod]
        public void RemoveCom()
        {
            var world = new World();
            var mv = EntityArchetype.Get(TypeInfo.Get<Move>());
            var arch = EntityArchetype.Get(TypeInfo.Get<Move>(), TypeInfo.Get<Jump>());

            var a = world.CreateEntity(arch);
            var b = world.CreateEntity(mv);

            Assert.IsTrue(world.HasComponent<Move>(a));
            Assert.IsTrue(world.HasComponent<Jump>(a));

            Assert.IsTrue(world.HasComponent<Move>(b));
            Assert.IsFalse(world.HasComponent<Jump>(b));

            world.RemoveComponent(a, TypeInfo.Get<Jump>());
            Assert.IsTrue(world.HasComponent<Move>(a));
            Assert.IsFalse(world.HasComponent<Jump>(a));

            world.RemoveComponent(b, TypeInfo.Get<Move>());
            Assert.IsFalse(world.HasComponent<Move>(b));
            Assert.IsFalse(world.HasComponent<Jump>(b));

            Assert.ThrowsException<ArgumentException>(() => world.RemoveComponent(b, TypeInfo.Get<Move>()));
        }
    }
}
