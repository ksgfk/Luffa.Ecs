using BenchmarkDotNet.Attributes;

namespace Luffa.Ecs.Benchmark
{
    /*
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.201
  [Host]     : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT
  DefaultJob : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT


|       Method |      Mean |     Error |    StdDev | Code Size |
|------------- |----------:|----------:|----------:|----------:|
|     OneArray |  8.922 ms | 0.1161 ms | 0.1086 ms |      86 B |
| ChunkWithCom | 29.343 ms | 0.1031 ms | 0.0861 ms |     702 B |
     */
    [DisassemblyDiagnoser(printSource: true)]
    public class ChunkAndArrayOneCom
    {
        public struct Life : IComponent
        {
            public float Health;
            public float Recover;
        }

        const int TestCount = 10_000_000;

        #region Array
        Life[] _lifeList;
        [GlobalSetup(Target = nameof(OneArray))]
        public void GetOriginalData()
        {
            Random rand = new Random();
            _lifeList = new Life[TestCount];
            for (int i = 0; i < TestCount; i++)
            {
                _lifeList[i].Health = (float)rand.NextDouble();
                _lifeList[i].Recover = (float)rand.NextDouble();
            }
        }
        [Benchmark]
        public void OneArray()
        {
            int count = _lifeList.Length;
            for (int i = 0; i < count; i++)
            {
                ref var t = ref _lifeList[i];
                t.Health += t.Recover * 0.1256f;
            }
        }
        #endregion

        #region Chunk
        EntityMemory _memory;
        [GlobalSetup(Target = nameof(ChunkWithCom))]
        public void GetChunkData()
        {
            Random rand = new Random();
            var arch = EntityArchetype.Get(TypeInfo.Get<Life>());
            _memory = new EntityMemory(arch);
            for (int i = 0; i < TestCount; i++)
            {
                var entity = _memory.Allocate(i);
                ref var t = ref _memory.GetUnmanagedComponent<Life>(entity);
                t.Health = (float)rand.NextDouble();
                t.Recover = (float)rand.NextDouble();
            }
        }
        [Benchmark]
        public void ChunkWithCom()
        {
            var viewer = _memory.GetViewer();
            var life = _memory.GetUnmanagedComponentLocator<Life>();
            foreach (var index in viewer)
            {
                ref var l = ref life.Locate(in index);
                l.Health += l.Recover * 0.1256f;
            }
        }
        #endregion
    }
}
