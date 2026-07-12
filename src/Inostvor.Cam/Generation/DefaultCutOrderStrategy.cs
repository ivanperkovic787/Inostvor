using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Cam.Generation;

/// <summary>
/// Deterministički default redoslijed: (1) RUPE PRIJE vanjske konture istog
/// dijela — kad se dio odreže, može se pomaknuti, rupe više nisu točne;
/// (2) dijelovi po poziciji (MinY pa MinX granica vanjske konture);
/// (3) rupe unutar dijela po istoj poziciji. Stabilan tie-break: ContourId.
/// Napredne strategije (najkraći put, toplinska ravnoteža) u M6 kroz isti kontrakt.
/// </summary>
public sealed class DefaultCutOrderStrategy : ICutOrderStrategy
{
    public IReadOnlyList<CutSequence> Order(IReadOnlyList<CutSequence> sequences, IReadOnlyList<Contour> contours)
    {
        ArgumentNullException.ThrowIfNull(sequences);
        ArgumentNullException.ThrowIfNull(contours);

        var contourById = contours.ToDictionary(c => c.Id);

        // Grupiranje: svaka rupa pripada najmanjoj vanjskoj konturi koja je sadrži (po granicama).
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

        (double Y, double X, int Id) PartKey(int outerId)
        {
            var bounds = contourById.TryGetValue(outerId, out var outer) ? outer.Bounds : default;
            return (bounds.MinY, bounds.MinX, outerId);
        }

        return sequences
            .OrderBy(s => PartKey(OwnerOuterId(contourById[s.SourceContourId])))
            .ThenBy(s => contourById[s.SourceContourId].Kind == ContourKind.Outer ? 1 : 0) // rupe prije outera
            .ThenBy(s => contourById[s.SourceContourId].Bounds.MinY)
            .ThenBy(s => contourById[s.SourceContourId].Bounds.MinX)
            .ThenBy(s => s.SourceContourId)
            .ToList();
    }
}
