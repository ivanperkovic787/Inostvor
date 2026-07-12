using Inostvor.Cam.Leads;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Inostvor.Sdk.Cam;
using Shouldly;
using Xunit;
using static Inostvor.Cam.Tests.CamTestHelpers;

namespace Inostvor.Cam.Tests;

public sealed class LeadStrategyTests
{
    private static LeadContext Context(double length = 4.0)
    {
        var contour = Contours(("0", SquareLines(0, 0, 10)))[0];
        // Prianjanje na (0,0), rez ide u +X, materijal (dio) je gore (+Y).
        return new LeadContext(new Point2(0, 0), Vector2.UnitX, Vector2.UnitY, contour, length, 2500);
    }

    [Fact]
    public void LineLead_ZavrsavaTocnoUAttachPointu_TocneDuljine()
    {
        var lead = new LineLeadStrategy().BuildLeadIn(Context()).ShouldHaveSingleItem();

        lead.Kind.ShouldBe(MoveKind.LeadIn);
        lead.Geometry.EndPoint.ShouldBe(new Point2(0, 0));
        lead.Length.ShouldBe(4.0, 1e-9);
    }

    [Fact]
    public void LineLead_PierceNaStraniOtpada()
    {
        var lead = new LineLeadStrategy().BuildLeadIn(Context()).Single();

        // Otpad je dolje (−Y): pierce mora imati Y < 0.
        lead.Geometry.StartPoint.Y.ShouldBeLessThan(0);
    }

    [Fact]
    public void ArcLead_TangentanNaPutanju()
    {
        var lead = new ArcLeadStrategy().BuildLeadIn(Context()).Single();
        var arc = lead.Geometry.ShouldBeOfType<ArcSeg>();

        arc.EndPoint.AlmostEquals(new Point2(0, 0), 1e-9).ShouldBeTrue();
        arc.Radius.ShouldBe(4.0, 1e-9);
        arc.Length.ShouldBe(Math.PI / 2.0 * 4.0, 1e-9); // četvrt kruga

        // Tangenta na kraju luka mora biti smjer rezanja (+X).
        var radial = (arc.EndPoint - arc.Center).Normalized();
        var endTangent = arc.IsCcw ? radial.Perpendicular() : -radial.Perpendicular();
        endTangent.X.ShouldBe(1.0, 1e-9);
        endTangent.Y.ShouldBe(0.0, 1e-9);
    }

    [Fact]
    public void ArcLead_CentarNaStraniOtpada()
    {
        var arc = (ArcSeg)new ArcLeadStrategy().BuildLeadIn(Context()).Single().Geometry;
        arc.Center.Y.ShouldBeLessThan(0); // otpad je −Y
    }

    [Fact]
    public void ArcLeadOut_PocinjeTocnoUAttachPointu()
    {
        var lead = new ArcLeadStrategy().BuildLeadOut(Context(2.0)).Single();
        lead.Kind.ShouldBe(MoveKind.LeadOut);
        lead.Geometry.StartPoint.AlmostEquals(new Point2(0, 0), 1e-9).ShouldBeTrue();
    }

    [Fact]
    public void Dispecer_BiraStrategijuPoStilu_NepoznatStil_BezLeada()
    {
        var service = new LeadGeneratorService([new LineLeadStrategy(), new ArcLeadStrategy()]);
        var ctx = Context();

        service.BuildLeadIn(LeadStyle.Line, ctx).Single().Geometry.ShouldBeOfType<LineSeg>();
        service.BuildLeadIn(LeadStyle.Arc, ctx).Single().Geometry.ShouldBeOfType<ArcSeg>();
        service.BuildLeadIn(LeadStyle.None, ctx).ShouldBeEmpty();
        service.BuildLeadIn(LeadStyle.Loop, ctx).ShouldBeEmpty(); // neregistriran → konzervativno ništa
    }
}
