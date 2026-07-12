using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Generation;

/// <summary>
/// Pohlepni najbliži susjed NA RAZINI DIJELA: sljedeći je dio čija je najbliža
/// pierce točka najbliža trenutnoj poziciji; unutar dijela rupe (po udaljenosti)
/// pa vanjska kontura. Deterministički tie-break: manja udaljenost → manji
/// ContourId. Nije globalni optimum (TSP) — "minimal-rapids" strategija je
/// posebna buduća implementacija kroz isti kontrakt.
/// </summary>
public sealed class NearestNeighborCutOrderStrategy : ICutOrderStrategy
{
    public string Id => "nearest-neighbor";

    public IReadOnlyList<CutSequence> Order(IReadOnlyList<CutSequence> sequences, IReadOnlyList<Contour> contours)
    {
        ArgumentNullException.ThrowIfNull(sequences);
        ArgumentNullException.ThrowIfNull(contours);

        var contourById = contours.ToDictionary(c => c.Id);
        var outers = contours.Where(c => c.Kind == ContourKind.Outer).ToList();

        int OwnerOuterId(Contour c)
        {
            if (c.Kind != ContourKind.Hole)
            {
                return c.Id;
            }

            var owner = -1;
            var ownerArea = double.PositiveInfinity;
            foreach (var outer in outers)
            {
                var area = outer.Bounds.Width * outer.Bounds.Height;
                if (outer.Bounds.Contains(c.Bounds) && area < ownerArea)
                {
                    owner = outer.Id;
                    ownerArea = area;
                }
            }

            return owner >= 0 ? owner : c.Id;
        }

        // Grupiraj sekvence po dijelu.
        var parts = sequences
            .GroupBy(s => OwnerOuterId(contourById[s.SourceContourId]))
            .ToDictionary(g => g.Key, g => g.ToList());

        var ordered = new List<CutSequence>(sequences.Count);
        var position = new Point2(0, 0);
        var remaining = new HashSet<int>(parts.Keys);

        while (remaining.Count > 0)
        {
            // Najbliži dio: minimalna udaljenost do bilo koje njegove pierce točke.
            var bestPart = -1;
            var bestDistance = double.PositiveInfinity;
            foreach (var partId in remaining.OrderBy(id => id)) // stabilna enumeracija
            {
                var d = parts[partId].Min(s => position.DistanceTo(s.PiercePoint));
                if (d < bestDistance || (d == bestDistance && partId < bestPart))
                {
                    bestDistance = d;
                    bestPart = partId;
                }
            }

            remaining.Remove(bestPart);

            // Unutar dijela: rupe najbliže-prvo, pa vanjska kontura.
            var holes = parts[bestPart]
                .Where(s => contourById[s.SourceContourId].Kind == ContourKind.Hole)
                .ToList();
            var rest = parts[bestPart].Except(holes).OrderBy(s => s.SourceContourId).ToList();

            while (holes.Count > 0)
            {
                var next = holes
                    .OrderBy(s => position.DistanceTo(s.PiercePoint))
                    .ThenBy(s => s.SourceContourId)
                    .First();
                holes.Remove(next);
                ordered.Add(next);
                position = next.EndPoint;
            }

            foreach (var sequence in rest)
            {
                ordered.Add(sequence);
                position = sequence.EndPoint;
            }
        }

        return ordered;
    }
}
