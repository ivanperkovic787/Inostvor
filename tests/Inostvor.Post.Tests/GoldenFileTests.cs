using Inostvor.Post.Plugins;
using Shouldly;
using Xunit;

namespace Inostvor.Post.Tests;

/// <summary>
/// Golden testovi (eksplicitni zahtjev): isti ToolpathProgram → BAJT-IDENTIČAN
/// izlaz. Promjena i jednog znaka emittera ruši test. Golden datoteke su ručno
/// izračunate prema pravilima emittera — na prvom Windows buildu ih verificirati
/// i po potrebi jednom potvrditi ("bless") pregledani ispravan izlaz.
/// </summary>
public sealed class GoldenFileTests
{
    [Fact]
    public void Mach3_GoldenFile_BajtIdentican()
    {
        var plugin = new Mach3PostPlugin();
        var post = plugin.Create(plugin.DefaultDialect, BuiltInMachineProfiles.Mach3Plasma);

        var result = post.Generate(GoldenTestProgram.Build());

        result.FileExtension.ShouldBe(".tap");
        result.GCode.ShouldBe(GoldenFileLocator.Read("mach3_basic.tap"));
    }

    [Fact]
    public void Ec300_GoldenFile_BajtIdentican()
    {
        var plugin = new Ec300PostPlugin();
        var post = plugin.Create(plugin.DefaultDialect, BuiltInMachineProfiles.Ec300Plasma);

        var result = post.Generate(GoldenTestProgram.Build());

        result.GCode.ShouldBe(GoldenFileLocator.Read("ec300_basic.tap"));
    }

    [Fact]
    public void Determinizam_PetGeneriranja_IdenticanString()
    {
        var plugin = new Mach3PostPlugin();
        var program = GoldenTestProgram.Build();
        var reference = plugin.Create(plugin.DefaultDialect, BuiltInMachineProfiles.Mach3Plasma).Generate(program).GCode;

        for (var i = 0; i < 5; i++)
        {
            plugin.Create(plugin.DefaultDialect, BuiltInMachineProfiles.Mach3Plasma)
                .Generate(program).GCode.ShouldBe(reference);
        }
    }
}
