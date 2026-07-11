using Inostvor.Kernel.Primitives;

namespace Inostvor.Core.Model.Import;

/// <summary>
/// Jedan izvorni CAD entitet preveden u interne segmente (u milimetrima, world koordinate).
/// Parser-neutralno: nijedan tip iz konkretne DXF biblioteke ne smije procuriti ovamo.
/// </summary>
/// <param name="Segments">Geometrija entiteta; polyline daje više segmenata, linija jedan.</param>
/// <param name="Layer">Efektivni layer (nakon pravila nasljeđivanja kroz INSERT).</param>
/// <param name="SourceType">Tip izvornog entiteta radi dijagnostike, npr. "LINE", "LWPOLYLINE", "INSERT/CIRCLE".</param>
/// <param name="Handle">Izvorni handle entiteta ako postoji (za povezivanje upozorenja s CAD datotekom).</param>
public sealed record ImportedEntity(
    IReadOnlyList<ISegment> Segments,
    string Layer,
    string SourceType,
    string? Handle);
