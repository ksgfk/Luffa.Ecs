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


|        Method |     Mean |   Error |  StdDev | Code Size |
|-------------- |---------:|--------:|--------:|----------:|
|    ThreeArray | 191.8 ms | 3.07 ms | 2.72 ms |   1,503 B |
| ChunkWith3Com | 213.3 ms | 0.74 ms | 0.57 ms |      40 B |
     */
    [DisassemblyDiagnoser]
    public class ChunkAndArrayThreeCom
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

        const int TestCount = 5_000_000;
        static float DeltaTime = 0.1425f;

        #region Array
        Transform[] _t;
        Move[] _m;
        Rotate[] _r;
        [GlobalSetup(Target = nameof(ThreeArray))]
        public void GetOriginalData()
        {
            Random rand = new Random();
            _t = new Transform[TestCount];
            _m = new Move[TestCount];
            _r = new Rotate[TestCount];
            for (int i = 0; i < TestCount; i++)
            {
                _t[i].Position = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _t[i].Rotation = Matrix4x4.CreateRotationY((float)rand.NextDouble());
                _m[i].Velocity = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                _r[i].Angle = (float)rand.NextDouble();
                _r[i].Axis = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
            }
        }
        [Benchmark]
        public void ThreeArray()
        {
            var count = _t.Length;
            for (int i = 0; i < count; i++)
            {
                ref var t = ref _t[i];
                ref var m = ref _m[i];
                ref var r = ref _r[i];
                t.Position += m.Velocity * DeltaTime;
                t.Rotation = Matrix4x4.Multiply(t.Rotation, Matrix4x4.CreateFromAxisAngle(r.Axis, r.Angle));
            }
        }
        #endregion

        #region Chunk
        #region JIT_Result
        /* .NET 6.0.3 (6.0.322.12309), X64 RyuJIT */
        /*
        ; Luffa.Ecs.Benchmark.ChunkAndArrayThreeCom.ChunkWith3Com()
               push      r15
               push      r14
               push      rdi
               push      rsi
               push      rbp
               push      rbx
               sub       rsp,248
               vzeroupper
               vmovaps   [rsp+230],xmm6
               vmovaps   [rsp+220],xmm7
               xor       eax,eax
               mov       [rsp+138],rax
               vxorps    xmm4,xmm4,xmm4
               vmovdqa   xmmword ptr [rsp+140],xmm4
               vmovdqa   xmmword ptr [rsp+150],xmm4
               mov       rax,0FFFFFFFFFF40
        M00_L00:
               vmovdqa   xmmword ptr [rsp+rax+220],xmm4
               vmovdqa   xmmword ptr [rsp+rax+230],xmm4
               vmovdqa   xmmword ptr [rsp+rax+240],xmm4
               add       rax,30
               jne       short M00_L00
               mov       rsi,rcx
               mov       rcx,[rsi+8]
               mov       rax,rcx
               mov       rdi,[rax+10]
               call      00007FFE0A022E08
               mov       ebx,eax
               mov       rcx,[rsi+8]
               cmp       [rcx],ecx
               call      00007FFE0A023C50
               mov       ebp,eax
               mov       rcx,[rsi+8]
               cmp       [rcx],ecx
               call      Luffa.Ecs.EntityMemory.GetUnmanagedComponentLocator[[Luffa.Ecs.Benchmark.ChunkAndArrayThreeCom+Rotate, Luffa.Ecs.Benchmark]]()
               mov       esi,eax
               mov       rcx,[rdi+10]
               mov       [rsp+150],rcx
               mov       eax,[rdi+38]
               mov       [rsp+158],eax
               mov       eax,[rdi+40]
               mov       [rsp+15C],eax
               xor       eax,eax
               mov       [rsp+160],eax
               cmp       dword ptr [rcx+10],0
               jbe       near ptr M00_L05
               mov       rcx,[rcx+8]
               cmp       dword ptr [rcx+8],0
               jbe       near ptr M00_L06
               mov       rcx,[rcx+10]
               mov       [rsp+168],rcx
               mov       dword ptr [rsp+170],0FFFFFFFF
               mov       dword ptr [rsp+174],0FFFFFFFF
               vmovdqu   xmm0,xmmword ptr [rsp+150]
               vmovdqu   xmmword ptr [rsp+1F8],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+160]
               vmovdqu   xmmword ptr [rsp+208],xmm0
               mov       rcx,[rsp+170]
               mov       [rsp+218],rcx
               jmp       near ptr M00_L02
        M00_L01:
               lea       rcx,[rsp+210]
               mov       rdx,[rcx]
               mov       ecx,[rcx+8]
               xor       eax,eax
               mov       [rsp+148],rax
               mov       eax,[rdx+8]
               cmp       ebx,eax
               jae       near ptr M00_L06
               movsxd    r8,ebx
               lea       r8,[rdx+r8+10]
               mov       [rsp+148],r8
               mov       rdi,[rsp+148]
               imul      r8d,ecx,4C
               movsxd    r14,r8d
               add       rdi,r14
               xor       r8d,r8d
               mov       [rsp+148],r8
               mov       [rsp+140],r8
               cmp       ebp,eax
               jae       near ptr M00_L06
               movsxd    r8,ebp
               lea       r8,[rdx+r8+10]
               mov       [rsp+140],r8
               mov       r8,[rsp+140]
               lea       r9d,[rcx+rcx*2]
               shl       r9d,2
               movsxd    r9,r9d
               add       r8,r9
               xor       r9d,r9d
               mov       [rsp+140],r9
               mov       [rsp+138],r9
               cmp       esi,eax
               jae       near ptr M00_L06
               movsxd    rax,esi
               lea       rdx,[rdx+rax+10]
               mov       [rsp+138],rdx
               mov       r14,[rsp+138]
               shl       ecx,4
               movsxd    r15,ecx
               add       r14,r15
               mov       [rsp+138],r9
               vmovss    xmm0,dword ptr [rdi+8]
               vmovsd    xmm6,qword ptr [rdi]
               vshufps   xmm6,xmm6,xmm0,44
               vmovss    xmm0,dword ptr [r8+8]
               vmovsd    xmm7,qword ptr [r8]
               vshufps   xmm7,xmm7,xmm0,44
               mov       rcx,7FFE09F957F8
               mov       edx,5
               call      CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
               vmovss    xmm2,dword ptr [7FFE0A025838]
               vinsertps xmm2,xmm2,xmm2,0E
               vshufps   xmm2,xmm2,xmm2,40
               vmulps    xmm2,xmm7,xmm2
               vaddps    xmm2,xmm6,xmm2
               vmovsd    qword ptr [rdi],xmm2
               vpshufd   xmm0,xmm2,2
               vmovss    dword ptr [rdi+8],xmm0
               vmovdqu   xmm2,xmmword ptr [rdi+0C]
               vmovdqu   xmmword ptr [rsp+1B8],xmm2
               vmovdqu   xmm2,xmmword ptr [rdi+1C]
               vmovdqu   xmmword ptr [rsp+1C8],xmm2
               vmovdqu   xmm2,xmmword ptr [rdi+2C]
               vmovdqu   xmmword ptr [rsp+1D8],xmm2
               vmovdqu   xmm2,xmmword ptr [rdi+3C]
               vmovdqu   xmmword ptr [rsp+1E8],xmm2
               lea       rcx,[rsp+178]
               vmovss    xmm2,dword ptr [r14+8]
               vmovsd    xmm0,qword ptr [r14]
               vshufps   xmm0,xmm0,xmm2,44
               vmovapd   [rsp+0A0],xmm0
               lea       rdx,[rsp+0A0]
               vmovss    xmm2,dword ptr [r14+0C]
               call      00007FFE0A00BDE8
               vmovdqu   xmm0,xmmword ptr [rsp+1B8]
               vmovdqu   xmmword ptr [rsp+0F8],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+1C8]
               vmovdqu   xmmword ptr [rsp+108],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+1D8]
               vmovdqu   xmmword ptr [rsp+118],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+1E8]
               vmovdqu   xmmword ptr [rsp+128],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+178]
               vmovdqu   xmmword ptr [rsp+0B8],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+188]
               vmovdqu   xmmword ptr [rsp+0C8],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+198]
               vmovdqu   xmmword ptr [rsp+0D8],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+1A8]
               vmovdqu   xmmword ptr [rsp+0E8],xmm0
               lea       rcx,[rdi+0C]
               vmovdqu   xmm0,xmmword ptr [rsp+0F8]
               vmovdqu   xmmword ptr [rsp+60],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+108]
               vmovdqu   xmmword ptr [rsp+70],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+118]
               vmovdqu   xmmword ptr [rsp+80],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+128]
               vmovdqu   xmmword ptr [rsp+90],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+0B8]
               vmovdqu   xmmword ptr [rsp+20],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+0C8]
               vmovdqu   xmmword ptr [rsp+30],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+0D8]
               vmovdqu   xmmword ptr [rsp+40],xmm0
               vmovdqu   xmm0,xmmword ptr [rsp+0E8]
               vmovdqu   xmmword ptr [rsp+50],xmm0
               lea       rdx,[rsp+60]
               lea       r8,[rsp+20]
               call      00007FFE0A00BDB0
        M00_L02:
               lea       rcx,[rsp+210]
               lea       rax,[rcx+8]
               inc       dword ptr [rax]
               lea       rax,[rcx+0C]
               inc       dword ptr [rax]
               mov       eax,[rcx+8]
               cmp       eax,[rsp+200]
               jge       short M00_L03
               mov       ecx,[rcx+0C]
               cmp       ecx,[rsp+204]
               setl      dil
               movzx     edi,dil
               jmp       short M00_L04
        M00_L03:
               lea       rcx,[rsp+1F8]
               call      00007FFE0A023C40
               movzx     edi,al
        M00_L04:
               test      edi,edi
               jne       near ptr M00_L01
               vmovaps   xmm6,[rsp+230]
               vmovaps   xmm7,[rsp+220]
               add       rsp,248
               pop       rbx
               pop       rbp
               pop       rsi
               pop       rdi
               pop       r14
               pop       r15
               ret
        M00_L05:
               call      System.ThrowHelper.ThrowArgumentOutOfRange_IndexException()
               int       3
        M00_L06:
               call      CORINFO_HELP_RNGCHKFAIL
               int       3
        ; Total bytes of code 1127
        */
        #endregion
        [GlobalSetup(Target = nameof(ChunkWith3Com))]
        public void GetChunkData()
        {
            Random rand = new Random();
            var arch = EntityArchetype.Get(TypeInfo.Get<Transform>(), TypeInfo.Get<Move>(), TypeInfo.Get<Rotate>());
            _memory = new EntityMemory(arch);
            for (int i = 0; i < TestCount; i++)
            {
                var entity = _memory.Allocate(i);
                ref var t = ref _memory.GetUnmanagedComponent<Transform>(entity);
                ref var m = ref _memory.GetUnmanagedComponent<Move>(entity);
                ref var r = ref _memory.GetUnmanagedComponent<Rotate>(entity);
                t.Position = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                t.Rotation = Matrix4x4.CreateRotationY((float)rand.NextDouble());
                m.Velocity = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                r.Angle = (float)rand.NextDouble();
                r.Axis = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
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
            foreach (var index in viewer)
            {
                ref var t = ref trans.Locate(in index);
                ref readonly var m = ref move.Locate(in index);
                ref readonly var r = ref rot.Locate(in index);
                t.Position += m.Velocity * DeltaTime;
                t.Rotation = Matrix4x4.Multiply(t.Rotation, Matrix4x4.CreateFromAxisAngle(r.Axis, r.Angle));
            }
        }
        #endregion
    }
}
