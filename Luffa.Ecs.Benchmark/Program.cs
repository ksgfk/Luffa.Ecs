// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Luffa.Ecs.Benchmark;

BenchmarkSwitcher benchmark = BenchmarkSwitcher.FromTypes(new[]{
    typeof(ChunkAndArrayOneCom),
    typeof(ChunkAndArrayTwoCom),
    typeof(ChunkAndArrayThreeCom),
    typeof(ChunkAndArrayFourCom)});

if (args.Length > 0)
{
    benchmark.Run(args);
}
else
{
    benchmark.RunAll();
}