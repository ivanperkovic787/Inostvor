using Inostvor.Core.Abstractions;
using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Fitting;

/// <summary>
/// Pohlepni arc fitting s APSOLUTNOM garancijom točnosti: luk se emitira samo
/// ako SVE obuhvaćene ulazne točke leže unutar tolerancije od luka I kutovi su
/// strogo monotoni duž smjera obilaska. U protivnom ostaju linije. Kolinearni
/// nizovi komprimiraju se u jednu liniju (isti kriterij tolerancije).
///
/// Na svakoj poziciji računaju se maksimalni linijski i maksimalni lučni niz;
/// bira se dulji (tie → linija, jer je jednostavnija). Deterministički.
/// </summary>
public sealed class ArcFitter : IArcFitter
{
    /// <summary>Polumjer iznad ovoga tretira se kao pravac (numerička stabilnost G2/G3 centra).</summary>
    private const double MaxArcRadius = 1e5;

    private const int MinArcPoints = 4;

    public IReadOnlyList<ISegment> Fit(IReadOnlyList<Point2> points, bool closed, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tolerance, 0.0);

        var pts = new List<Point2>(points);
        if (closed && pts.Count > 1 && pts[0].AlmostEquals(pts[^1]))
        {
            pts.RemoveAt(pts.Count - 1); // radimo s prstenom bez duplicirane zadnje točke
        }

        // ZATVORENI PRSTEN: šav (točka gdje niz počinje) je proizvoljan — kod kerf
        // offseta ga postavlja normalizacija (leksikografski minimum), dakle usred
        // glatkog luka. Naivno fitanje od šava do šava zato uvijek ostavlja kratki
        // REP koji je prekratak za vlastiti luk i degradira u liniju, iako su rep i
        // prvi luk FIZIČKI ISTI luk. Rješenje: rotiraj prsten tako da šav padne na
        // KUT (mjesto stvarnog prekida zakrivljenosti). Ako kuta nema (čista
        // kružnica), fit kreće bilo gdje i cijeli prsten postaje jedan-dva luka.
        if (closed && pts.Count >= MinArcPoints)
        {
            var seam = FindCornerIndex(pts, tolerance);
            if (seam > 0)
            {
                var rotated = new List<Point2>(pts.Count);
                for (var k = 0; k < pts.Count; k++)
                {
                    rotated.Add(pts[(seam + k) % pts.Count]);
                }

                pts = rotated;
            }
        }

        if (closed && pts.Count > 1)
        {
            pts.Add(pts[0]); // zatvori niz za obradu
        }

        var segments = new List<ISegment>();
        var i = 0;
        while (i < pts.Count - 1)
        {
            var lineEnd = ExtendLine(pts, i, tolerance);
            var arcEnd = ExtendArc(pts, i, tolerance, out var arc);

            if (arc is not null && arcEnd > lineEnd)
            {
                segments.Add(arc);
                i = arcEnd;
            }
            else
            {
                if (pts[i].DistanceTo(pts[lineEnd]) > Tolerance.Geometric)
                {
                    segments.Add(new LineSeg(pts[i], pts[lineEnd]));
                }

                i = lineEnd;
            }
        }

        // Zatvoreni prsten: ako su ZADNJI i PRVI segment isti luk (šav je pao usred
        // glatkog luka), spoji ih u jedan — inače bi ostala umjetna podjela na šavu.
        if (closed && segments.Count >= 2)
        {
            segments = MergeSeamArcs(segments, tolerance);
        }

