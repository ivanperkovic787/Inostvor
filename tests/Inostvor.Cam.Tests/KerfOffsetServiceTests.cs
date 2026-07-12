using Inostvor.Cam.Offset;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;
using static Inostvor.Cam.Tests.CamTestHelpers;

namespace Inostvor.Cam.Tests;

public sealed class KerfOffsetServiceTests
{
    private static readonly KerfOffsetService Service = new();

    [Fact]
    public void VanjskiKrug_OffsetVan_PolumjerPlusPolaKerfa()
    {
        var contour = Contours(("0", [FullCircle(50, 50, 10)]))[0];

        var rings = Service.Offset(contour, kerfWidth: 2.0, tessellationTolerance: 0.01);

        var ring = rings.ShouldHaveSingleItem();
        var center = new Point2(50, 50);
        foreach (var p in ring)
        {
            center.DistanceTo(p).ShouldBe(11.0, 0.03); // r + kerf/2, unutar tessellacijske pogreške
        }
    }

    [Fact]
    public void Rupa_OffsetUnutra_PolumjerMinusPolaKerfa()
    {
        var contours = Contours(
            ("0", SquareLines(0, 0, 100)),
            ("0", [FullCircle(50, 50, 10)]));
        var hole = contours[1];

        var rings = Service.Offset(hole, kerfWidth: 2.0, tessellationTolerance: 0.01);

        var ring = rings.ShouldHaveSingleItem();
        var center = new Point2(50, 50);
        foreach (var p in ring)
        {
            center.DistanceTo(p).ShouldBe(9.0, 0.03); // r − kerf/2
        }
    }

    [Fact]
    public void VanjskiKvadrat_GraniceRastuZaPolaKerfa()
    {
        var contour = Contours(("0", SquareLines(0, 0, 10)))[0];

        var ring = Service.Offset(contour, 2.0, 0.01).ShouldHaveSingleItem();

        ring.Min(p => p.X).ShouldBe(-1.0, 0.02);
        ring.Max(p => p.X).ShouldBe(11.0, 0.02);
        ring.Min(p => p.Y).ShouldBe(-1.0, 0.02);
        ring.Max(p => p.Y).ShouldBe(11.0, 0.02);
    }

    [Fact]
    public void KerfNula_PutanjaJednakaGeometriji()
    {
        var contour = Contours(("0", SquareLines(0, 0, 10)))[0];

        var ring = Service.Offset(contour, 0.0, 0.01).ShouldHaveSingleItem();

        ring.Count.ShouldBe(4);
        ring[0].ShouldBe(new Point2(0, 0)); // normalizacija: leksikografski minimum prvi
    }

    [Fact]
    public void OtvorenaKontura_SredisnjicaBezKerfa()
    {
        var contour = Contours(("0", [L(0, 0, 100, 0)]))[0];

        var ring = Service.Offset(contour, 2.0, 0.01).ShouldHaveSingleItem();

        ring.Count.ShouldBe(2);
        ring[0].ShouldBe(new Point2(0, 0));
        ring[1].ShouldBe(new Point2(100, 0));
    }

    [Fact]
    public void Determinizam_PetPokretanja_BajtIdentican()
    {
        var contours = Contours(
            ("0", SquareLines(0, 0, 100)),
            ("0", [FullCircle(30, 30, 8)]),
            ("0", [FullCircle(70, 70, 8)]));

        var reference = contours.Select(c => Service.Offset(c, 1.6, 0.01)).ToList();

        for (var run = 0; run < 5; run++)
        {
            for (var ci = 0; ci < contours.Count; ci++)
            {
                var rings = Service.Offset(contours[ci], 1.6, 0.01);
                rings.Count.ShouldBe(reference[ci].Count);
                for (var ri = 0; ri < rings.Count; ri++)
                {
                    rings[ri].Count.ShouldBe(reference[ci][ri].Count);
                    for (var pi = 0; pi < rings[ri].Count; pi++)
                    {
                        rings[ri][pi].ShouldBe(reference[ci][ri][pi]); // egzaktna jednakost doubleova
                    }
                }
            }
        }
    }

    [Fact]
    public void Normalizacija_PrviElementJeLeksikografskiMinimum()
    {
        var contour = Contours(("0", [FullCircle(50, 50, 10)]))[0];

        var ring = Service.Offset(contour, 2.0, 0.01).ShouldHaveSingleItem();

        var min = ring.OrderBy(p => p.X).ThenBy(p => p.Y).First();
        ring[0].ShouldBe(min);
    }
}
