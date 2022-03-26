using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luffa.Ecs.Test
{
    [TestClass]
    public class TestTypeInfo
    {
        struct A : IComponent { int Foo; }
        struct B : IComponent { double Foo; }
        class C : IComponent { int Foo; }

        [TestMethod]
        public unsafe void TestId()
        {
            Assert.AreEqual(typeof(A), TypeInfo<A>.Type);
            Assert.AreEqual(typeof(B), TypeInfo<B>.Type);
            Assert.AreEqual(typeof(C), TypeInfo<C>.Type);
            Assert.AreEqual(true, TypeInfo<A>.IsUnmanaged);
            Assert.AreEqual(true, TypeInfo<B>.IsUnmanaged);
            Assert.AreEqual(false, TypeInfo<C>.IsUnmanaged);
        }
    }
}
