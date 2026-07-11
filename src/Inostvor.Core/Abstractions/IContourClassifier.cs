using Inostvor.Core.Model.Geometry;

namespace Inostvor.Core.Abstractions;

/// <summary>
/// Port klasifikacije kontura: određuje Outer/Hole ugnježđivanjem i normalizira
/// orijentaciju (Outer → CCW, Hole → CW). Otvorene konture prolaze nepromijenjene.
/// </summary>
public interface IContourClassifier
{
    IReadOnlyList<Contour> Classify(IReadOnlyList<Contour> contours);
}
