using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Inostvor.Sdk.Cam;

namespace Inostvor.Cam.Leads;

/// <summary>
/// Ravni lead pod 45° prema strani OTPADA (suprotno od InwardNormal) — pierce
/// dalje od dobre površine, ulaz/izlaz ne oštećuju dio.
/// </summary>
public sealed class LineLeadStrategy : ILeadStrategy
{
    public LeadStyle Style => LeadStyle.Line;

    public IReadOnlyList<CutMove> BuildLeadIn(LeadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var pierce = PiercePoint(context);
        return [new CutMove(new LineSeg(pierce, context.AttachPoint), MoveKind.LeadIn, context.FeedRate)];
    }

    public IReadOnlyList<CutMove> BuildLeadOut(LeadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var exit = ExitPoint(context);
        return [new CutMove(new LineSeg(context.AttachPoint, exit), MoveKind.LeadOut, context.FeedRate)];
    }

    private static Point2 PiercePoint(LeadContext c)
    {
        // 45° unatrag od tangente, na stranu otpada.
        var direction = ((-c.Tangent) + (-c.InwardNormal)).Normalized();
        return c.AttachPoint + (direction * c.Length);
    }

    private static Point2 ExitPoint(LeadContext c)
    {
        var direction = (c.Tangent + (-c.InwardNormal)).Normalized();
        return c.AttachPoint + (direction * c.Length);
    }
}
