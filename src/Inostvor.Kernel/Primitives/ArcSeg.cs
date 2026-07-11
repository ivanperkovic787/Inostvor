namespace Inostvor.Kernel.Primitives;

/// <summary>
/// Kružni luk definiran centrom, polumjerom, početnim kutom i PREDZNAČENIM opsegom
/// (sweep): pozitivan = CCW, negativan = CW. |Sweep| = 2π predstavlja puni krug.
/// Nepromjenjiv; degenerirani lukovi (polumjer ili lučna duljina ispod tolerancije)
/// odbijaju se u konstruktoru.
/// </summary>
public sealed class ArcSeg : ISegment
{
    public ArcSeg(Point2 center, double radius, double startAngle, double sweepAngle)
    {
        if (radius < Tolerance.Geometric)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Polumjer luka je ispod geometrijske tolerancije.");
        }

        var absSweep = Math.Abs(sweepAngle);
        if (absSweep * radius < Tolerance.Geometric)
        {
            throw new ArgumentOutOfRangeException(nameof(sweepAngle), sweepAngle, "Lučna duljina je ispod geometrijske tolerancije (degeneriran luk).");
        }

        if (absSweep > Math.Tau + Tolerance.Angular)
        {
            throw new ArgumentOutOfRangeException(nameof(sweepAngle), sweepAngle, "Opseg luka ne može biti veći od punog kruga (2π).");
        }

