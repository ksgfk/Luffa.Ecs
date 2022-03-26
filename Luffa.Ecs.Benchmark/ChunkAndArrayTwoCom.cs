using BenchmarkDotNet.Attributes;
using System.Numerics;

namespace Luffa.Ecs.Benchmark
{
    /*
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.201
  [Host]     : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT
  DefaultJob : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT


|        Method |     Mean |    Error |   StdDev | Code Size |
|-------------- |---------:|---------:|---------:|----------:|
|      TwoArray | 13.37 ms | 0.064 ms | 0.053 ms |     219 B |
| ChunkWith2Com | 28.65 ms | 0.151 ms | 0.134 ms |   1,124 B |
    */
    [DisassemblyDiagnoser(printSource: true)]
    public class ChunkAndArrayTwoCom
    {
        public struct Transform : IComponent
        {
            public Vector3 Position;
        }
        public struct Move : IComponent
        {
            public Vector3 Velocity;
        }

        const int TestCount = 5_000_000;
        static float DeltaTime = 0.1425f;

        #region Array
        Transform[] _transList;
        Move[] _moveList;
        [GlobalSetup(Target = nameof(TwoArray))]
        public void GetOriginalData()
        {
            Random rand = new Random();
            var transList = new Transform[TestCount];
            var moveList = new Move[TestCount];
            for (int i = 0; i < TestCount; i++)
            {
                transList[i].Position = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                moveList[i].Velocity = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
            }
            _transList = transList;
            _moveList = moveList;
        }
        [Benchmark]
        public void TwoArray()
        {
            int count = _transList.Length;
            for (int i = 0; i < count; i++)
            {
                ref var t = ref _transList[i];
                ref var m = ref _moveList[i];
                t.Position += m.Velocity * DeltaTime;
            }
        }
        #endregion

        #region Chunk
        EntityMemory _memory;
        [GlobalSetup(Target = nameof(ChunkWith2Com))]
        public void GetChunkData()
        {
            Random rand = new Random();
            var arch = EntityArchetype.Get(TypeInfo.Get<Transform>(), TypeInfo.Get<Move>());
            _memory = new EntityMemory(arch);
            for (int i = 0; i < TestCount; i++)
            {
                var entity = _memory.Allocate(i);
                _memory.GetUnmanagedComponent<Transform>(entity).Position = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _memory.GetUnmanagedComponent<Move>(entity).Velocity = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
            }
        }
        [Benchmark]
        public void ChunkWith2Com()
        {
            var viewer = _memory.GetViewer();
            var trans = _memory.GetUnmanagedComponentLocator<Transform>();
            var move = _memory.GetUnmanagedComponentLocator<Move>();
            foreach (var index in viewer)
            {
                ref var t = ref trans.Locate(in index);
                ref readonly var p = ref move.Locate(in index);
                t.Position += p.Velocity * DeltaTime;
            }
        }
        #endregion
    }
}
