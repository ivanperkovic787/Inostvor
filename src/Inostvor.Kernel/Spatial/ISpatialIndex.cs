using Inostvor.Kernel.Primitives;

namespace Inostvor.Kernel.Spatial;

/// <summary>
/// Prostorni indeks za brze upite preklapanja. Kontrakt je build-and-query:
/// puni se Insertom, na promjenu geometrije se čisti i gradi iznova.
/// (KD/R-Tree implementacije dolaze s nestingom u V2 kroz isti kontrakt.)
/// </summary>
public interface ISpatialIndex<T>
{
    int Count { get; }

    void Insert(Aabb bounds, T item);

    /// <summary>Dodaje u <paramref name="results"/> sve stavke čiji AABB siječe zadano područje.</summary>
    void Query(Aabb area, ICollection<T> results);

    /// <summary>Dodaje u <paramref name="results"/> sve stavke čiji AABB sadrži zadanu točku (uz geometrijsku toleranciju).</summary>
    void Query(Point2 point, ICollection<T> results);

    void Clear();
}
