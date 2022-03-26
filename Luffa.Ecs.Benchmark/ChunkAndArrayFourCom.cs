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


|            Method |     Mean |    Error |   StdDev | Code Size |
|------------------ |---------:|---------:|---------:|----------:|
|         FourArray | 15.28 ms | 0.147 ms | 0.138 ms |   1,650 B |
|     ChunkWith3Com | 15.89 ms | 0.104 ms | 0.092 ms |   3,227 B |
| OneArrayFourParam | 15.50 ms | 0.109 ms | 0.096 ms |   1,643 B |
     */
    [DisassemblyDiagnoser]
    public class ChunkAndArrayFourCom
    {
        public struct Transform : IComponent
        {
            public Vector3 Position;
            public Matrix4x4 Rotation;
        }
        public struct Move : IComponent
        {
            public Vector3 Velocity;
        }
        public struct Rotate : IComponent
        {
            public Vector3 Axis;
            public float Angle;
        }
        public struct Coeff : IComponent
        {
            public float Val;
        }

        public struct BigSet
        {
            public Transform t;
            public Move m;
            public Rotate r;
            public Coeff v;
        }

        const int TestCount = 100_000;
        static float DeltaTime = 0.1425f;

        #region Array
        Transform[] _t;
        Move[] _m;
        Rotate[] _r;
        Coeff[] _v;
        [GlobalSetup(Target = nameof(FourArray))]
        public void GetOriginalData()
        {
            Random rand = new Random();
            _t = new Transform[TestCount];
            _m = new Move[TestCount];
            _r = new Rotate[TestCount];
            _v = new Coeff[TestCount];
            for (int i = 0; i < TestCount; i++)
            {
                _t[i].Position = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _t[i].Rotation = Matrix4x4.CreateRotationY((float)rand.NextDouble());
                _m[i].Velocity = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _r[i].Angle = (float)rand.NextDouble();
                _r[i].Axis = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _v[i].Val = (float)rand.NextDouble();
            }
        }
        [Benchmark]
        public void FourArray()
        {
            var count = _t.Length;
            for (int i = 0; i < count; i++)
            {
                ref var t = ref _t[i];
                ref var m = ref _m[i];
                ref var r = ref _r[i];
                ref var v = ref _v[i];
                t.Position += m.Velocity * DeltaTime;
                t.Rotation = Matrix4x4.Multiply(t.Rotation, Matrix4x4.CreateFromAxisAngle(r.Axis, r.Angle));
                t.Position -= new Vector3(v.Val);
                m.Velocity *= new Vector3(v.Val);
                r.Angle /= v.Val;
            }
        }
        #endregion

        #region Chunk
        [GlobalSetup(Target = nameof(ChunkWith3Com))]
        public void GetChunkData()
        {
            Random rand = new Random();
            var arch = EntityArchetype.Get(
                TypeInfo.Get<Transform>(),
                TypeInfo.Get<Move>(),
                TypeInfo.Get<Rotate>(),
                TypeInfo.Get<Coeff>());
            _memory = new EntityMemory(arch);
            for (int i = 0; i < TestCount; i++)
            {
                var entity = _memory.Allocate(i);
                ref var t = ref _memory.GetUnmanagedComponent<Transform>(entity);
                ref var m = ref _memory.GetUnmanagedComponent<Move>(entity);
                ref var r = ref _memory.GetUnmanagedComponent<Rotate>(entity);
                ref var v = ref _memory.GetUnmanagedComponent<Coeff>(entity);
                t.Position = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                t.Rotation = Matrix4x4.CreateRotationY((float)rand.NextDouble());
                m.Velocity = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                r.Angle = (float)rand.NextDouble();
                r.Axis = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                v.Val = (float)rand.NextDouble();
            }
        }
        EntityMemory _memory;
        [Benchmark]
        public void ChunkWith3Com()
        {
            var viewer = _memory.GetViewer();
            var trans = _memory.GetUnmanagedComponentLocator<Transform>();
            var move = _memory.GetUnmanagedComponentLocator<Move>();
            var rot = _memory.GetUnmanagedComponentLocator<Rotate>();
            var coe = _memory.GetUnmanagedComponentLocator<Coeff>();
            foreach (var index in viewer)
            {
                ref var t = ref trans.Locate(in index);
                ref var m = ref move.Locate(in index);
                ref var r = ref rot.Locate(in index);
                ref readonly var v = ref coe.Locate(in index);
                t.Position += m.Velocity * DeltaTime;
                t.Rotation = Matrix4x4.Multiply(t.Rotation, Matrix4x4.CreateFromAxisAngle(r.Axis, r.Angle));
                t.Position -= new Vector3(v.Val);
                m.Velocity *= new Vector3(v.Val);
                r.Angle /= v.Val;
            }
        }
        #endregion

        #region OneArrayFourParam
        BigSet[] _set;
        [GlobalSetup(Target = nameof(OneArrayFourParam))]
        public void GetOneArrayFourParamData()
        {
            Random rand = new Random();
            _set = new BigSet[TestCount];
            for (int i = 0; i < TestCount; i++)
            {
                _set[i].t.Position = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _set[i].t.Rotation = Matrix4x4.CreateRotationY((float)rand.NextDouble());
                _set[i].m.Velocity = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _set[i].r.Angle = (float)rand.NextDouble();
                _set[i].r.Axis = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _set[i].v.Val = (float)rand.NextDouble();
            }
        }
        [Benchmark]
        public void OneArrayFourParam()
        {
            var count = _set.Length;
            for (int i = 0; i < count; i++)
            {
                _set[i].t.Position += _set[i].m.Velocity * DeltaTime;
                _set[i].t.Rotation = Matrix4x4.Multiply(_set[i].t.Rotation, Matrix4x4.CreateFromAxisAngle(_set[i].r.Axis, _set[i].r.Angle));
                _set[i].t.Position -= new Vector3(_set[i].v.Val);
                _set[i].m.Velocity *= new Vector3(_set[i].v.Val);
                _set[i].r.Angle /= _set[i].v.Val;
            }
        }
        #endregion
    }
}
