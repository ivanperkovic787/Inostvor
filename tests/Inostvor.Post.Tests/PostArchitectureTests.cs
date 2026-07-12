using Inostvor.Post.Plugins;
using Inostvor.Sdk.Post;
using Shouldly;
using Xunit;

namespace Inostvor.Post.Tests;

public sealed class PostArchitectureTests
{
    [Fact]
    public void Katalog_RezolucijaPoIdu()
    {
        var catalog = new PostProcessorCatalog([new Mach3PostPlugin(), new Ec300PostPlugin()]);

        catalog.Plugins.Count.ShouldBe(2);
        catalog.Find("inostvor.post.mach3")!.DisplayName.ShouldBe("Mach3");
        catalog.Find("inostvor.post.ec300").ShouldNotBeNull();
        catalog.Find("inostvor.post.nepostoji").ShouldBeNull();
    }

    [Fact]
    public void JedanDijalekt_ViseStrojeva()
    {
        // Mach3 Plasma i Mach3 Router: ISTI plugin, različiti profili → različit G-kod.
        var plugin = new Mach3PostPlugin();
        var program = GoldenTestProgram.Build();

        var plasma = plugin.Create(plugin.DefaultDialect, BuiltInMachineProfiles.Mach3Plasma).Generate(program).GCode;
        var router = plugin.Create(plugin.DefaultDialect, BuiltInMachineProfiles.Mach3Router).Generate(program).GCode;

        plasma.ShouldContain("G00 Z3.8");   // pierce visina plazme
        router.ShouldContain("G00 Z5");     // pierce visina routera
        router.ShouldContain("Z-3");        // dubina prolaza
        plasma.ShouldNotBe(router);
    }

    [Fact]
    public void Uc300Eth_JeMach3Profil_NePosebanPost()
    {
        BuiltInMachineProfiles.Uc300EthPlasma.PostProcessorId.ShouldBe("inostvor.post.mach3");
    }

    [Fact]
    public void ProbeMacro_IzProfila_UPierceSekvenci()
    {
        var plugin = new Ec300PostPlugin();
        var profile = BuiltInMachineProfiles.Ec300Plasma with { ProbeMacro = "M101" };

        var gcode = plugin.Create(plugin.DefaultDialect, profile).Generate(GoldenTestProgram.Build()).GCode;

        var lines = gcode.Split('\n').ToList();
        var probeIndex = lines.IndexOf("M101");
        probeIndex.ShouldBeGreaterThan(0);
        lines[probeIndex + 1].ShouldBe("G00 Z3.8"); // probe PRIJE spuštanja na pierce visinu
    }

    [Fact]
    public void Postprocesor_NeMijenjaProgram()
    {
        // Redoslijed emitiranih sekvenci == redoslijed programa (post ne smije mijenjati CAM odluke).
        var program = GoldenTestProgram.Build();
        var plugin = new Mach3PostPlugin();

        var gcode = plugin.Create(plugin.DefaultDialect, BuiltInMachineProfiles.Mach3Plasma).Generate(program).GCode;

        gcode.IndexOf("kontura #7", StringComparison.Ordinal)
            .ShouldBeLessThan(gcode.IndexOf("kontura #9", StringComparison.Ordinal));
        program.Sequences[0].SourceContourId.ShouldBe(7); // ulaz netaknut (record immutability)
    }
}
