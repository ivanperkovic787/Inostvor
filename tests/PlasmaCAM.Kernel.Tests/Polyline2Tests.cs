using PlasmaCAM.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class Polyline2Tests
{
    private static Polyline2 Square() => new([
        new LineSeg(new Point2(0, 0), new Point2(2, 0)),
        new LineSeg(new Point2(2, 0), new Point2(2, 2)),
        new LineSeg(new Point2(2, 2), new Point2(0, 2)),
        new LineSeg(new Point2(0, 2), new Point2(0, 0)),
    ]);

    [Fact]
    public void ZatvoreniKvadrat_SvojstvaTocna()
    {
        var p = Square();
        p.Count.ShouldBe(4);
        p.IsClosed.ShouldBeTrue();
        p.Length.ShouldBe(8.0, 1e-12);
        p.Bounds.ShouldBe(new Aabb(0, 0, 2, 2));
        p.Vertices.Count().ShouldBe(5); // 4 starta + zadnji end
    }

    [Fact]
    public void OtvorenaPolyline_IsClosedFalse()
    {
        var p = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(1, 0)),
            new LineSeg(new Point2(1, 0), new Point2(1, 1)),
        ]);
        p.IsClosed.ShouldBeFalse();
        p.StartPoint.ShouldBe(new Point2(0, 0));
        p.EndPoint.ShouldBe(new Point2(1, 1));
    }

    [Fact]
    public void GapVeciOdTolerancije_Baca()
    {
        Should.Throw<ArgumentException>(() => new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(1, 0)),
            new LineSeg(new Point2(1.1, 0), new Point2(2, 0)), // gap 0.1 > default 0.05
        ]));
    }

    [Fact]
    public void GapUnutarSireTolerancije_Prolazi()
    {
        var p = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(1, 0)),
            new LineSeg(new Point2(1.1, 0), new Point2(2, 0)),
        ], joinTolerance: 0.15);
        p.Count.ShouldBe(2);
    }

    [Fact]
    public void PraznaLista_Baca()
    {
        Should.Throw<ArgumentException>(() => new Polyline2([]));
    }

    [Fact]
    public void Reversed_ObrceRedoslijedISmjer()
    {
        var p = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(1, 0)),
            new LineSeg(new Point2(1, 0), new Point2(1, 1)),
        ]);

        var r = p.Reversed();
        r.StartPoint.ShouldBe(new Point2(1, 1));
        r.EndPoint.ShouldBe(new Point2(0, 0));
        r.Length.ShouldBe(p.Length, 1e-12);
    }

    [Fact]
    public void MjesovitaPolyline_LinijaLuk()
    {
        // Linija (0,0)→(1,0), zatim CCW polukrug od (1,0) do (1,2) s centrom (1,1).
        var p = new Polyline2([
            new LineSeg(new Point2(0, 0), new Point2(1, 0)),
            ArcSeg.FromStartEndCenter(new Point2(1, 0), new Point2(1, 2), new Point2(1, 1), isCcw: true),
        ]);

        p.Count.ShouldBe(2);
        p.Length.ShouldBe(1.0 + Math.PI, 1e-9);
        p.EndPoint.AlmostEquals(new Point2(1, 2), 1e-9).ShouldBeTrue();
    }
}
