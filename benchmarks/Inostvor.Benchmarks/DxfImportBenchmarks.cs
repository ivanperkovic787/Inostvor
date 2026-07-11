using BenchmarkDotNet.Attributes;
using Inostvor.Import.NetDxf;

namespace Inostvor.Benchmarks;

/// <summary>
/// End-to-end import 2001 entiteta (grid 50×40 krugova + okvir) — čuva prag
/// performansi cijelog lanca netDxf parse → mapiranje → transformacije.
/// </summary>
[MemoryDiagnoser]
public class DxfImportBenchmarks
{
    private string _path = null!;

    [GlobalSetup]
    public void Setup()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Inostvor.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Korijen repozitorija nije pronađen.");
        }

        _path = Path.Combine(dir.FullName, "tests", "TestData", "LargeFiles", "grid_2000_circles.dxf");
    }

    [Benchmark]
    public int ImportGrid2000Circles()
    {
        var result = new NetDxfImporter().Import(_path);
        return result.Entities.Count;
    }
}
