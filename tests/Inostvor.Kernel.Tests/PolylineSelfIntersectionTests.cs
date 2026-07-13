using Inostvor.Kernel.Intersections;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class PolylineSelfIntersectionTests
{
    [Fact]
    public void MasnaKravata_JedanSamopresjek()
    {
        // (0,0)→(2,2)→(2,0)→(0,2): segment 0 i segment 2 križaju se u (1,1).
        var p = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(2, 2)),
            new LineSeg(new Point2(2, 2), new Point2(2, 0)),
            new LineSeg(new Point2(2, 0), new Point2(0, 2)),
        ]);

        var hits = PolylineSelfIntersection.Find(p);

        hits.Count.ShouldBe(1);
        hits[0].SegmentA.ShouldBe(0);
        hits[0].SegmentB.ShouldBe(2);
        hits[0].Point.AlmostEquals(new Point2(1, 1), 1e-9).ShouldBeTrue();
    }

    [Fact]
    public void ZatvoreniKvadrat_BezSamopresjeka()
    {
        var square = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(2, 0)),
            new LineSeg(new Point2(2, 0), new Point2(2, 2)),
            new LineSeg(new Point2(2, 2), new Point2(0, 2)),
            new LineSeg(new Point2(0, 2), new Point2(0, 0)),
        ]);

        PolylineSelfIntersection.Find(square).ShouldBeEmpty();
    }

    [Fact]
    public void SusjedniSegmenti_ZajednickiVrhNijeSamopresjek()
    {
        var zigzag = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(1, 1)),
            new LineSeg(new Point2(1, 1), new Point2(2, 0)),
            new LineSeg(new Point2(2, 0), new Point2(3, 1)),
        ]);

        PolylineSelfIntersection.Find(zigzag).ShouldBeEmpty();
    }

    [Fact]
    public void LukTangencijalnoDodirujeLiniju_DetektiraSe()
    {
        // L0: (0,0)→(4,0); L1: (4,0)→(4,2); A2: CW luk od (4,2) do (0,2) oko (2,2), r=2 —
        // luk dolje dodiruje L0 tangencijalno u (2,0). Par (0,2) nije susjedan → samopresjek.
        var p = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(4, 0)),
            new LineSeg(new Point2(4, 0), new Point2(4, 2)),
            ArcSeg.FromStartEndCenter(new Point2(4, 2), new Point2(0, 2), new Point2(2, 2), isCcw: false),
        ]);

        var hits = PolylineSelfIntersection.Find(p);

        hits.Count.ShouldBe(1);
        hits[0].SegmentA.ShouldBe(0);
        hits[0].SegmentB.ShouldBe(2);
        hits[0].Point.AlmostEquals(new Point2(2, 0), 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void DvaLukaSeSijeku_UnutarPolyline()
    {
        // Dvije GORNJE polukružnice jednakog polumjera r=2, centri (0,0) i (2,0).
        // Kružnice se sijeku u (1, ±√3); GORNJI presjek (1, +√3) leži na OBA luka.
        //   A0: centar (0,0), start 0°, sweep +180° (CCW)  → gornja polovica, (2,0) → (-2,0)
        //   L1: (-2,0) → (0,0)  — spojnica koja lanac vodi do početka A2
        //   A2: centar (2,0), start 180°, sweep -180° (CW) → gornja polovica, (0,0) → (4,0)
        var p = new Polyline2([
            new ArcSeg(new Point2(0, 0), 2.0, 0.0, Math.PI),
            new LineSeg(new Point2(-2, 0), new Point2(0, 0)),
            new ArcSeg(new Point2(2, 0), 2.0, Math.PI, -Math.PI),
        ]);

        var hits = PolylineSelfIntersection.Find(p);

        // Točno jedan presjek, s EGZAKTNO poznatom pozicijom (1, √3).
        var hit = hits.ShouldHaveSingleItem();
        hit.SegmentA.ShouldBe(0);
        hit.SegmentB.ShouldBe(2);
        hit.Point.X.ShouldBe(1.0, 1e-9);
        hit.Point.Y.ShouldBe(Math.Sqrt(3.0), 1e-9);
    }

    [Fact]
    public void KruzniceSeSijeku_AliLukoviNe_PrazanRezultat()
    {
        // KLJUČNI rubni slučaj: pune kružnice se sijeku, ali presjeci NE leže na lukovima.
        // Centri (0,0) i (1,-2), oba r=2 → kružnice se sijeku u (-0.983, -1.742) i (1.983, -0.258).
        // Oba presjeka su na DONJIM polovicama kružnice A0, a luk A0 pokriva samo gornju.
        // Detektor MORA odbaciti oba — inače bi CAM prijavljivao lažne samopresjeke.
        var p = new Polyline2([
            new ArcSeg(new Point2(0, 0), 2.0, 0.0, Math.PI),            // gornja polovica oko (0,0)
            new LineSeg(new Point2(-2, 0), new Point2(-1, -2)),
            new ArcSeg(new Point2(1, -2), 2.0, Math.PI, -Math.PI),      // gornja polovica oko (1,-2)
        ]);

        PolylineSelfIntersection.Find(p).ShouldBeEmpty();
    }

    [Fact]
    public void VelikiZigzagBezPresjeka_PrazanRezultat()
    {
        // Performantni smoke-test oblika: 500 segmenata cik-cak, nula presjeka.
        var segments = new List<ISegment>();
        for (var i = 0; i < 500; i++)
        {
            segments.Add(new LineSeg(new Point2(i, i % 2), new Point2(i + 1, (i + 1) % 2)));
        }

        var p = new Polyline2(segments);
        PolylineSelfIntersection.Find(p).ShouldBeEmpty();
    }
}
