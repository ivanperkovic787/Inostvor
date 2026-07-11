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
        // Dvije polukružnice čije se kružnice sijeku: spojene linijama u otvorenu polyline.
        // A0: gornja polukružnica oko (0,0) r=2: (2,0)→(-2,0).
        // L1: (-2,0)→(-1,-2).  A2: gornja polukružnica oko (1,-2) r=2: (-1,-2)→(3,-2).
        // Kružnica A2 (centar (1,-2), r=2) siječe kružnicu A0 (centar (0,0), r=2);
        // presjeci obiju KRUŽNICA: računamo i provjeravamo da je barem jedan na oba LUKA.
        var p = new Polyline2([
            new ArcSeg(new Point2(0, 0), 2.0, 0.0, Math.PI),
            new LineSeg(new Point2(-2, 0), new Point2(-1, -2)),
            new ArcSeg(new Point2(1, -2), 2.0, Math.PI, -Math.PI), // CW od (-1,-2) do (3,-2): gornja polovica
        ]);

        // Gornja polovica kružnice oko (1,-2) doseže do y = 0 (vrh u (1,0)) i prolazi kroz
        // (1-√3·? ) ... presjeci kružnica: d=√5, računamo samo da hit postoji i da je na luku 0 i 2.
        var hits = PolylineSelfIntersection.Find(p);

        hits.ShouldNotBeEmpty();
        hits.All(h => h.SegmentA == 0 && h.SegmentB == 2).ShouldBeTrue();
        foreach (var h in hits)
        {
            h.Point.DistanceTo(new Point2(0, 0)).ShouldBe(2.0, 1e-6);
            h.Point.DistanceTo(new Point2(1, -2)).ShouldBe(2.0, 1e-6);
        }
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
