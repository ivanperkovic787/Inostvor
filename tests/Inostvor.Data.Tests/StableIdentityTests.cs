using Inostvor.Core.Model;
using Inostvor.Core.Model.Library;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Project;
using Inostvor.Data.Project;
using Shouldly;
using Xunit;

namespace Inostvor.Data.Tests;

/// <summary>ADR-006: trajni objekti imaju stabilne UUID-e od V1.</summary>
public sealed class StableIdentityTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("inostvor_id_").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void SviTrajniObjekti_ImplementirajuIIdentifiable()
    {
        typeof(IIdentifiable).IsAssignableFrom(typeof(MachineProfile)).ShouldBeTrue();
        typeof(IIdentifiable).IsAssignableFrom(typeof(TechnologyEntry)).ShouldBeTrue();
    }

    [Fact]
    public void NoviObjekti_DobivajuJedinstveneIdjeve()
    {
        var a = new MachineProfile { Name = "A", PostProcessorId = "x" };
        var b = new MachineProfile { Name = "A", PostProcessorId = "x" };

        a.Id.ShouldNotBe(Guid.Empty);
        a.Id.ShouldNotBe(b.Id);
    }

    [Fact]
    public void Preimenovanje_CuvaIdentitet()
    {
        var original = new TechnologyEntry { Name = "Steel 2 mm" };
        var renamed = original with { Name = "Čelik 2 mm" };

        renamed.Id.ShouldBe(original.Id); // record 'with' čuva Id
    }

    [Fact]
    public async Task ProjektIIzvori_ZadrzavajuIdKrozRoundTrip()
    {
        var dxfPath = Path.Combine(_dir, "a.dxf");
        await File.WriteAllTextAsync(dxfPath, "X");

        var source = ProjectDxfSource.Create("a.dxf", dxfPath, CacheKey.HashFile(dxfPath));
        var machine = new MachineProfile { Name = "M", PostProcessorId = "inostvor.post.mach3" };
        var technologyId = Guid.NewGuid();
        var doc = new ProjectDocument
        {
            Name = "P",
            DxfSources = [source],
            Machine = machine,
            TechnologyId = technologyId,
        };

        var path = Path.Combine(_dir, "p.ino");
        await new ProjectStore([]).SaveAsync(doc, path);
        var loaded = await new ProjectStore([]).LoadAsync(path);

        loaded.Document.Id.ShouldBe(doc.Id);
        loaded.Document.Machine.Id.ShouldBe(machine.Id);
        loaded.Document.TechnologyId.ShouldBe(technologyId);
        loaded.Document.DxfSources.ShouldHaveSingleItem().Id.ShouldBe(source.Id);
    }
}
