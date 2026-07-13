using System.Reflection;

namespace Inostvor.ViewModels;

/// <summary>
/// Podaci za About dijalog — MODEL je spreman, UI dijalog dolazi kasnije
/// (planirano; ADR-003). Ime autora nije dio naziva programa.
/// </summary>
public sealed record OpenSourceLicense(string Component, string License);

public static class AboutInfo
{
    public const string ApplicationName = "Inostvor";

    public const string Author = "Ivan";

    public const string Greeting =
        "Hvala što koristite Inostvor. Neka rezovi budu čisti, a razmaci točni.";

    public const string Description =
        "Profesionalna CAM aplikacija za CNC rezanje: uvoz DXF-a, detekcija i validacija kontura, " +
        "kerf kompenzacija, leadovi, simulacija i generiranje G-koda kroz plugin postprocesore.";

    public static string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public static int Year => DateTime.Now.Year;

    public static string Copyright => FormattableString.Invariant($"© {Year} {Author}");

    /// <summary>Licence komponenti otvorenog koda koje Inostvor koristi.</summary>
    public static IReadOnlyList<OpenSourceLicense> Licenses { get; } =
    [
        new("netDxf", "MIT"),
        new("Clipper2", "BSL-1.0"),
        new("SkiaSharp", "MIT"),
        new("CommunityToolkit.Mvvm", "MIT"),
        new("Serilog", "Apache-2.0"),
        new("Dapper", "Apache-2.0"),
        new("Microsoft.Data.Sqlite", "MIT"),
        new("xUnit", "Apache-2.0"),
        new("Shouldly", "BSD-3-Clause"),
        new("BenchmarkDotNet", "MIT"),
    ];
}
