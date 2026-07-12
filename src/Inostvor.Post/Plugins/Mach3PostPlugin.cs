using Inostvor.Core.Model.Machines;
using Inostvor.Post.Emission;
using Inostvor.Sdk.Post;

namespace Inostvor.Post.Plugins;

/// <summary>Mach3 dijalekt — OBIČAN plugin bez privilegija (ADR-004).</summary>
public sealed class Mach3PostPlugin : IPostProcessorPlugin
{
    public string Id => "inostvor.post.mach3";

    public string DisplayName => "Mach3";

    public GCodeDialect DefaultDialect { get; } = new()
    {
        Name = "Mach3",
        FileExtension = ".tap",
        Decimals = 3,
        HeaderLines =
        [
            "( Inostvor - {POST} - {MACHINE} )",
            "( Sekvenci: {SEQUENCES} | Rez: {CUTLENGTH} mm | Vrijeme: {TOTALTIME} s )",
        ],
        FooterLines = ["M05", "M30"],
    };

    public IPostProcessor Create(GCodeDialect dialect, MachineProfile profile)
        => new GCodePostBase(dialect, profile);
}
