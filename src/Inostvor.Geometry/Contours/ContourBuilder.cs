using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Geometry.Contours;

/// <summary>
/// Detekcija kontura pohlepnim lančanjem s prostornim indeksom krajnjih točaka.
///
/// GARANCIJE:
/// 1. Determinizam — isti ulaz daje IDENTIČAN izlaz (Id-jevi, redoslijed, spojevi):
///    segmenti se obilaze ulaznim redoslijedom, kandidati biraju strogim uređajem
///    (razmak, indeks, kraj), nijedan rezultat ne ovisi o enumeraciji hash struktura.
/// 2. Evidencija spajanja — svaki premošteni razmak &gt; geometrijske tolerancije
///    (a ≤ tolerancije spajanja) bilježi se kao <see cref="ContourJoin"/>; geometrija
///    se NE mijenja (healing razmaka je posao kerf faze u M5, gdje Clipper radi u µm).
/// 3. Layeri se ne miješaju — konture se grade po layeru, redoslijedom prve pojave.
///
/// OGRANIČENJE (dokumentirano): kod grananja (3+ kraja u istoj točki) lančanje je
/// pohlepno — bira se najmanji razmak pa najmanji indeks. Za CAD podatke to je
/// ispravno u praksi; globalna optimizacija grafa nije potrebna u V1.
/// </summary>
public sealed class ContourBuilder : IContourBuilder
{
    public ContourBuildResult Build(IReadOnlyList<ImportedEntity> entities, ContourBuildSettings settings)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(settings);

        var contours = new List<Contour>();
        var joins = new List<ContourJoin>();
        var nextId = 0;

        // Grupiranje po layeru, redoslijedom prve pojave (determinizam).
        var layerOrder = new List<string>();
        var segmentsByLayer = new Dictionary<string, List<ISegment>>(StringComparer.Ordinal);
        foreach (var entity in entities)
        {
            if (!segmentsByLayer.TryGetValue(entity.Layer, out var list))
            {
                list = [];
                segmentsByLayer[entity.Layer] = list;
                layerOrder.Add(entity.Layer);
            }

            list.AddRange(entity.Segments);
        }

        foreach (var layer in layerOrder)
        {
            BuildLayer(layer, segmentsByLayer[layer], settings, contours, joins, ref nextId);
        }

        return new ContourBuildResult(contours, joins);
    }

    private static void BuildLayer(
        string layer,
        List<ISegment> segments,
        ContourBuildSettings settings,
        List<Contour> contours,
        List<ContourJoin> joins,
        ref int nextId)
    {
        var tolerance = Math.Max(settings.JoinTolerance, Tolerance.Geometric);

        var index = new EndpointIndex(tolerance);
        for (var i = 0; i < segments.Count; i++)
        {
            index.Add(segments[i].StartPoint, i, atStart: true);
            index.Add(segments[i].EndPoint, i, atStart: false);
        }

        var used = new bool[segments.Count];

        for (var seed = 0; seed < segments.Count; seed++)
        {
            if (used[seed])
            {
                continue;
            }

            used[seed] = true;
            var chain = new LinkedList<ISegment>();
            chain.AddLast(segments[seed]);
            var chainStart = segments[seed].StartPoint;
            var chainEnd = segments[seed].EndPoint;
            var pendingJoins = new List<(Point2 Location, double Gap)>();

            // Širenje naprijed (od kraja lanca).
            while (index.TryFindNearest(chainEnd, tolerance, used, out var c))
            {
                used[c.SegmentIndex] = true;
                var next = c.AtStart ? segments[c.SegmentIndex] : segments[c.SegmentIndex].Reversed();
                if (c.Gap > Tolerance.Geometric)
                {
                    pendingJoins.Add((chainEnd.MidPointTo(next.StartPoint), c.Gap));
                }

                chain.AddLast(next);
                chainEnd = next.EndPoint;
            }

            // Širenje natrag (od početka lanca) — seed nije nužno bio prvi segment konture.
            while (index.TryFindNearest(chainStart, tolerance, used, out var c))
            {
                used[c.SegmentIndex] = true;
                var previous = c.AtStart ? segments[c.SegmentIndex].Reversed() : segments[c.SegmentIndex];
                if (c.Gap > Tolerance.Geometric)
                {
                    pendingJoins.Add((chainStart.MidPointTo(previous.EndPoint), c.Gap));
                }

                chain.AddFirst(previous);
                chainStart = previous.StartPoint;
            }

            // Zatvaranje: krajevi unutar tolerancije. Jednosegmentni lanac smije se
            // zatvoriti samo za luk (puni/skoro puni krug) — linija se ne zatvara na sebe.
            var closingGap = chainStart.DistanceTo(chainEnd);
            var canClose = closingGap <= tolerance && (chain.Count > 1 || chain.First!.Value is ArcSeg);
            var closedByTolerance = canClose && closingGap > Tolerance.Geometric;

            var polyline = new Polyline2([.. chain], joinTolerance: tolerance);
            var kind = canClose ? ContourKind.Unclassified : ContourKind.Open;
            contours.Add(new Contour(nextId, polyline, layer, kind, closedByTolerance));

            foreach (var (location, gap) in pendingJoins)
            {
                joins.Add(new ContourJoin(nextId, location, gap, IsClosingJoin: false));
            }

            if (closedByTolerance)
            {
                joins.Add(new ContourJoin(nextId, chainStart.MidPointTo(chainEnd), closingGap, IsClosingJoin: true));
            }

            nextId++;
        }
    }
}
