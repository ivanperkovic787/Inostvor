// ============================================================================
// JEDINA datoteka u sustavu koja dodiruje netDxf API (izolacija adaptera).
// Pretpostavljena netDxf 3.x API površina (verifikacija na prvom Windows buildu):
//   - DxfDocument.Load(path), DxfDocument.CheckDxfFileVersion(path, out bool)
//   - doc.Entities.All : IEnumerable<EntityObject>
//   - doc.DrawingVariables.InsUnits : netDxf.Units.DrawingUnits
//   - Line, Arc (stupnjevi, uvijek CCW), Circle, Polyline2D (Vertexes: Position/Bulge,
//     IsClosed), Polyline3D, Spline (PolygonalVertexes(int), IsClosed), Ellipse
//     (MajorAxis/MinorAxis = PUNE duljine osi, Rotation/StartAngle/EndAngle stupnjevi,
//     IsFullEllipse), Insert (Block.Origin/Entities, Position, Scale, Rotation)
//   - EntityObject.Layer (Name/IsVisible/IsFrozen), .Handle, .Normal
// Svako odstupanje API-ja lomi se ISKLJUČIVO ovdje — popravak je lokalan.
// ============================================================================
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Units;
using PlasmaCAM.Core.Abstractions;
using PlasmaCAM.Core.Model.Import;
using PlasmaCAM.Kernel;
using PlasmaCAM.Kernel.Primitives;
using PlasmaCAM.Kernel.Transforms;

namespace PlasmaCAM.Import.NetDxf;

/// <summary>
/// <see cref="IDxfImporter"/> implementacija nad netDxf bibliotekom.
/// Podržava DXF AutoCad2000+ (netDxf ograničenje — R12 zahtijeva drugi importer).
/// Sva geometrija izlazi u milimetrima, world koordinatama, s razriješenim
/// INSERT transformacijama i nasljeđivanjem layera.
/// </summary>
public sealed class NetDxfImporter : IDxfImporter
{
    private readonly ImportSettings _settings;

    public NetDxfImporter(ImportSettings? settings = null) => _settings = settings ?? ImportSettings.Default;

    public string Name => "netDxf";

    public ImportResult Import(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ImportResult.Fail("Putanja datoteke nije zadana.");
        }

        if (!File.Exists(filePath))
        {
            return ImportResult.Fail(FormattableString.Invariant($"Datoteka ne postoji: {filePath}"));
        }

