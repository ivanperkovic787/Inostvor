using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class MathUtilTests
{
    private const double Eps = 1e-12;

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(-1.0, Math.Tau - 1.0)]
    [InlineData(Math.Tau, 0.0)]
    [InlineData(Math.Tau + 0.5, 0.5)]
    [InlineData(-Math.Tau, 0.0)]
    [InlineData(3.0 * Math.PI, Math.PI)]
    public void NormalizeAngle_URaspon0Do2Pi(double input, double expected)
        => MathUtil.NormalizeAngle(input).ShouldBe(expected, Eps);

    [Fact]
    public void NormalizeAngle_RezultatUvijekUPolurasponu()
    {
        for (var a = -20.0; a <= 20.0; a += 0.37)
        {
            var r = MathUtil.NormalizeAngle(a);
            r.ShouldBeGreaterThanOrEqualTo(0.0);
            r.ShouldBeLessThan(Math.Tau);
        }
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(Math.PI, Math.PI)]
    [InlineData(Math.PI + 0.1, -Math.PI + 0.1)]
    [InlineData(-0.5, -0.5)]
    [InlineData(Math.Tau - 0.25, -0.25)]
    public void NormalizeAngleSigned_URasponMinusPiDoPi(double input, double expected)
        => MathUtil.NormalizeAngleSigned(input).ShouldBe(expected, Eps);

    [Fact]
    public void Lerp_KrajnjeISredisnjeVrijednosti()
    {
        MathUtil.Lerp(10.0, 20.0, 0.0).ShouldBe(10.0, Eps);
        MathUtil.Lerp(10.0, 20.0, 1.0).ShouldBe(20.0, Eps);
        MathUtil.Lerp(10.0, 20.0, 0.5).ShouldBe(15.0, Eps);
        MathUtil.Lerp(10.0, 20.0, 2.0).ShouldBe(30.0, Eps); // ekstrapolacija dopuštena
    }

    [Fact]
    public void DegRad_Konverzije()
    {
        MathUtil.DegToRad(180.0).ShouldBe(Math.PI, Eps);
        MathUtil.RadToDeg(Math.PI / 2.0).ShouldBe(90.0, Eps);
    }

    [Fact]
    public void SignedArea_CcwKvadrat_Pozitivna()
    {
        Point2[] square = [new(0, 0), new(2, 0), new(2, 2), new(0, 2)];
        MathUtil.SignedArea(square).ShouldBe(4.0, Eps);
        MathUtil.IsCcw(square).ShouldBeTrue();
    }

    [Fact]
    public void SignedArea_CwKvadrat_Negativna()
    {
        Point2[] square = [new(0, 0), new(0, 2), new(2, 2), new(2, 0)];
        MathUtil.SignedArea(square).ShouldBe(-4.0, Eps);
        MathUtil.IsCcw(square).ShouldBeFalse();
    }

    [Fact]
    public void SignedArea_ManjeOdTriVrha_Nula()
    {
        MathUtil.SignedArea([new Point2(0, 0), new Point2(1, 1)]).ShouldBe(0.0);
    }
}
