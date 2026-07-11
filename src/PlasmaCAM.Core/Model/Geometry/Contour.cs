using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Core.Model.Geometry;

/// <summary>Vrsta konture nakon klasifikacije.</summary>
public enum ContourKind
{
    /// <summary>Zatvorena, ali još neklasificirana (izlaz ContourBuildera prije klasifikacije).</summary>
    Unclassified = 0,

    /// <summary>Otvorena kontura — krajevi se ne poklapaju unutar tolerancije spajanja.</summary>
    Open = 1,

    /// <summary>Vanjska (obodna) kontura dijela.</summary>
    Outer = 2,

    /// <summary>Rupa unutar dijela.</summary>
    Hole = 3,
}

/// <summary>
/// Neprekinuti lanac segmenata nastao detekcijom kontura. Nepromjenjiv;
/// klasifikacija i normalizacija orijentacije proizvode NOVE instance.
/// </summary>
public sealed class Contour
{
    public Contour(int id, Polyline2 polyline, string layer, ContourKind kind, bool closedByTolerance)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        ArgumentNullException.ThrowIfNull(layer);

        Id = id;
        Polyline = polyline;
        Layer = layer;
        Kind = kind;
        ClosedByTolerance = closedByTolerance;
    }

    /// <summary>Stabilan identifikator unutar jednog builda (determinizam izvještaja).</summary>
    public int Id { get; }

    public Polyline2 Polyline { get; }

    public string Layer { get; }

    public ContourKind Kind { get; }

    /// <summary>
    /// True ako je zatvorenost postignuta tolerancijom spajanja (krajevi se NE poklapaju
    /// egzaktno, ali je razmak ≤ tolerancije) — korisnik to mora moći vidjeti.
    /// </summary>
    public bool ClosedByTolerance { get; }

    public bool IsClosed => Kind is ContourKind.Outer or ContourKind.Hole
        || (Kind == ContourKind.Unclassified && Polyline.IsClosed) || ClosedByTolerance;

    public Aabb Bounds => Polyline.Bounds;

    public double Length => Polyline.Length;

    public int SegmentCount => Polyline.Count;

    /// <summary>Nova instanca s drugom klasifikacijom.</summary>
    public Contour WithKind(ContourKind kind) => new(Id, Polyline, Layer, kind, ClosedByTolerance);

    /// <summary>Nova instanca s obrnutim smjerom obilaska (za normalizaciju orijentacije).</summary>
    public Contour Reversed() => new(Id, Polyline.Reversed(), Layer, Kind, ClosedByTolerance);
}
