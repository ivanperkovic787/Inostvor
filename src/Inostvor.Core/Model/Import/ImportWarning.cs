namespace Inostvor.Core.Model.Import;

/// <summary>Nefatalan problem tijekom importa — datoteka je učitana, ali korisnik mora znati.</summary>
public sealed record ImportWarning(string Code, string Message, string? Handle = null);

/// <summary>Stabilni kodovi upozorenja importa (za testove i buduću lokalizaciju).</summary>
public static class ImportWarningCodes
{
    public const string DegenerateEntity = "DEGENERATE_ENTITY";
    public const string UnsupportedEntity = "UNSUPPORTED_ENTITY";
    public const string UnsupportedNormal = "UNSUPPORTED_NORMAL";
    public const string NonUniformScale = "NONUNIFORM_SCALE";
    public const string HiddenLayerSkipped = "HIDDEN_LAYER_SKIPPED";
    public const string UnitlessAssumedMm = "UNITLESS_ASSUMED_MM";
    public const string UnknownUnitsAssumedMm = "UNKNOWN_UNITS_ASSUMED_MM";
    public const string CyclicBlock = "CYCLIC_BLOCK";
    public const string InsertDepthExceeded = "INSERT_DEPTH_EXCEEDED";
    public const string ArcEqualAngles = "ARC_EQUAL_ANGLES";
    public const string NonPlanarFlattened = "NONPLANAR_FLATTENED";
    public const string SplineTessellated = "SPLINE_TESSELLATED";
    public const string EllipseTessellated = "ELLIPSE_TESSELLATED";
    public const string WarningLimitReached = "WARNING_LIMIT_REACHED";
}
