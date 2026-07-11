using Inostvor.Kernel.Primitives;

namespace Inostvor.Kernel.Intersections;

/// <summary>Presjek dvaju ravnih segmenata.</summary>
public static class LineLineIntersection
{
    /// <summary>
    /// Pokušava naći presjek segmenata (uključujući dodire na krajevima, unutar tolerancije).
    /// Vraća false za paralelne i kolinearne segmente — kolinearno preklapanje ima beskonačno
    /// presjeka i provjerava se zasebno kroz <see cref="AreCollinearOverlapping"/>.
    /// </summary>
    public static bool TryIntersect(LineSeg a, LineSeg b, out Point2 point)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var r = a.EndPoint - a.StartPoint;
        var s = b.EndPoint - b.StartPoint;
        var denom = r.Cross(s);

        // Test paralelnosti: |sin kuta| ispod kutne tolerancije.
        if (Math.Abs(denom) <= Tolerance.Angular * a.Length * b.Length)
        {
            point = default;
            return false;
        }

        var qp = b.StartPoint - a.StartPoint;
        var t = qp.Cross(s) / denom;
        var u = qp.Cross(r) / denom;

        // Parametarska tolerancija izvedena iz geometrijske (mm), po duljini svakog segmenta.
        var tTol = Tolerance.Geometric / a.Length;
        var uTol = Tolerance.Geometric / b.Length;

        if (t < -tTol || t > 1.0 + tTol || u < -uTol || u > 1.0 + uTol)
        {
            point = default;
            return false;
        }

        point = a.PointAt(Math.Clamp(t, 0.0, 1.0));
        return true;
    }

    /// <summary>Jesu li segmenti kolinearni I preklapaju li se (dionica zajedničke duljine > tolerancije ili dodir).</summary>
    public static bool AreCollinearOverlapping(LineSeg a, LineSeg b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var r = a.EndPoint - a.StartPoint;
        var s = b.EndPoint - b.StartPoint;

        if (Math.Abs(r.Cross(s)) > Tolerance.Angular * a.Length * b.Length)
        {
            return false; // nisu paralelni
        }

        // Udaljenost b.Start od pravca kroz a mora biti unutar geometrijske tolerancije.
        var qp = b.StartPoint - a.StartPoint;
        if (Math.Abs(qp.Cross(r)) / a.Length > Tolerance.Geometric)
        {
            return false; // paralelni, ali ne kolinearni
        }

        // Projekcija intervala b na parametar segmenta a.
        var rr = r.LengthSquared;
        var t0 = qp.Dot(r) / rr;
        var t1 = (b.EndPoint - a.StartPoint).Dot(r) / rr;
        if (t0 > t1)
        {
            (t0, t1) = (t1, t0);
        }

        var tol = Tolerance.Geometric / a.Length;
        return t1 >= -tol && t0 <= 1.0 + tol;
    }
}
