using Inostvor.Core.Model.Import;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Import.NetDxf.Tests;

public sealed class NetDxfImporterCurveAndInvalidTests
{
    private static ImportResult Import(string category, string file)
        => new NetDxfImporter().Import(TestDataLocator.Get(category, file));

    [Fact]
    public void Spline_TesselliraSe_KrajeviNaFitTockama()
    {
        var r = Import("Decorative", "spline_wave.dxf");

        r.Success.ShouldBeTrue(r.Error);
        r.Warnings.ShouldContain(w => w.Code == ImportWarningCodes.SplineTessellated);

        var e = r.Entities.ShouldHaveSingleItem();
        e.Segments.Count.ShouldBeGreaterThan(20);
        e.Segments.All(s => s is LineSeg).ShouldBeTrue();
        e.Segments[0].StartPoint.AlmostEquals(new Point2(0, 0), 0.01).ShouldBeTrue();
        e.Segments[^1].EndPoint.AlmostEquals(new Point2(80, 0), 0.01).ShouldBeTrue();
    }

    [Fact]
    public void ZatvoreniSpline_LanacSeZatvara()
    {
        var r = Import("Decorative", "spline_closed.dxf");

        var e = r.Entities.ShouldHaveSingleItem();
        e.Segments[^1].EndPoint.AlmostEquals(e.Segments[0].StartPoint, 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void PunaElipsa_ZatvorenaISOcekivanimGabaritima()
    {
        var r = Import("Decorative", "ellipse_full.dxf");

        r.Warnings.ShouldContain(w => w.Code == ImportWarningCodes.EllipseTessellated);
        var e = r.Entities.ShouldHaveSingleItem();
        e.Segments[^1].EndPoint.AlmostEquals(e.Segments[0].StartPoint, 1e-6).ShouldBeTrue();

        // Centar (50,30), polu-osi 30 i 15 (ezdxf major_axis = vektor POLU-osi, ratio 0.5).
        var bounds = Aabb.FromPoints(e.Segments.SelectMany(s => new[] { s.StartPoint, s.EndPoint }));
        bounds.MinX.ShouldBe(20.0, 0.05);
        bounds.MaxX.ShouldBe(80.0, 0.05);
        bounds.MinY.ShouldBe(15.0, 0.05);
        bounds.MaxY.ShouldBe(45.0, 0.05);
    }

    [Fact]
    public void ElipticniLuk_PolaElipse()
    {
        var r = Import("Decorative", "ellipse_arc.dxf");

        var e = r.Entities.ShouldHaveSingleItem();
        // Pola elipse (0→π): od (40,0) do (-40,0), vrh na (0,16).
        e.Segments[0].StartPoint.AlmostEquals(new Point2(40, 0), 0.05).ShouldBeTrue();
        e.Segments[^1].EndPoint.AlmostEquals(new Point2(-40, 0), 0.05).ShouldBeTrue();

        var maxY = e.Segments.SelectMany(s => new[] { s.StartPoint, s.EndPoint }).Max(p => p.Y);
        maxY.ShouldBe(16.0, 0.05);
    }

    [Fact]
    public void R12_JasnaGreskaBezIznimke()
    {
        var r = Import("Invalid", "r12_rectangle.dxf");

        r.Success.ShouldBeFalse();
        r.Error.ShouldNotBeNull();
        r.Error.ShouldContain("2000"); // poruka mora uputiti na minimalnu podržanu verziju
    }

    [Fact]
    public void NijeDxf_Fail()
    {
        var r = Import("Invalid", "not_a_dxf.dxf");
        r.Success.ShouldBeFalse();
    }

    [Fact]
    public void OdrezanaDatoteka_Fail()
    {
        var r = Import("Invalid", "truncated.dxf");
        r.Success.ShouldBeFalse();
    }

    [Fact]
    public void NepostojecaDatoteka_Fail()
    {
        var r = new NetDxfImporter().Import(TestDataLocator.Get("Invalid", "ne_postoji.dxf"));
        r.Success.ShouldBeFalse();
    }

    [Fact]
    public void DegeneriraniEntiteti_PreskocnjeniUzUpozorenja_ValidanPrezivi()
    {
        // Datoteka sadrži 3 degenerirana entiteta (linija nulte duljine, polilinija s
        // jednom točkom, polilinija s dvije identične točke) + 1 validnu liniju 50 mm.
        // NAPOMENA: ARC s jednakim start/end kutom NIJE degeneracija — importer ga
        // namjerno tretira kao PUNI KRUG (M2 odluka: mnogi CAD-ovi tako pišu krugove).
        var r = Import("Invalid", "degenerate_entities.dxf");

        r.Success.ShouldBeTrue(r.Error);
        r.Entities.Count.ShouldBe(1); // samo validna linija (0,0)→(50,0) preživi
        r.Entities[0].Segments.ShouldHaveSingleItem().Length.ShouldBe(50.0, 1e-6);
        r.Warnings.Count(w => w.Code == ImportWarningCodes.DegenerateEntity).ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void VelikaDatoteka_2000Krugova_UcitavaSe()
    {
        var r = Import("LargeFiles", "grid_2000_circles.dxf");

        r.Success.ShouldBeTrue(r.Error);
        r.Entities.Count.ShouldBe(2001); // 2000 krugova + okvir
    }
}