        try
        {
            var version = DxfDocument.CheckDxfFileVersion(filePath, out _);
            if (version == DxfVersion.Unknown)
            {
                return ImportResult.Fail("Datoteka nije prepoznata kao DXF.");
            }

            if (version < DxfVersion.AutoCad2000)
            {
                return ImportResult.Fail(FormattableString.Invariant(
                    $"DXF verzija '{version}' nije podržana netDxf parserom (minimalno AutoCad2000). " +
                    "Starije datoteke (R12/R13/R14) spremiti u noviji format ili koristiti drugi importer."));
            }

            var doc = DxfDocument.Load(filePath);
            if (doc is null)
            {
                return ImportResult.Fail("netDxf nije uspio učitati datoteku (oštećena ili nevaljana struktura).");
            }

            return MapDocument(doc);
        }
        catch (Exception ex)
        {
            // Import je granica sustava: parser iznimke postaju Fail rezultat (Baseline v1.1).
            return ImportResult.Fail(FormattableString.Invariant($"Greška pri čitanju DXF-a: {ex.Message}"));
        }
    }

    // ------------------------------------------------------------------ mapiranje

    private ImportResult MapDocument(DxfDocument doc)
    {
        var warnings = new WarningCollector(_settings.MaxWarnings);
        var (unitsName, scale) = ResolveUnits(doc, warnings);
        var rootMatrix = Matrix3x2d.CreateScale(scale, scale);

        var entities = new List<ImportedEntity>();
        var blockStack = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entity in doc.Entities.All)
        {
            MapEntity(entity, rootMatrix, inheritedLayer: null, sourcePrefix: string.Empty, entities, warnings, blockStack, depth: 0);
        }

        var layers = entities.Select(e => e.Layer).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        return ImportResult.Ok(entities, warnings.Items, unitsName, scale, layers);
    }

    private static (string Name, double ScaleToMm) ResolveUnits(DxfDocument doc, WarningCollector warnings)
    {
        var units = doc.DrawingVariables.InsUnits;
        switch (units)
        {
            case DrawingUnits.Millimeters: return ("Millimeters", 1.0);
            case DrawingUnits.Centimeters: return ("Centimeters", 10.0);
            case DrawingUnits.Meters: return ("Meters", 1000.0);
            case DrawingUnits.Inches: return ("Inches", 25.4);
            case DrawingUnits.Feet: return ("Feet", 304.8);
            case DrawingUnits.Unitless:
                warnings.Add(ImportWarningCodes.UnitlessAssumedMm, "Datoteka nema definirane jedinice ($INSUNITS=0) — pretpostavljeni milimetri.");
                return ("Unitless", 1.0);
            default:
                warnings.Add(
                    ImportWarningCodes.UnknownUnitsAssumedMm,
                    FormattableString.Invariant($"Jedinice '{units}' nisu podržane — pretpostavljeni milimetri."));
                return (units.ToString(), 1.0);
        }
    }

    private void MapEntity(
        EntityObject entity,
        in Matrix3x2d parentMatrix,
        string? inheritedLayer,
        string sourcePrefix,
        List<ImportedEntity> output,
        WarningCollector warnings,
        HashSet<string> blockStack,
        int depth)
    {
        var layer = ResolveEffectiveLayer(entity, inheritedLayer);

        if (entity.Layer is not null && (entity.Layer.IsFrozen || !entity.Layer.IsVisible))
        {
            warnings.Add(
                ImportWarningCodes.HiddenLayerSkipped,
                FormattableString.Invariant($"Entitet na zamrznutom/skrivenom layeru '{entity.Layer.Name}' preskočen."),
                entity.Handle);
            return;
        }

        if (!TryGetOcsMatrix(entity, warnings, out var ocs))
        {
            return;
        }

        var matrix = ocs * parentMatrix;

        switch (entity)
        {
            case Insert insert:
                MapInsert(insert, matrix, layer, sourcePrefix, output, warnings, blockStack, depth);
                break;

            case Line line:
                MapLine(line, matrix, layer, sourcePrefix, output, warnings);
                break;

            case Circle circle:
                MapArcLike(
                    new Point2(circle.Center.X, circle.Center.Y), circle.Radius, 0.0, Math.Tau,
                    circle, "CIRCLE", matrix, layer, sourcePrefix, output, warnings);
                CheckPlanar(circle.Center.Z, circle, warnings);
                break;

            case Arc arc:
            {
                var start = MathUtil.DegToRad(arc.StartAngle);
                var sweep = MathUtil.NormalizeAngle(MathUtil.DegToRad(arc.EndAngle) - start);
                if (sweep * arc.Radius < Tolerance.Geometric)
                {
                    // DXF ARC s jednakim kutovima: dogovorno puni krug, uz upozorenje.
                    sweep = Math.Tau;
                    warnings.Add(ImportWarningCodes.ArcEqualAngles, "ARC s jednakim početnim i krajnjim kutom tretiran kao puni krug.", arc.Handle);
                }

                MapArcLike(new Point2(arc.Center.X, arc.Center.Y), arc.Radius, start, sweep, arc, "ARC", matrix, layer, sourcePrefix, output, warnings);
                CheckPlanar(arc.Center.Z, arc, warnings);
                break;
            }

            case Polyline2D poly:
                MapPolyline2D(poly, matrix, layer, sourcePrefix, output, warnings);
                break;

            case Polyline3D poly3:
                MapPolyline3D(poly3, matrix, layer, sourcePrefix, output, warnings);
                break;

            case Spline spline:
                MapSampledCurve(
                    SampleSpline(spline), spline.IsClosed, spline, "SPLINE",
                    ImportWarningCodes.SplineTessellated, "SPLINE tesselliran u linijske segmente.",
                    matrix, layer, sourcePrefix, output, warnings);
                break;

            case Ellipse ellipse:
                MapSampledCurve(
                    SampleEllipse(ellipse), ellipse.IsFullEllipse, ellipse, "ELLIPSE",
                    ImportWarningCodes.EllipseTessellated, "ELLIPSE tessellirana u linijske segmente.",
                    matrix, layer, sourcePrefix, output, warnings);
                break;

            default:
                warnings.Add(
                    ImportWarningCodes.UnsupportedEntity,
                    FormattableString.Invariant($"Entitet tipa '{entity.GetType().Name}' nije podržan i preskočen je."),
                    entity.Handle);
                break;
        }
    }

    // ------------------------------------------------------------------ pojedinačni mapperi

    private void MapInsert(
        Insert insert,
        in Matrix3x2d matrix,
        string effectiveLayer,
        string sourcePrefix,
        List<ImportedEntity> output,
        WarningCollector warnings,
        HashSet<string> blockStack,
        int depth)
    {
        if (depth >= _settings.MaxInsertDepth)
        {
            warnings.Add(
                ImportWarningCodes.InsertDepthExceeded,
                FormattableString.Invariant($"INSERT dubina veća od {_settings.MaxInsertDepth} — grana preskočena."),
                insert.Handle);
            return;
        }

        var blockName = insert.Block.Name;
        if (!blockStack.Add(blockName))
        {
            warnings.Add(
                ImportWarningCodes.CyclicBlock,
                FormattableString.Invariant($"Ciklična referenca bloka '{blockName}' — grana preskočena."),
                insert.Handle);
            return;
        }

        try
        {
            // Blok koordinate → world: p' = InsertPos + Rot·Scale·(p − BasePoint)
            var local = Matrix3x2d.CreateTranslation(-insert.Block.Origin.X, -insert.Block.Origin.Y)
                      * Matrix3x2d.CreateScale(insert.Scale.X, insert.Scale.Y)
                      * Matrix3x2d.CreateRotation(MathUtil.DegToRad(insert.Rotation))
                      * Matrix3x2d.CreateTranslation(insert.Position.X, insert.Position.Y);

            var total = local * matrix;
            var childPrefix = string.IsNullOrEmpty(sourcePrefix) ? "INSERT/" : sourcePrefix + "INSERT/";

            foreach (var child in insert.Block.Entities)
            {
                // Nasljeđivanje: entiteti bloka na layeru "0" preuzimaju layer INSERT-a.
                MapEntity(child, total, inheritedLayer: effectiveLayer, childPrefix, output, warnings, blockStack, depth + 1);
            }
        }
        finally
        {
            blockStack.Remove(blockName);
        }
    }

    private static void MapLine(
        Line line, in Matrix3x2d matrix, string layer, string sourcePrefix,
        List<ImportedEntity> output, WarningCollector warnings)
    {
        CheckPlanar(line.StartPoint.Z, line, warnings);
        var p1 = matrix.TransformPoint(new Point2(line.StartPoint.X, line.StartPoint.Y));
        var p2 = matrix.TransformPoint(new Point2(line.EndPoint.X, line.EndPoint.Y));

        if (p1.DistanceTo(p2) < Tolerance.Geometric)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, "LINE nulte duljine preskočena.", line.Handle);
            return;
        }

        output.Add(new ImportedEntity([new LineSeg(p1, p2)], layer, sourcePrefix + "LINE", line.Handle));
    }

    private void MapArcLike(
        Point2 center, double radius, double startAngle, double sweep,
        EntityObject source, string sourceType, in Matrix3x2d matrix, string layer,
        string sourcePrefix, List<ImportedEntity> output, WarningCollector warnings)
    {
        if (radius < Tolerance.Geometric)
        {
            warnings.Add(
                ImportWarningCodes.DegenerateEntity,
                FormattableString.Invariant($"{sourceType} polumjera ~0 preskočen."),
                source.Handle);
            return;
        }

        var raw = new ArcSeg(center, radius, startAngle, sweep);
        var segments = SegmentTransform.Transform(raw, matrix, _settings.TessellationTolerance, out var tessellated);

        if (segments.Count == 0)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, FormattableString.Invariant($"{sourceType} degenerirao pod transformacijom."), source.Handle);
            return;
        }

        if (tessellated)
        {
            warnings.Add(
                ImportWarningCodes.NonUniformScale,
                FormattableString.Invariant($"{sourceType} pod neuniformnom skalom tesselliran u {segments.Count} linijskih segmenata."),
                source.Handle);
        }

        output.Add(new ImportedEntity(segments, layer, sourcePrefix + sourceType, source.Handle));
    }

    private void MapPolyline2D(
        Polyline2D poly, in Matrix3x2d matrix, string layer, string sourcePrefix,
        List<ImportedEntity> output, WarningCollector warnings)
    {
        var vertices = poly.Vertexes;
        if (vertices.Count < 2)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, "LWPOLYLINE s manje od 2 vrha preskočena.", poly.Handle);
            return;
        }

        var segments = new List<ISegment>();
        var pairCount = poly.IsClosed ? vertices.Count : vertices.Count - 1;

        for (var i = 0; i < pairCount; i++)
        {
            var v0 = vertices[i];
            var v1 = vertices[(i + 1) % vertices.Count];
            var a = new Point2(v0.Position.X, v0.Position.Y);
            var b = new Point2(v1.Position.X, v1.Position.Y);

            if (a.DistanceTo(b) < Tolerance.Geometric)
            {
                warnings.Add(ImportWarningCodes.DegenerateEntity, "Duplicirani vrh polyline preskočen.", poly.Handle);
                continue;
            }

            ISegment local = Math.Abs(v0.Bulge) < Tolerance.Angular
                ? new LineSeg(a, b)
                : ArcSeg.FromBulge(a, b, v0.Bulge);

            var transformed = SegmentTransform.Transform(local, matrix, _settings.TessellationTolerance, out var tess);
            if (tess)
            {
                warnings.Add(ImportWarningCodes.NonUniformScale, "Bulge segment polyline tesselliran (neuniformna skala).", poly.Handle);
            }

            segments.AddRange(transformed);
        }

        if (segments.Count == 0)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, "LWPOLYLINE bez validnih segmenata preskočena.", poly.Handle);
            return;
        }

        output.Add(new ImportedEntity(segments, layer, sourcePrefix + "LWPOLYLINE", poly.Handle));
    }

    private static void MapPolyline3D(
        Polyline3D poly, in Matrix3x2d matrix, string layer, string sourcePrefix,
        List<ImportedEntity> output, WarningCollector warnings)
    {
        var vertices = poly.Vertexes;
        if (vertices.Count < 2)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, "POLYLINE s manje od 2 vrha preskočena.", poly.Handle);
            return;
        }

        var nonPlanar = false;
        var segments = new List<ISegment>();
        var pairCount = poly.IsClosed ? vertices.Count : vertices.Count - 1;

        for (var i = 0; i < pairCount; i++)
        {
            var v0 = vertices[i];
            var v1 = vertices[(i + 1) % vertices.Count];
            nonPlanar |= Math.Abs(v0.Z) > Tolerance.Geometric;

            var a = matrix.TransformPoint(new Point2(v0.X, v0.Y));
            var b = matrix.TransformPoint(new Point2(v1.X, v1.Y));
            if (a.DistanceTo(b) >= Tolerance.Geometric)
            {
                segments.Add(new LineSeg(a, b));
            }
        }

        if (nonPlanar)
        {
            warnings.Add(ImportWarningCodes.NonPlanarFlattened, "3D POLYLINE spljoštena u XY ravninu (Z odbačen).", poly.Handle);
        }

        if (segments.Count == 0)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, "POLYLINE bez validnih segmenata preskočena.", poly.Handle);
            return;
        }

        output.Add(new ImportedEntity(segments, layer, sourcePrefix + "POLYLINE", poly.Handle));
    }

    private static void MapSampledCurve(
        IReadOnlyList<Point2> localPoints, bool isClosed, EntityObject source, string sourceType,
        string warningCode, string warningMessage, in Matrix3x2d matrix, string layer,
        string sourcePrefix, List<ImportedEntity> output, WarningCollector warnings)
    {
        if (localPoints.Count < 2)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, FormattableString.Invariant($"{sourceType} bez upotrebljive geometrije preskočen."), source.Handle);
            return;
        }

        var segments = new List<ISegment>(localPoints.Count);
        var previous = matrix.TransformPoint(localPoints[0]);
        var first = previous;

        for (var i = 1; i < localPoints.Count; i++)
        {
            var current = matrix.TransformPoint(localPoints[i]);
            if (previous.DistanceTo(current) >= Tolerance.Geometric)
            {
                segments.Add(new LineSeg(previous, current));
                previous = current;
            }
        }

        if (isClosed && previous.DistanceTo(first) >= Tolerance.Geometric)
        {
            segments.Add(new LineSeg(previous, first));
        }

        if (segments.Count == 0)
        {
            warnings.Add(ImportWarningCodes.DegenerateEntity, FormattableString.Invariant($"{sourceType} degenerirao pri tessellaciji."), source.Handle);
            return;
        }

        warnings.Add(warningCode, warningMessage, source.Handle);
        output.Add(new ImportedEntity(segments, layer, sourcePrefix + sourceType, source.Handle));
    }

    // ------------------------------------------------------------------ uzorkovanje krivulja

    /// <summary>
    /// Adaptivno uzorkovanje splinea: broj uzoraka se udvostručuje dok duljina
    /// poligona ne konvergira (relativna promjena &lt; Tolerance.Relative·10⁴, tj. 1e-5).
    /// Konvergencija duljine je robustan proxy za odstupanje tetive bez pristupa
    /// parametrizaciji krivulje.
    /// </summary>
    private static List<Point2> SampleSpline(Spline spline)
    {
        List<Point2> Sample(int precision)
        {
            var raw = spline.PolygonalVertexes(precision);
            var pts = new List<Point2>(raw.Count);
            foreach (var v in raw)
            {
                pts.Add(new Point2(v.X, v.Y));
            }

            return pts;
        }

        static double PolyLength(List<Point2> pts)
        {
            var len = 0.0;
            for (var i = 1; i < pts.Count; i++)
            {
                len += pts[i - 1].DistanceTo(pts[i]);
            }

            return len;
        }

        var precision = 64;
        var points = Sample(precision);
        var length = PolyLength(points);

        while (precision < 1024)
        {
            precision *= 2;
            var refined = Sample(precision);
            var refinedLength = PolyLength(refined);
            var converged = Math.Abs(refinedLength - length) <= Math.Max(refinedLength, 1.0) * 1e-5;
            points = refined;
            length = refinedLength;
            if (converged)
            {
                break;
            }
        }

        return points;
    }

    /// <summary>
    /// Uzorkovanje elipse s korakom izvedenim iz najmanjeg polumjera zakrivljenosti
    /// (b²/a na krajevima velike osi) — sagitta tetive ostaje unutar tolerancije.
    /// NAPOMENA: netDxf MajorAxis/MinorAxis su PUNE duljine osi (ne polu-osi).
    /// </summary>
    private List<Point2> SampleEllipse(Ellipse ellipse)
    {
        var a = ellipse.MajorAxis / 2.0;
        var b = ellipse.MinorAxis / 2.0;
        if (a < Tolerance.Geometric || b < Tolerance.Geometric)
        {
            return [];
        }

        var startParam = MathUtil.DegToRad(ellipse.StartAngle);
        var endParam = MathUtil.DegToRad(ellipse.EndAngle);
        double range;
        if (ellipse.IsFullEllipse)
        {
            range = Math.Tau;
        }
        else
        {
            range = MathUtil.NormalizeAngle(endParam - startParam);
            if (range < Tolerance.Angular)
            {
                range = Math.Tau;
            }
        }

        var minCurvatureRadius = (b * b) / a;
        var ratio = Math.Min(_settings.TessellationTolerance / minCurvatureRadius, 1.0);
        var maxStep = 2.0 * Math.Acos(1.0 - ratio);
        var steps = Math.Clamp((int)Math.Ceiling(range / maxStep), 8, Tessellation.MaxChordsPerArc);

        var rotation = MathUtil.DegToRad(ellipse.Rotation);
        var cosR = Math.Cos(rotation);
        var sinR = Math.Sin(rotation);
        var cx = ellipse.Center.X;
        var cy = ellipse.Center.Y;

        var points = new List<Point2>(steps + 1);
        for (var i = 0; i <= steps; i++)
        {
            var t = startParam + (range * i / steps);
            var ex = a * Math.Cos(t);
            var ey = b * Math.Sin(t);
            points.Add(new Point2(cx + (ex * cosR) - (ey * sinR), cy + (ex * sinR) + (ey * cosR)));
        }

        return points;
    }

    // ------------------------------------------------------------------ pomoćne

    private static string ResolveEffectiveLayer(EntityObject entity, string? inheritedLayer)
    {
        var own = entity.Layer?.Name ?? "0";

        // AutoCAD pravilo: entiteti bloka na layeru "0" nasljeđuju layer INSERT-a.
        return own == "0" && inheritedLayer is not null ? inheritedLayer : own;
    }

    /// <summary>
    /// OCS (Object Coordinate System): podržane su normale ±Z. Normala -Z znači
    /// zrcaljenje po X osi (npr. MIRROR u AutoCAD-u); proizvoljne 3D normale se preskaču.
    /// </summary>
    private static bool TryGetOcsMatrix(EntityObject entity, WarningCollector warnings, out Matrix3x2d ocs)
    {
        var n = entity.Normal;
        if (Math.Abs(n.X) < Tolerance.Relative && Math.Abs(n.Y) < Tolerance.Relative)
        {
            ocs = n.Z < 0 ? Matrix3x2d.CreateScale(-1.0, 1.0) : Matrix3x2d.Identity;
            return true;
        }

        warnings.Add(
            ImportWarningCodes.UnsupportedNormal,
            FormattableString.Invariant($"Entitet s proizvoljnom 3D normalom ({n.X:0.###}, {n.Y:0.###}, {n.Z:0.###}) preskočen — plazma radi u XY ravnini."),
            entity.Handle);
        ocs = Matrix3x2d.Identity;
        return false;
    }

    private static void CheckPlanar(double z, EntityObject entity, WarningCollector warnings)
    {
        if (Math.Abs(z) > Tolerance.Geometric)
        {
            warnings.Add(ImportWarningCodes.NonPlanarFlattened, "Entitet s Z ≠ 0 spljošten u XY ravninu.", entity.Handle);
        }
    }
}
