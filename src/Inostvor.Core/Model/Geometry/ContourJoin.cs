using Inostvor.Kernel.Primitives;

namespace Inostvor.Core.Model.Geometry;

/// <summary>
/// Zapis o JEDNOM automatskom premoštenju razmaka tijekom detekcije kontura:
/// dvije krajnje točke bile su različite, ali unutar tolerancije spajanja.
/// Geometrija se pri tome NE mijenja — razmak se premošćuje logički, a zapis
/// omogućuje korisniku da vidi točno što je spojeno.
/// </summary>
/// <param name="ContourId">Kontura u kojoj je spoj nastao.</param>
/// <param name="Location">Sredina premoštenog razmaka.</param>
/// <param name="GapSize">Veličina razmaka. [mm]</param>
/// <param name="IsClosingJoin">True ako je spoj zatvorio konturu (spoj kraja na početak).</param>
public sealed record ContourJoin(int ContourId, Point2 Location, double GapSize, bool IsClosingJoin);
