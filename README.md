<h1 align="center">Luffa.Ecs</h1>
<p align="center">轻量级EntityComponentSystem(ECS)框架</p>

## Introduction

**Luffa.Ecs**是C#（目标框架.Net Standard 2.1）编写的，平台无关的，基于Chunk和Archetype的轻量级ECS框架

（其实是复刻Unity DOTS，虽然API名字不一样）

## Dependencies

**Luffa.Ecs** 只依赖于：[System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe)，因为.Net Standard 2.1不包含这个库

**Luffa.Ecs.Test** 使用MSTest

**Luffa.Ecs.Benchmark** 使用[BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

对于开发来说，**Luffa.Ecs.Test**和**Luffa.Ecs.Benchmark**都需要.Net 6环境

## Example

```c#
using Luffa.Ecs;
using System;
using System.Numerics;

public static class Time
{
    public static float DeltaTime = 0.14251f; //hard code
}

public struct Transform : IComponent
{
    public Vector3 Position;
}
public struct Move : IComponent
{
    public Vector3 Velocity;
}

public class MoveSystem : SystemBase
{
    public override IEntityFilter Filter { get; } = EntityFilter.FromRequire(
        TypeInfo.Get<Transform>(),
        TypeInfo.Get<Move>()
    );
    
    protected override void UpdateEntity(World world, EntityMemory memory, CmdBuffer cmd)
    {
        var view = GetComponentViewer(memory);
        var trans = GetUnmanagedLocator<Transform>(memory);
        var mv = GetUnmanagedLocator<Move>(memory);
        foreach (var i in view)
        {
            ref Transform t = ref trans.Locate(in i);
            ref readonly Move m = ref mv.Locate(in i);
            t.Position += m.Velocity * Time.DeltaTime;
        }
    }
}

internal static class Program
{
    private static void Main(String[] args)
    {
        World world = new World();
        EntityArchetype canMove = EntityArchetype.Get(TypeInfo.Get<Transform>(), TypeInfo.Get<Move>());
        world.CreateEntity(canMove);
        world.AddSystem<MoveSystem>();
        world.OnUpdate();
    }
}
```

## TODO

* Singleton Component

## Detail

### Component Storage

C#的类型可以大致划分成**managed**和**unmanaged**两类。关于非托管类型有哪些，[Microsoft文档](https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/builtin-types/unmanaged-types)写的很清楚

非托管类型与C++中的trivial type非常类似，直接对它们进行memcpy完全没有问题。

对于非托管类型，储存方式与Unity DOTS非常类似（不能说类似，只能说一模一样）。[Unity文档](https://docs.unity3d.com/Packages/com.unity.entities@0.50/manual/ecs_components.html#archetypes-and-chunks)

对于托管类型，储存方式与Unity DOTS不太一样。一种原型拥有的所有托管类型，都各自拥有一个独立的数组，而不是像Unity DOTS存放在World中的大数组内，因为托管类型可以是class和struct，这样储存对于那些虽然是struct但内部有托管类的情况或许会好一些

虽然基于Archetype有很多好处，但它也是有缺陷的，在添加和删除实体时会对组件进行复制操作，这样会比其他结构的增删慢

### Safety

安全检查很薄弱，或者说根本没有，全靠用户自觉，一不小心就会枚举器失效，而且不会有任何提示

**绝对不可以在枚举Entity的时候调用可以改变World结构的方法，包括增删Entity，增删Entity拥有的Component**

可以使用`CmdBuffer`来更改实体

### About Filter

现有的EntityFilter很简单，只能暴力匹配需要的（Require）和不可以存在的（Exclude）原型，大概够用了？

### About Multi-Thread

没有多线程执行System，还不知道怎么做出无锁并行执行系统

### About Untiy Engine Integrate

（咕？

### About Name

[Luffa](https://en.wikipedia.org/wiki/Luffa)是丝瓜这种蔬菜的意思，因为丝瓜瓤的网状结构，会让人联想到游戏编程中那些错从复杂的依赖关系

## License

MIT