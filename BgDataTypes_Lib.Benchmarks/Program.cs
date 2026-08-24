using BenchmarkDotNet.Running;
using BgDataTypes_Lib.Benchmarks;

// Entry point for the benchmark harness. Run the whole set with
//   dotnet run -c Release --project BgDataTypes_Lib.Benchmarks
// or filter, e.g.
//   dotnet run -c Release --project BgDataTypes_Lib.Benchmarks -- --filter *FourMoves*
// Add --disasm to inspect the generated code (inlining checks).
BenchmarkSwitcher.FromAssembly(typeof(PlayConstructionBenchmarks).Assembly).Run(args);
