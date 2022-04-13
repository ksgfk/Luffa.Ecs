using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Luffa.Ecs.Test
{
    [TestClass]
    public class TestCmdBuffer
    {
        struct A : IComponent { }
        struct B : IComponent { }

        class SimpleSystem : SystemBase
        {
            public override IEntityFilter Filter { get; } = EntityFilter.FromRequire(TypeInfo.Get<A>());

            protected override void UpdateEntity(World world, EntityMemory memory, CmdBuffer cmd)
            {
                ComponentViewer view = GetComponentViewer(memory);
                var el = GetEntityLocator(memory);
                foreach (var i in view)
                {
                    var e = el.Locate(i, world);
                    if (e.Index % 3 == 0)
                    {
                        cmd.DestroyEntity(e);
                    }
                    else
                    {
                        cmd.AddComponent(e, TypeInfo.Get<B>());
                    }
                }
            }
        }

        [TestMethod]
        public void TestSimple()
        {
            World world = new();
            EntityArchetype empty = EntityArchetype.Get();
            EntityArchetype a = EntityArchetype.Get(TypeInfo.Get<A>());
            world.AddArchetype(empty);
            world.AddArchetype(a);
            world.AddSystem<SimpleSystem>();

            for (int i = 0; i < 16; i++)
            {
                world.CreateEntity(a);
            }
            Assert.AreEqual(16, world.EntityCount);

            world.OnUpdate();

            Assert.AreEqual(10, world.EntityCount);

            EntityFilter filter = EntityFilter.FromRequire(TypeInfo.Get<A>(), TypeInfo.Get<B>());
            world.FilterEntity(filter);
            Assert.AreEqual(1, filter.MatchedCount);
            EntityMemory memory = world.GetEntityMemory(filter.MatchedArchetype.First());
            Assert.AreEqual(10, memory.Count);
        }
    }
}
