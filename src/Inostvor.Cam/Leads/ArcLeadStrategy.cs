using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Inostvor.Sdk.Cam;

namespace Inostvor.Cam.Leads;

/// <summary>
/// Četvrt-kružni lead TANGENTAN na putanju u točki prianjanja — standard za
/// rupe: bez zareza na ulazu, glatka promjena smjera. Polumjer = Length,
/// centar na strani otpada (suprotno od InwardNormal).
/// </summary>
public sealed class ArcLeadStrategy : ILeadStrategy
{
    public LeadStyle Style => LeadStyle.Arc;

    public IReadOnlyList<CutMove> BuildLeadIn(LeadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var radius = context.Length;
        var scrapNormal = -context.InwardNormal;
        var center = context.AttachPoint + (scrapNormal * radius);

        // Luk završava u AttachPointu s tangentom = context.Tangent.
        // Smjer luka: tangenta u završnoj točki mora biti Tangent ⇒
        // CCW ako je Tangent = perpendikular(AttachPoint−Center) u CCW smislu.
        var radial = (context.AttachPoint - center).Normalized();
        var isCcw = radial.Cross(context.Tangent) > 0.0;

        var endAngle = radial.Angle;
        var sweep = isCcw ? Math.PI / 2.0 : -Math.PI / 2.0;
        var arc = new ArcSeg(center, radius, endAngle - sweep, sweep);

        return [new CutMove(arc, MoveKind.LeadIn, context.FeedRate)];
    }

    public IReadOnlyList<CutMove> BuildLeadOut(LeadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var radius = context.Length;
        var scrapNormal = -context.InwardNormal;
        var center = context.AttachPoint + (scrapNormal * radius);

        var radial = (context.AttachPoint - center).Normalized();
        var isCcw = radial.Cross(context.Tangent) > 0.0;

        var startAngle = radial.Angle;
        var sweep = isCcw ? Math.PI / 2.0 : -Math.PI / 2.0;
        var arc = new ArcSeg(center, radius, startAngle, sweep);

        return [new CutMove(arc, MoveKind.LeadOut, context.FeedRate)];
    }
}
