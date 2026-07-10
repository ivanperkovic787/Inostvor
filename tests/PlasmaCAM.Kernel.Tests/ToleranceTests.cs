using PlasmaCAM.Kernel;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class ToleranceTests
{
    [Theory]
    [InlineData(1.0, 1.0, true)]
    [InlineData(1.0, 1.0000005, true)]
    [InlineData(1.0, 1.00001, false)]
    [InlineData(-3.0, -3.0000009, true)]
    public void AreEqual_GeometrijskaTolerancija(double a, double b, bool expected)
        => Tolerance.AreEqual(a, b).ShouldBe(expected);

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(9e-7, true)]
    [InlineData(-9e-7, true)]
    [InlineData(2e-6, false)]
    public void IsZero_GeometrijskaTolerancija(double v, bool expected)
        => Tolerance.IsZero(v).ShouldBe(expected);

    [Fact]
    public void AreEqual_EksplicitnaTolerancija()
    {
        Tolerance.AreEqual(1.0, 1.04, 0.05).ShouldBeTrue();
        Tolerance.AreEqual(1.0, 1.06, 0.05).ShouldBeFalse();
    }
}
