using Inostvor.Cam.Leads;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;
using Inostvor.Sdk.Cam;

namespace Inostvor.Cam.Generation;

/// <summary>
/// Orkestrator CAM cjevovoda po konturi: kerf offset → (opcionalni) arc fitting →
/// leadovi → overcut → sekvenca; zatim redoslijed rezanja i brzi pomaci.
///
/// Izlaz je isključivo neutralni IR (ToolpathProgram) — nijedan bajt G-koda,
/// nijedna pretpostavka o kontroleru ili procesu. Sve operacije su zamjenjivi
/// servisi kroz sučelja (IKerfOffsetService, IArcFitter, ILeadStrategy,
/// IOvercutService, ICutOrderStrategy).
/// </summary>
public sealed class ToolpathGenerator : IToolpathGenerator
{
    private readonly IKerfOffsetService _kerf;
    private readonly IArcFitter _arcFitter;
    private readonly LeadGeneratorService _leads;
    private readonly IOvercutService _overcut;
    private readonly ICutOrderStrategyProvider _cutOrderProvider;

    public ToolpathGenerator(
        IKerfOffsetService kerf,
        IArcFitter arcFitter,
        LeadGeneratorService leads,
        IOvercutService overcut,
        ICutOrderStrategyProvider cutOrderProvider)
    {
        ArgumentNullException.ThrowIfNull(kerf);
        ArgumentNullException.ThrowIfNull(arcFitter);
        ArgumentNullException.ThrowIfNull(leads);
        ArgumentNullException.ThrowIfNull(overcut);
        ArgumentNullException.ThrowIfNull(cutOrderProvider);
        _kerf = kerf;
        _arcFitter = arcFitter;
        _leads = leads;
        _overcut = overcut;
        _cutOrderProvider = cutOrderProvider;
    }

    public ToolpathProgram Generate(IReadOnlyList<Contour> contours, TechnologySettings technology)
    {
        ArgumentNullException.ThrowIfNull(contours);
        ArgumentNullException.ThrowIfNull(technology);

        var sequences = new List<CutSequence>();
        foreach (var contour in contours)
        {
            if (contour.Kind == ContourKind.Unclassified)
            {
                continue; // pipeline uvijek klasificira; obrana u dubinu
            }

            foreach (var ring in _kerf.Offset(contour, KerfFor(contour, technology), technology.OffsetTessellationTolerance))
            {
                var sequence = BuildSequence(contour, ring, technology);
                if (sequence is not null)
                {
                    sequences.Add(sequence);
                }
            }
        }

        var ordered = _cutOrderProvider.Resolve(technology.CutOrderStrategyId).Order(sequences, contours);

        // Brzi pomaci: ishodište stroja → pierce 1 → … (eksplicitno, za simulaciju).
        var rapids = new List<RapidMove>(ordered.Count);
        var position = new Point2(0, 0);
        foreach (var sequence in ordered)
        {
            rapids.Add(new RapidMove(position, sequence.PiercePoint));
            position = sequence.EndPoint;
        }

        return new ToolpathProgram(ordered, rapids, technology, ComputeStatistics(ordered, rapids, technology));
    }

    private CutSequence? BuildSequence(Contour contour, IReadOnlyList<Point2> ring, TechnologySettings technology)
    {
        var isClosed = contour.Kind != ContourKind.Open;

        // Putanja: arc fitting (opcionalan) ili sirove linije.
        IReadOnlyList<ISegment> path = technology.EnableArcFitting
            ? _arcFitter.Fit(ring, isClosed, technology.ArcFittingTolerance)
            : ToLines(ring, isClosed);

        if (path.Count == 0)
        {
            return null;
        }

        var cutMoves = path.Select(s => new CutMove(s, MoveKind.Cut, technology.FeedRate)).ToList();

        var moves = new List<CutMove>();
        var attach = path[0].StartPoint;
        var piercePoint = attach;

        if (isClosed)
        {
            var context = BuildLeadContext(contour, path, technology);

            var leadIn = _leads.BuildLeadIn(technology.LeadInStyle, context with { Length = technology.LeadInLength });
            moves.AddRange(leadIn);
            if (leadIn.Count > 0)
            {
                piercePoint = leadIn[0].Geometry.StartPoint;
            }

            moves.AddRange(cutMoves);
            moves = _overcut.Apply(moves, technology.OvercutLength, technology.FeedRate).ToList();

            var leadOut = _leads.BuildLeadOut(technology.LeadOutStyle, context with { Length = technology.LeadOutLength });
            moves.AddRange(leadOut);
        }
        else
        {
            moves.AddRange(cutMoves); // otvorena: središnjica, bez leadova (V1)
        }

        return new CutSequence(contour.Id, piercePoint, moves);
    }

    private static LeadContext BuildLeadContext(Contour contour, IReadOnlyList<ISegment> path, TechnologySettings technology)
    {
        var attach = path[0].StartPoint;
        var tangent = Tangent(path[0]);

        // Materijal koji OSTAJE: Outer CCW → lijevo od tangente je dio (unutra);
        // Hole CW → lijevo od tangente je dio (izvan rupe). U oba slučaja: lijevo.
        var inwardNormal = tangent.Perpendicular();

        return new LeadContext(attach, tangent, inwardNormal, contour, technology.LeadInLength, technology.FeedRate);
    }

    private static Vector2 Tangent(ISegment segment) => segment switch
    {
        LineSeg line => line.Direction,
        ArcSeg arc => TangentOfArcAtStart(arc),
        _ => Vector2.UnitX,
    };

    private static Vector2 TangentOfArcAtStart(ArcSeg arc)
    {
        var radial = (arc.StartPoint - arc.Center).Normalized();
        return arc.IsCcw ? radial.Perpendicular() : -radial.Perpendicular();
    }

    private static double KerfFor(Contour contour, TechnologySettings technology)
        => contour.Kind == ContourKind.Open ? 0.0 : technology.KerfWidth;

    private static List<ISegment> ToLines(IReadOnlyList<Point2> ring, bool closed)
    {
        var segments = new List<ISegment>(ring.Count);
        var count = closed ? ring.Count : ring.Count - 1;
        for (var i = 0; i < count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            if (a.DistanceTo(b) > Tolerance.Geometric)
            {
                segments.Add(new LineSeg(a, b));
            }
        }

        return segments;
    }

    private static ToolpathStatistics ComputeStatistics(
        IReadOnlyList<CutSequence> sequences, IReadOnlyList<RapidMove> rapids, TechnologySettings technology)
    {
        var cutLength = sequences.Sum(s => s.CutLength);
        var rapidLength = rapids.Sum(r => r.Length);
        var cutTime = sequences.SelectMany(s => s.Moves).Sum(m => m.Duration);
        var rapidTime = technology.RapidRate > 0 ? rapidLength / technology.RapidRate * 60.0 : 0.0;
        var pierceTime = sequences.Count * technology.PierceTime;

        return new ToolpathStatistics(cutLength, rapidLength, cutTime, rapidTime, pierceTime, sequences.Count);
    }
}