        Center = center;
        Radius = radius;
        StartAngle = MathUtil.NormalizeAngle(startAngle);
        SweepAngle = Math.Clamp(sweepAngle, -Math.Tau, Math.Tau);
    }

    public Point2 Center { get; }

    public double Radius { get; }

    /// <summary>Početni kut, normaliziran u [0, 2π).</summary>
    public double StartAngle { get; }

    /// <summary>Predznačeni opseg: pozitivan = CCW, negativan = CW. Raspon [-2π, 2π].</summary>
    public double SweepAngle { get; }

    public bool IsCcw => SweepAngle > 0.0;

    public double EndAngle => MathUtil.NormalizeAngle(StartAngle + SweepAngle);

    public Point2 StartPoint => PointAtAngle(StartAngle);

    public Point2 EndPoint => PointAtAngle(StartAngle + SweepAngle);

    public double Length => Math.Abs(SweepAngle) * Radius;

    /// <summary>Je li luk puni krug (|Sweep| = 2π unutar tolerancije).</summary>
    public bool IsFullCircle => Tolerance.AreEqual(Math.Abs(SweepAngle), Math.Tau, Tolerance.Angular * 10.0);

    public Aabb Bounds
    {
        get
        {
            var bounds = Aabb.FromCorners(StartPoint, EndPoint);
            for (var k = 0; k < 4; k++)
            {
                var extremeAngle = k * (Math.PI / 2.0);
                if (ContainsAngle(extremeAngle))
                {
                    bounds = bounds.Union(Aabb.FromCorners(PointAtAngle(extremeAngle), PointAtAngle(extremeAngle)));
                }
            }

            return bounds;
        }
    }

    public Point2 PointAt(double t) => PointAtAngle(StartAngle + (t * SweepAngle));

    /// <summary>Točka na kružnici (ne nužno na luku!) za zadani kut.</summary>
    public Point2 PointAtAngle(double angle)
        => new(Center.X + (Radius * Math.Cos(angle)), Center.Y + (Radius * Math.Sin(angle)));

    /// <summary>
    /// Leži li zadani kut unutar kutnog raspona luka. Kutna tolerancija je izvedena iz
    /// geometrijske tolerancije na trenutnom polumjeru (konzistentno u mm, ne u radijanima).
    /// </summary>
    public bool ContainsAngle(double angle)
    {
        var angularTolerance = (Tolerance.Geometric / Radius) + Tolerance.Angular;
        double travelled = SweepAngle > 0.0
            ? MathUtil.NormalizeAngle(angle - StartAngle)
            : MathUtil.NormalizeAngle(StartAngle - angle);

        var span = Math.Abs(SweepAngle);
        return travelled <= span + angularTolerance || travelled >= Math.Tau - angularTolerance;
    }

    public Point2 ClosestPoint(Point2 point)
    {
        var toPoint = point - Center;
        if (toPoint.Length < Tolerance.Geometric)
        {
            // Točka u centru — sve točke luka su jednako udaljene; deterministički vraćamo start.
            return StartPoint;
        }

        var angle = toPoint.Angle;
        if (ContainsAngle(angle))
        {
            return Center + (toPoint * (Radius / toPoint.Length));
        }

        var toStart = point.DistanceSquaredTo(StartPoint);
        var toEnd = point.DistanceSquaredTo(EndPoint);
        return toStart <= toEnd ? StartPoint : EndPoint;
    }

    public ISegment Reversed() => new ArcSeg(Center, Radius, StartAngle + SweepAngle, -SweepAngle);

    /// <summary>
    /// Luk iz početne i krajnje točke te centra. Ako se start i end poklapaju,
    /// rezultat je puni krug u zadanom smjeru.
    /// </summary>
    public static ArcSeg FromStartEndCenter(Point2 start, Point2 end, Point2 center, bool isCcw)
    {
        var rStart = start.DistanceTo(center);
        var rEnd = end.DistanceTo(center);
        if (!Tolerance.AreEqual(rStart, rEnd, Tolerance.Geometric * 10.0))
        {
            throw new ArgumentException(
                FormattableString.Invariant($"Start i end nisu na istoj kružnici oko centra (r_start={rStart}, r_end={rEnd})."));
        }

        var startAngle = (start - center).Angle;
        var endAngle = (end - center).Angle;

        double sweep;
        if (isCcw)
        {
            sweep = MathUtil.NormalizeAngle(endAngle - startAngle);
            if (sweep * rStart < Tolerance.Geometric)
            {
                sweep = Math.Tau; // start == end → puni krug
            }
        }
        else
        {
            sweep = -MathUtil.NormalizeAngle(startAngle - endAngle);
            if (-sweep * rStart < Tolerance.Geometric)
            {
                sweep = -Math.Tau;
            }
        }

        return new ArcSeg(center, rStart, startAngle, sweep);
    }

    /// <summary>
    /// Luk iz DXF bulge zapisa: bulge = tan(θ/4); pozitivan bulge = CCW od starta prema endu
    /// (AutoCAD konvencija). NAPOMENA o predznaku: za tetivu koja putuje u smjeru +X,
    /// pozitivan bulge daje luk ISPOD tetive s centrom IZNAD (CCW gibanje ima centar s lijeve
    /// strane smjera putovanja) — klasičan izvor bugova, pokriveno testovima.
    /// </summary>
    public static ArcSeg FromBulge(Point2 start, Point2 end, double bulge)
    {
        if (Math.Abs(bulge) < Tolerance.Angular)
        {
            throw new ArgumentOutOfRangeException(nameof(bulge), bulge, "Bulge je (gotovo) nula — segment je linija, koristiti LineSeg.");
        }

        var chord = end - start;
        var chordLength = chord.Length;
        if (chordLength < Tolerance.Geometric)
        {
            throw new ArgumentException("Start i end se poklapaju — puni krug se ne može rekonstruirati iz bulgea.", nameof(end));
        }

        var halfChord = chordLength / 2.0;
        var sagitta = Math.Abs(bulge) * halfChord;
        var radius = ((halfChord * halfChord) + (sagitta * sagitta)) / (2.0 * sagitta);

        var mid = start.MidPointTo(end);
        var leftNormal = chord.Perpendicular() / chordLength;

        // Centar je udaljen (radius - sagitta) od sredine tetive; za pozitivan bulge (CCW)
        // leži na LIJEVOJ strani smjera putovanja. Za velike lukove (|bulge| > 1) izraz
        // (radius - sagitta) postaje negativan i formula automatski prelazi na drugu stranu.
        var center = mid + (leftNormal * (Math.Sign(bulge) * (radius - sagitta)));

        return FromStartEndCenter(start, end, center, isCcw: bulge > 0.0);
    }

    public override string ToString() => FormattableString.Invariant(
        $"Arc c={Center} r={Radius:0.######} start={MathUtil.RadToDeg(StartAngle):0.##}° sweep={MathUtil.RadToDeg(SweepAngle):0.##}°");
}
