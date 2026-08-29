using System.IO;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Fallout.Persistence.Solution.Serializer;

namespace Fallout.Persistence.Solution.Benchmarks;

// Baseline benchmarks for the .slnx parser inlined from Microsoft's
// vs-solutionpersistence (see #248). Sweeps project count × folder layout
// to characterise the scaling shape. Numbers from these inform whether a
// proper XmlSerializer-based rewrite would be worth the effort.
//
// Each iteration measures `serializer.OpenAsync(path, ct)` end-to-end on a
// pre-staged temp .slnx file. Fixture generation is in `[GlobalSetup]` so
// its cost is excluded from measurements.
[MemoryDiagnoser] // alloc + GC pressure are interesting alongside time
[BenchmarkCategory("slnx", "parse")]
public class SlnxParseBenchmarks
{
    [Params(1, 10, 100, 1000)]
    public int ProjectCount;

    [Params(false, true)]
    public bool WithFolders;

    private string _fixturePath = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var content = SlnxFixtureBuilder.Build(ProjectCount, WithFolders);
        _fixturePath = SlnxFixtureBuilder.WriteToTempFile(content);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_fixturePath))
        {
            File.Delete(_fixturePath);
        }
    }

    [Benchmark]
    public object ParseSlnx()
    {
        // Equivalent to Fallout.Solutions.SolutionModelExtensions.ReadSolution(),
        // minus the AbsolutePath + facade-wrap layer — measures the parser path
        // in isolation. .GetAwaiter().GetResult() rather than AsyncHelper because
        // AsyncHelper is internal to Fallout.Utilities and we don't want to
        // [InternalsVisibleTo] a benchmark project just for one call.
        var serializer = SolutionSerializers.GetSerializerByMoniker(_fixturePath);
        return serializer!.OpenAsync(_fixturePath, CancellationToken.None).GetAwaiter().GetResult();
    }
}
