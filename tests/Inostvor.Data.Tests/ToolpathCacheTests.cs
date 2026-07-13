using Inostvor.Core.Model.Project;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Data.Project;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Data.Tests;

/// <summary>Cache je ubrzanje, nikad izvor istine (ADR-006): valjan → koristi se, nevaljan → tiho odbačen.</summary>
public sealed class ToolpathCacheTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("inostvor_cache_").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string WriteDxf(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static ToolpathProgram Program()
    {
        var seq = new CutSequence(3, new Point2(0, 0),
        [
            new CutMove(new LineSeg(new Point2(0, 0), new Point2(10, 0)), MoveKind.Cut, 3000),
            new CutMove(new ArcSeg(new Point2(10, 5), 5, -Math.PI / 2, Math.PI), MoveKind.Cut, 3000),
        ]);
        var stats = new ToolpathStatistics(seq.CutLength, 0, 1, 0, 0.6, 1);
        return new ToolpathProgram([seq], [new RapidMove(new Point2(0, 0), new Point2(0, 0))],
            TechnologySettings.Default, stats);
    }

    private ProjectDocument Document(string dxfPath, TechnologySettings? tech = null) => new()
    {
        Name = "Cache test",
        DxfSources = [ProjectDxfSource.Create("a.dxf", dxfPath, CacheKey.HashFile(dxfPath))],
        Technology = tech ?? TechnologySettings.Default,
    };

    [Fact]
    public async Task ValjanCache_VracaSe_SLukovimaILinijama()
    {
        var dxf = WriteDxf("a.dxf", "SADRZAJ");
        var doc = Document(dxf);
        var cache = new ToolpathCache(
            CacheKey.ComputeInputHash(doc.DxfSources, doc.Technology), CacheKey.PipelineVersion, Program());
        var path = Path.Combine(_dir, "p.ino");

        await new ProjectStore([]).SaveAsync(doc, path, cache);
        var loaded = await new ProjectStore([]).LoadAsync(path);

        loaded.Cache.ShouldNotBeNull();
        var moves = loaded.Cache.Program.Sequences.ShouldHaveSingleItem().Moves;
        moves.Count.ShouldBe(2);
        moves[0].Geometry.ShouldBeOfType<LineSeg>().EndPoint.ShouldBe(new Point2(10, 0));
        var arc = moves[1].Geometry.ShouldBeOfType<ArcSeg>(); // polimorfizam preživio JSON
        arc.Radius.ShouldBe(5);
        arc.Center.ShouldBe(new Point2(10, 5));
    }

    [Fact]
    public async Task PromijenjenDxf_CacheOdbacen()
    {
        var dxf = WriteDxf("a.dxf", "ORIGINAL");
        var doc = Document(dxf);
        var cache = new ToolpathCache(
            CacheKey.ComputeInputHash(doc.DxfSources, doc.Technology), CacheKey.PipelineVersion, Program());
        var path = Path.Combine(_dir, "p.ino");
        await new ProjectStore([]).SaveAsync(doc, path, cache);

        // Simulacija izmjene DXF-a: hash u dokumentu više ne odgovara cacheu.
        var changedDxf = WriteDxf("b.dxf", "IZMIJENJENO");
        var changedDoc = doc with
        {
            DxfSources = [ProjectDxfSource.Create("a.dxf", changedDxf, CacheKey.HashFile(changedDxf))],
        };
        var path2 = Path.Combine(_dir, "p2.ino");
        await new ProjectStore([]).SaveAsync(changedDoc, path2, cache); // stari (nevaljan) cache

        var loaded = await new ProjectStore([]).LoadAsync(path2);

        loaded.Cache.ShouldBeNull(); // hash se ne poklapa → regeneracija
    }

    [Fact]
    public async Task PromijenjenaTehnologija_CacheOdbacen()
    {
        var dxf = WriteDxf("a.dxf", "SADRZAJ");
        var doc = Document(dxf);
        var cache = new ToolpathCache(
            CacheKey.ComputeInputHash(doc.DxfSources, doc.Technology), CacheKey.PipelineVersion, Program());

        // Isti DXF, DRUGI kerf → drugi hash ulaza.
        var changed = doc with { Technology = doc.Technology with { KerfWidth = 2.5 } };
        var path = Path.Combine(_dir, "p.ino");
        await new ProjectStore([]).SaveAsync(changed, path, cache);

        (await new ProjectStore([]).LoadAsync(path)).Cache.ShouldBeNull();
    }

    [Fact]
    public async Task StaraVerzijaCjevovoda_CacheOdbacen()
    {
        var dxf = WriteDxf("a.dxf", "SADRZAJ");
        var doc = Document(dxf);
        var stale = new ToolpathCache(
            CacheKey.ComputeInputHash(doc.DxfSources, doc.Technology),
            CacheKey.PipelineVersion - 1, // stariji algoritam
            Program());
        var path = Path.Combine(_dir, "p.ino");
        await new ProjectStore([]).SaveAsync(doc, path, stale);

        (await new ProjectStore([]).LoadAsync(path)).Cache.ShouldBeNull();
    }

    [Fact]
    public async Task BezCachea_ProjektSeOtvaraNormalno()
    {
        var dxf = WriteDxf("a.dxf", "SADRZAJ");
        var path = Path.Combine(_dir, "p.ino");
        await new ProjectStore([]).SaveAsync(Document(dxf), path, cache: null);

        var loaded = await new ProjectStore([]).LoadAsync(path);

        loaded.Cache.ShouldBeNull();
        loaded.Document.Name.ShouldBe("Cache test");
    }

    [Fact]
    public void InputHash_Deterministican_IOsjetljivNaSvakiUlaz()
    {
        var dxf = WriteDxf("a.dxf", "X");
        var sources = new[] { ProjectDxfSource.Create("a.dxf", dxf, CacheKey.HashFile(dxf)) };
        var tech = TechnologySettings.Default;

        var a = CacheKey.ComputeInputHash(sources, tech);
        var b = CacheKey.ComputeInputHash(sources, tech);
        a.ShouldBe(b); // deterministički

        CacheKey.ComputeInputHash(sources, tech with { KerfWidth = 9.9 }).ShouldNotBe(a);
        CacheKey.ComputeInputHash(sources, tech with { CutOrderStrategyId = "nearest-neighbor" }).ShouldNotBe(a);
        CacheKey.ComputeInputHash(sources, tech with { EnableArcFitting = false }).ShouldNotBe(a);
        CacheKey.ComputeInputHash(sources, tech with { LeadInLength = 7 }).ShouldNotBe(a);
    }
}
