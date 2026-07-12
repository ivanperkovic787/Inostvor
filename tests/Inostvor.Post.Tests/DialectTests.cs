using Inostvor.Post.Plugins;
using Inostvor.Sdk.Post;
using Shouldly;
using Xunit;

namespace Inostvor.Post.Tests;

/// <summary>Deklarativne opcije dijalekta — svaka mijenja izlaz bez dodira koda.</summary>
public sealed class DialectTests
{
    private static string Generate(GCodeDialect dialect)
        => new Mach3PostPlugin().Create(dialect, BuiltInMachineProfiles.Mach3Plasma)
            .Generate(GoldenTestProgram.Build()).GCode;

    private static GCodeDialect Base() => new Mach3PostPlugin().DefaultDialect;

    [Fact]
    public void LinijskiBrojevi_PrefiksIKorak()
    {
        var gcode = Generate(Base() with { UseLineNumbers = true, LineNumberStart = 10, LineNumberStep = 5 });

        var lines = gcode.Split('\n');
        lines[0].ShouldStartWith("N10 ");
        lines[1].ShouldStartWith("N15 ");
    }

    [Fact]
    public void Inci_KoordinateIPosmakPretvoreni()
    {
        var gcode = Generate(Base() with { Units = UnitsMode.Inches, Decimals = 4 });

        gcode.ShouldContain("G20 G90");
        gcode.ShouldContain("X2.3622");    // 60 mm / 25.4
        gcode.ShouldContain("F118.1102");  // 3000 mm/min / 25.4
        gcode.ShouldNotContain("G21");
    }

    [Fact]
    public void KomentariIskljuceni_NemaZagrada()
    {
        var gcode = Generate(Base() with { EmitComments = false });
        gcode.ShouldNotContain("(");
    }

    [Fact]
    public void TockaZarezKomentari()
    {
        var gcode = Generate(Base() with { CommentStart = "; ", CommentEnd = "" });
        gcode.ShouldContain("; Sekvenca 1/2");
        gcode.ShouldNotContain("( Sekvenca");
    }

    [Fact]
    public void NemodalniKodovi_G01NaSvakojLiniji()
    {
        var gcode = Generate(Base() with { ModalMotionCodes = false });
        gcode.ShouldContain("G01 X60 Y20");
        gcode.ShouldContain("G01 X60 Y70");
    }

    [Fact]
    public void DrugaciijiTorchKodovi()
    {
        var gcode = Generate(Base() with { TorchOnCode = "M62 P0", TorchOffCode = "M63 P0" });
        gcode.ShouldContain("M62 P0");
        gcode.ShouldContain("M63 P0");
        gcode.ShouldNotContain("M03");
    }

    [Fact]
    public void Ekstenzija_IzDijalekta()
    {
        var plugin = new Mach3PostPlugin();
        var result = plugin.Create(plugin.DefaultDialect with { FileExtension = ".nc" }, BuiltInMachineProfiles.Mach3Plasma)
            .Generate(GoldenTestProgram.Build());
        result.FileExtension.ShouldBe(".nc");
    }
}
