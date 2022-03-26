using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Luffa.Ecs.Test
{
    [TestClass]
    public class TestSystem
    {
        class Move : IComponent { }
        class 禁锢 : IComponent { } //???
        class 冻结 : IComponent { }
        class 眩晕 : IComponent { }
        class Die : IComponent { }

        [TestMethod]
        public void TestMatch()
        {
            var canMove = new EntityFilter.Builder()
                .Require<Move>()
                .Exclude<禁锢>().Exclude<冻结>().Exclude<眩晕>().Build();

            var normal = EntityArchetype.Get(TypeInfo.Get<Move>());
            var frozen = EntityArchetype.Get(TypeInfo.Get<Move>(), TypeInfo.Get<冻结>());
            var dead = EntityArchetype.Get(TypeInfo.Get<Die>());

            Console.WriteLine(canMove.IsMatch(normal));
            Console.WriteLine(canMove.IsMatch(frozen));
            Console.WriteLine(canMove.IsMatch(dead));
        }
    }
}