        return segments;
    }

    /// <summary>
    /// Indeks točke u kojoj se smjer naglo mijenja (KUT) — prirodno mjesto za šav
    /// zatvorenog prstena. Vraća 0 ako prsten nema kuta (glatka krivulja).
    /// Prag: 15° promjene smjera između susjednih tetiva.
    /// </summary>
    private static int FindCornerIndex(List<Point2> pts, double tolerance)
    {
        const double CornerThresholdRad = 0.26; // ~15°

        var bestIndex = 0;
        var bestTurn = CornerThresholdRad;

        for (var k = 0; k < pts.Count; k++)
        {
            var previous = pts[(k - 1 + pts.Count) % pts.Count];
            var current = pts[k];
            var next = pts[(k + 1) % pts.Count];

            var incoming = current - previous;
            var outgoing = next - current;
            if (incoming.Length < Tolerance.Geometric || outgoing.Length < Tolerance.Geometric)
            {
                continue;
            }

            var turn = Math.Abs(MathUtil.NormalizeAngle(outgoing.Angle - incoming.Angle));
            if (turn > Math.PI)
            {
                turn = Math.Tau - turn;
            }

            if (turn > bestTurn)
            {
                bestTurn = turn;
                bestIndex = k;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Spaja zadnji i prvi segment ako oba leže na ISTOJ kružnici i nastavljaju se
    /// (šav je pao usred glatkog luka). Rezultat je jedan luk preko šava.
    /// Konzervativno: spaja SAMO lukove, i samo ako se centar, polumjer i smjer
    /// podudaraju unutar tolerancije.
    /// </summary>
    private static List<ISegment> MergeSeamArcs(List<ISegment> segments, double tolerance)
    {
        if (segments[0] is not ArcSeg first || segments[^1] is not ArcSeg last)
        {
            return segments;
        }

        var sameCircle = last.Center.DistanceTo(first.Center) <= tolerance
            && Math.Abs(last.Radius - first.Radius) <= tolerance
            && last.IsCcw == first.IsCcw;
        if (!sameCircle)
        {
            return segments;
        }

        var combinedSweep = last.SweepAngle + first.SweepAngle;
        if (Math.Abs(combinedSweep) >= Math.Tau - (Tolerance.Angular * 10.0))
        {
            return segments; // spajanje bi dalo puni krug — ostavi šav
        }

        var merged = new ArcSeg(last.Center, last.Radius, last.StartAngle, combinedSweep);

        var result = new List<ISegment>(segments.Count - 1) { merged };
        for (var k = 1; k < segments.Count - 1; k++)
        {
            result.Add(segments[k]);
        }

        return result;
    }

    /// <summary>Najdalji j takav da su SVE točke i..j unutar tolerancije od tetive p[i]→p[j].</summary>
    private static int ExtendLine(List<Point2> pts, int i, double tolerance)
    {
        var j = i + 1;
        while (j + 1 < pts.Count)
        {
            var candidate = j + 1;
            if (pts[i].DistanceTo(pts[candidate]) <= Tolerance.Geometric)
            {
                break; // degenerirana tetiva
            }

            var chord = new LineSeg(pts[i], pts[candidate]);
            var allWithin = true;
            for (var k = i + 1; k < candidate; k++)
            {
                if (chord.DistanceTo(pts[k]) > tolerance)
                {
                    allWithin = false;
                    break;
                }
            }

            if (!allWithin)
            {
                break;
            }

            j = candidate;
        }

        return j;
    }

    /// <summary>
    /// Najdalji j za koji postoji VERIFICIRAN luk kroz p[i]..p[j]; vraća i sam luk.
    ///
    /// KLJUČNO: NE raste pohlepno od najmanjeg prozora. Rekonstrukcija kružnice iz
    /// KRATKOG isječka luka je loše uvjetovan problem — ulazne točke nose
    /// kvantizacijski šum Clippera (±0.5 µm), koji pri malom kutnom rasponu naraste
    /// u pogrešku centra od 0.1 mm i sruši verifikaciju. Zato tražimo NAJVEĆI prozor
    /// koji prolazi verifikaciju (eksponencijalna pretraga + binarno sužavanje):
    /// veliki prozor je dobro uvjetovan, šum se poništava, luk se pronađe.
    /// </summary>
    private static int ExtendArc(List<Point2> pts, int i, double tolerance, out ArcSeg? best)
    {
        best = null;
        var bestEnd = i + 1;

        var maxEnd = pts.Count - 1;
        if (maxEnd - i + 1 < MinArcPoints)
        {
            return bestEnd;
        }

        // Faza 1: eksponencijalno traži gornju granicu koja NE prolazi.
        var lo = i + MinArcPoints - 1;   // najmanji dopušteni prozor
        var step = MinArcPoints;
        var hi = lo;

        while (hi <= maxEnd)
        {
            var arc = TryBuildVerifiedArc(pts, i, hi, tolerance);
            if (arc is null)
            {
                break;
            }

            best = arc;
            bestEnd = hi;
            lo = hi;
            step *= 2;
            hi = Math.Min(i + step, maxEnd);
            if (lo == maxEnd)
            {
                break;
            }
        }

        // Faza 2: binarno sužavanje između zadnjeg uspjeha (lo) i prvog neuspjeha (hi).
        var upper = Math.Min(hi, maxEnd);
        while (lo + 1 < upper)
        {
            var mid = lo + ((upper - lo) / 2);
            var arc = TryBuildVerifiedArc(pts, i, mid, tolerance);
            if (arc is not null)
            {
                best = arc;
                bestEnd = mid;
                lo = mid;
            }
            else
            {
                upper = mid;
            }
        }

        return bestEnd;
    }

    private static ArcSeg? TryBuildVerifiedArc(List<Point2> pts, int i, int j, double tolerance)
    {
        if (j - i + 1 < MinArcPoints)
        {
            return null;
        }

        var chord = pts[i].DistanceTo(pts[j]) > Tolerance.Geometric ? new LineSeg(pts[i], pts[j]) : null;
        if (chord is null)
        {
            return null;
        }

        // Najudaljenija točka od tetive — mjera "zakrivljenosti" prozora.
        var farIndex = i + 1;
        var farDistance = -1.0;
        for (var k = i + 1; k < j; k++)
        {
            var d = chord.DistanceTo(pts[k]);
            if (d > farDistance)
            {
                farDistance = d;
                farIndex = k;
            }
        }

        if (farDistance <= tolerance / 2.0)
        {
            return null; // praktički ravno — linija je ispravniji izbor
        }

        // Centar i polumjer LEAST-SQUARES fitom preko SVIH točaka prozora (Kåsa).
        // Kružnica kroz tri točke je loše uvjetovana na šumovitom ulazu; LSQ šum poništava.
        if (!TryFitCircle(pts, i, j, out var center, out var radius))
        {
            return null;
        }

        if (radius > MaxArcRadius || radius <= Tolerance.Geometric)
        {
            return null;
        }

        // Smjer: putovanje i → far → j.
        var a0 = (pts[i] - center).Angle;
        var aFar = MathUtil.NormalizeAngle((pts[farIndex] - center).Angle - a0);
        var aEnd = MathUtil.NormalizeAngle((pts[j] - center).Angle - a0);
        var isCcw = aFar < aEnd;

        // VERIFIKACIJA (garancija točnosti): svaka točka unutar tolerancije od luka,
        // kutovi strogo monotoni duž smjera obilaska.
        var previousOffset = 0.0;
        for (var k = i; k <= j; k++)
        {
            if (Math.Abs(center.DistanceTo(pts[k]) - radius) > tolerance)
            {
                return null;
            }

            if (k == i)
            {
                continue;
            }

            var offset = MathUtil.NormalizeAngle((pts[k] - center).Angle - a0);
            var forward = isCcw ? offset : MathUtil.NormalizeAngle(-offset);
            var previousForward = isCcw ? previousOffset : MathUtil.NormalizeAngle(-previousOffset);
            if (k > i + 1 && forward <= previousForward)
            {
                return null; // kut se vraća — nije jednostavan luk
            }

            previousOffset = offset;
        }

        var sweep = isCcw ? aEnd : aEnd - Math.Tau;
        if (Math.Abs(sweep) >= Math.Tau - (Tolerance.Angular * 10.0))
        {
            return null; // puni krug preko fitanja nije dopušten (šav ostaje)
        }

        // Luk se sidri na STVARNE krajnje točke ulaza (kontinuitet putanje),
        // uz centar iz LSQ fita — polumjer se preračunava iz početne točke.
        var anchoredRadius = center.DistanceTo(pts[i]);
        return new ArcSeg(center, anchoredRadius, a0, sweep);
    }

    /// <summary>
    /// Least-squares fit kružnice (Kåsa): minimizira Σ(x² + y² − 2·cx·x − 2·cy·y − k)².
    /// Linearan sustav 3×3 — stabilan na šumovitom ulazu, za razliku od kružnice kroz
    /// tri točke koja pri malom kutnom rasponu eksplodira.
    /// </summary>
    private static bool TryFitCircle(List<Point2> pts, int i, int j, out Point2 center, out double radius)
    {
        center = default;
        radius = 0.0;

        double sx = 0, sy = 0, sz = 0, sxx = 0, syy = 0, sxy = 0, sxz = 0, syz = 0;
        var n = j - i + 1;
        for (var k = i; k <= j; k++)
        {
            var x = pts[k].X;
            var y = pts[k].Y;
            var z = (x * x) + (y * y);
            sx += x;
            sy += y;
            sz += z;
            sxx += x * x;
            syy += y * y;
            sxy += x * y;
            sxz += x * z;
            syz += y * z;
        }

        // Sustav: [sxx sxy sx][a]   [sxz]
        //         [sxy syy sy][b] = [syz]     gdje je centar = (a/2, b/2)
        //         [sx  sy  n ][c]   [sz ]
        Span<double> m = stackalloc double[12]
        {
            sxx, sxy, sx, sxz,
            sxy, syy, sy, syz,
            sx,  sy,  n,  sz,
        };

        // Gaussova eliminacija s parcijalnim pivotiranjem.
        for (var col = 0; col < 3; col++)
        {
            var pivot = col;
            for (var row = col + 1; row < 3; row++)
            {
                if (Math.Abs(m[(row * 4) + col]) > Math.Abs(m[(pivot * 4) + col]))
                {
                    pivot = row;
                }
            }

            if (Math.Abs(m[(pivot * 4) + col]) < 1e-12)
            {
                return false; // singularno — točke su kolinearne
            }

            if (pivot != col)
            {
                for (var c = 0; c < 4; c++)
                {
                    (m[(col * 4) + c], m[(pivot * 4) + c]) = (m[(pivot * 4) + c], m[(col * 4) + c]);
                }
            }

            for (var row = col + 1; row < 3; row++)
            {
                var factor = m[(row * 4) + col] / m[(col * 4) + col];
                for (var c = col; c < 4; c++)
                {
                    m[(row * 4) + c] -= factor * m[(col * 4) + c];
                }
            }
        }

        Span<double> solution = stackalloc double[3];
        for (var row = 2; row >= 0; row--)
        {
            var sum = m[(row * 4) + 3];
            for (var c = row + 1; c < 3; c++)
            {
                sum -= m[(row * 4) + c] * solution[c];
            }

            solution[row] = sum / m[(row * 4) + row];
        }

        var cx = solution[0] / 2.0;
        var cy = solution[1] / 2.0;
        var rSquared = solution[2] + (cx * cx) + (cy * cy);
        if (rSquared <= 0 || !double.IsFinite(rSquared))
        {
            return false;
        }

        center = new Point2(cx, cy);
        radius = Math.Sqrt(rSquared);
        return double.IsFinite(radius);
    }

}
