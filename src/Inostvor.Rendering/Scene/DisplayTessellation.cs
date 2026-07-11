using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Rendering.Scene;

/// <summary>
/// Level-of-detail politika prikaza + cache tessellacije lukova za prikaz.
///
/// - Tolerancija tetive ovisi o zoomu: pola piksela u world jedinicama —
///   vizualno savršen luk bez rasipanja točaka pri odzumiranju.
/// - Bucket = potencija broja 2 tolerancije: cache se NE regenerira na svakom
///   pikselu zooma, nego kad se tolerancija promijeni ~2×.
/// - Segmenti manji od praga piksela preskaču se (sub-pixel cull).
///
/// Cache je vezan uz JEDNU scenu (nova scena → novi cache); prikazna
/// tessellacija je isključivo za crtanje — CAM nikad ne vidi ove točke,
/// izvor istine ostaje egzaktna geometrija konture.
/// </summary>
public sealed class DisplayTessellation
{
    /// <summary>Ciljna pogreška tetive u pikselima.</summary>
    public const double ChordTolerancePixels = 0.5;

    /// <summary>Segment čija je dijagonala AABB-a manja od ovoga (px) ne crta se.</summary>
    public const double SubPixelCullThreshold = 0.75;

    private readonly Dictionary<(int SegmentId, int Bucket), IReadOnlyList<Point2>> _arcCache = [];

    /// <summary>World tolerancija tetive za zadanu skalu (px/mm), ograničena na razuman raspon.</summary>
    public static double WorldChordTolerance(double scale)
        => Math.Clamp(ChordTolerancePixels / scale, 1e-4, 5.0);

    /// <summary>Bucket tolerancije: log2 kvantizacija — stabilan ključ cachea kroz raspon zooma.</summary>
    public static int ToleranceBucket(double worldTolerance)
        => (int)Math.Floor(Math.Log2(worldTolerance));

    /// <summary>True ako je segment premalen da bi bio vidljiv pri zadanoj skali.</summary>
    public static bool IsSubPixel(ISegment segment, double scale)
    {
        ArgumentNullException.ThrowIfNull(segment);
        var b = segment.Bounds;
        var diagonalPixels = Math.Sqrt((b.Width * b.Width) + (b.Height * b.Height)) * scale;
        return diagonalPixels < SubPixelCullThreshold;
    }

    /// <summary>
    /// Točke luka za prikaz pri zadanoj skali; kesirano po (segment, bucket).
    /// Isti bucket vraća ISTU instancu (bez realokacija tijekom pan-a).
    /// </summary>
    public IReadOnlyList<Point2> GetArcPoints(int segmentId, ArcSeg arc, double scale)
    {
        ArgumentNullException.ThrowIfNull(arc);

        var tolerance = WorldChordTolerance(scale);
        var bucket = ToleranceBucket(tolerance);
        var key = (segmentId, bucket);

        if (_arcCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Tolerancija = donja granica bucketa → prikaz nikad grublji od ciljanih 0.5 px.
        var bucketTolerance = Math.Pow(2.0, bucket);
        var points = Tessellation.TessellateArc(arc, bucketTolerance);
        _arcCache[key] = points;
        return points;
    }

    public int CachedEntryCount => _arcCache.Count;
}
