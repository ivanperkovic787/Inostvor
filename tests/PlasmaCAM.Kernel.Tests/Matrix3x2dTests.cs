using PlasmaCAM.Kernel.Primitives;
using PlasmaCAM.Kernel.Transforms;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class Matrix3x2dTests
{
    private const double Eps = 1e-12;

    private static void ShouldBePoint(Point2 actual, double x, double y)
    {
        actual.X.ShouldBe(x, Eps);
        actual.Y.ShouldBe(y, Eps);
    }

    [Fact]
    public void Identity_NeMijenjaTocku()
    {
        var p = Matrix3x2d.Identity.TransformPoint(new Point2(3.5, -2.25));
        ShouldBePoint(p, 3.5, -2.25);
    }

    [Fact]
    public void Translation_PomiceTocku_AliNeVektor()
    {
        var m = Matrix3x2d.CreateTranslation(10, -5);
        ShouldBePoint(m.TransformPoint(new Point2(1, 1)), 11, -4);

        var v = m.TransformVector(new Vector2(1, 1));
        v.X.ShouldBe(1.0, Eps);
        v.Y.ShouldBe(1.0, Eps);
    }

    [Fact]
    public void Rotation_90CCW_OkoIshodista()
    {
        var m = Matrix3x2d.CreateRotation(Math.PI / 2.0);
        ShouldBePoint(m.TransformPoint(new Point2(1, 0)), 0, 1);
        ShouldBePoint(m.TransformPoint(new Point2(0, 1)), -1, 0);
    }

    [Fact]
    public void Rotation_OkoCentra()
    {
        var m = Matrix3x2d.CreateRotation(Math.PI / 2.0, new Point2(2, 0));
        ShouldBePoint(m.TransformPoint(new Point2(3, 0)), 2, 1);
        ShouldBePoint(m.TransformPoint(new Point2(2, 0)), 2, 0); // centar miruje
    }

    [Fact]
    public void Scale_OkoIshodistaIOkoCentra()
    {
        ShouldBePoint(Matrix3x2d.CreateScale(2, 3).TransformPoint(new Point2(1, 1)), 2, 3);

        var m = Matrix3x2d.CreateScale(2, 2, new Point2(1, 1));
        ShouldBePoint(m.TransformPoint(new Point2(2, 1)), 3, 1);
        ShouldBePoint(m.TransformPoint(new Point2(1, 1)), 1, 1); // centar miruje
    }

    [Fact]
    public void Zrcaljenje_NegativanScale_ObrceOrijentaciju()
    {
        var m = Matrix3x2d.CreateScale(-1, 1);
        ShouldBePoint(m.TransformPoint(new Point2(2, 3)), -2, 3);
        m.Determinant.ShouldBe(-1.0, Eps);
    }

    [Fact]
    public void Kompozicija_PrimjenjujeLijevoPaDesno()
    {
        // Prvo rotacija 90° CCW, zatim translacija (10, 0): (1,0) → (0,1) → (10,1).
        var m = Matrix3x2d.CreateRotation(Math.PI / 2.0) * Matrix3x2d.CreateTranslation(10, 0);
        ShouldBePoint(m.TransformPoint(new Point2(1, 0)), 10, 1);

        // Obrnuti redoslijed: (1,0) → (11,0) → (0,11).
        var m2 = Matrix3x2d.CreateTranslation(10, 0) * Matrix3x2d.CreateRotation(Math.PI / 2.0);
        ShouldBePoint(m2.TransformPoint(new Point2(1, 0)), 0, 11);
    }

    [Fact]
    public void TryInvert_RoundTripVracaOriginal()
    {
        var m = Matrix3x2d.CreateRotation(0.7, new Point2(3, -2))
              * Matrix3x2d.CreateScale(2, 0.5)
              * Matrix3x2d.CreateTranslation(-4, 9);

        m.TryInvert(out var inv).ShouldBeTrue();

        var p = new Point2(5.25, -1.75);
        var roundTrip = inv.TransformPoint(m.TransformPoint(p));
        roundTrip.X.ShouldBe(p.X, 1e-9);
        roundTrip.Y.ShouldBe(p.Y, 1e-9);
    }

    [Fact]
    public void TryInvert_SingularnaMatrica_VracaFalse()
    {
        var singular = Matrix3x2d.CreateScale(0, 1);
        singular.TryInvert(out var inv).ShouldBeFalse();
        inv.ShouldBe(Matrix3x2d.Identity);
    }
}
