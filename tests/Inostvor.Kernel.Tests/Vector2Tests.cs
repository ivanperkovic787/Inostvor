using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class Vector2Tests
{
    private const double Eps = 1e-12;

    [Fact]
    public void Length_IDotICross()
    {
        var v = new Vector2(3, 4);
        v.Length.ShouldBe(5.0, Eps);
        v.LengthSquared.ShouldBe(25.0, Eps);
        v.Dot(new Vector2(1, 2)).ShouldBe(11.0, Eps);
        new Vector2(1, 0).Cross(new Vector2(0, 1)).ShouldBe(1.0, Eps);   // drugi je lijevo
        new Vector2(0, 1).Cross(new Vector2(1, 0)).ShouldBe(-1.0, Eps); // drugi je desno
    }

    [Fact]
    public void Normalized_JedinicnaDuljina_IIznimkaZaNulVektor()
    {
        var n = new Vector2(3, 4).Normalized();
        n.Length.ShouldBe(1.0, Eps);
        n.X.ShouldBe(0.6, Eps);
        n.Y.ShouldBe(0.8, Eps);

        Should.Throw<InvalidOperationException>(() => new Vector2(0, 0).Normalized());
        Should.Throw<InvalidOperationException>(() => new Vector2(1e-8, 0).Normalized());
    }

    [Fact]
    public void Perpendicular_LijevaOkomica()
    {
        new Vector2(1, 0).Perpendicular().ShouldBe(new Vector2(0, 1));
        new Vector2(0, 1).Perpendicular().ShouldBe(new Vector2(-1, 0));
    }

    [Fact]
    public void Rotated_90Stupnjeva()
    {
        var r = new Vector2(1, 0).Rotated(Math.PI / 2.0);
        r.X.ShouldBe(0.0, Eps);
        r.Y.ShouldBe(1.0, Eps);
    }

    [Fact]
    public void FromAngle_IAngle_Konzistentni()
    {
        var v = Vector2.FromAngle(Math.PI / 3.0);
        v.Length.ShouldBe(1.0, Eps);
        v.Angle.ShouldBe(Math.PI / 3.0, Eps);
    }

    [Fact]
    public void Operatori_Aritmetika()
    {
        (new Vector2(1, 2) + new Vector2(3, 4)).ShouldBe(new Vector2(4, 6));
        (new Vector2(3, 4) - new Vector2(1, 2)).ShouldBe(new Vector2(2, 2));
        (-new Vector2(1, -2)).ShouldBe(new Vector2(-1, 2));
        (new Vector2(1, 2) * 3.0).ShouldBe(new Vector2(3, 6));
        (3.0 * new Vector2(1, 2)).ShouldBe(new Vector2(3, 6));
        (new Vector2(3, 6) / 3.0).ShouldBe(new Vector2(1, 2));
    }
}
