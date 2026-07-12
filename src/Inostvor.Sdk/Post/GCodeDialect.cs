namespace Inostvor.Sdk.Post;

public enum UnitsMode
{
    /// <summary>G21.</summary>
    Millimeters = 0,

    /// <summary>G20 (koordinate se pretvaraju iz mm).</summary>
    Inches = 1,
}

public enum PositioningMode
{
    /// <summary>G90.</summary>
    Absolute = 0,

    /// <summary>G91 (rezervirano; emitter V1 podržava Absolute).</summary>
    Relative = 1,
}

/// <summary>
/// DEKLARATIVNI opis dijalekta G-koda — čisti podaci, bez koda. Sve što se
/// razlikuje među kontrolerima na razini formata živi ovdje; kontrolerske
/// SEKVENCE (pierce, probe) žive u kodu postprocesora. Record je namjerno
/// serializabilan bez ikakvog ponašanja: to je preduvjet budućeg grafičkog
/// editora postprocesora (uređivanje dijalekta = uređivanje podataka).
/// </summary>
public sealed record GCodeDialect
{
    public required string Name { get; init; }

    public string FileExtension { get; init; } = ".tap";

    /// <summary>Broj decimala koordinata i posmaka.</summary>
    public int Decimals { get; init; } = 3;

    public UnitsMode Units { get; init; } = UnitsMode.Millimeters;

    public PositioningMode Positioning { get; init; } = PositioningMode.Absolute;

    public bool EmitComments { get; init; } = true;

    /// <summary>Početak komentara; kraj prazan za ";" stil.</summary>
    public string CommentStart { get; init; } = "(";

    public string CommentEnd { get; init; } = ")";

    public bool UseLineNumbers { get; init; }

    public string LineNumberPrefix { get; init; } = "N";

    public int LineNumberStart { get; init; } = 10;

    public int LineNumberStep { get; init; } = 10;

    public string TorchOnCode { get; init; } = "M03";

    public string TorchOffCode { get; init; } = "M05";

    public string RapidCode { get; init; } = "G00";

    public string LinearCode { get; init; } = "G01";

    public string ArcCwCode { get; init; } = "G02";

    public string ArcCcwCode { get; init; } = "G03";

    /// <summary>Modalni motion kodovi: G01 se ne ponavlja na uzastopnim linijama.</summary>
    public bool ModalMotionCodes { get; init; } = true;

    /// <summary>Posmak se emitira samo pri promjeni.</summary>
    public bool EmitFeedOnlyOnChange { get; init; } = true;

    /// <summary>Zaglavlje; placeholderi: {NAME}, {DATE}, {UNITS}, {SEQUENCES}, {CUTLENGTH}, {TOTALTIME}.</summary>
    public IReadOnlyList<string> HeaderLines { get; init; } = [];

    public IReadOnlyList<string> FooterLines { get; init; } = ["M30"];
}
