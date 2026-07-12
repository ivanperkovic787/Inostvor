using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Post.Emission;
using Inostvor.Sdk.Post;

namespace Inostvor.Post.Plugins;

/// <summary>
/// EC300 (DigitalDream) — Mach3 dijalekt sa specifičnom pierce sekvencom:
/// dodatni G04 P0.3 nakon paljenja luka (stabilizacija prije glavnog dwella;
/// EC300 firmware nema pouzdan G31, touch-off ide kroz ProbeMacro profila).
/// OBIČAN plugin — ista pravila kao svi budući kontroleri.
/// </summary>
public sealed class Ec300PostPlugin : IPostProcessorPlugin
{
    public string Id => "inostvor.post.ec300";

    public string DisplayName => "EC300 (Mach3 dijalekt)";

    public GCodeDialect DefaultDialect { get; } = new()
    {
        Name = "EC300",
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
        => new Ec300Post(dialect, profile);

    private sealed class Ec300Post(GCodeDialect dialect, MachineProfile profile) : GCodePostBase(dialect, profile)
    {
        protected override void EmitPierceSequence(GCodeBuilder b, ToolpathProgram program, CutSequence sequence)
        {
            if (Profile.ProbeMacro.Length > 0)
            {
                b.Line(Profile.ProbeMacro);
            }

            b.Line(FormattableString.Invariant($"{Dialect.RapidCode} Z{b.Number(U(Profile.PierceHeight))}"));
            b.Line(Dialect.TorchOnCode);
            b.Line("G04 P0.3");
            b.Line(FormattableString.Invariant($"G04 P{b.Number(program.Technology.PierceTime)}"));
            b.Line(FormattableString.Invariant($"{Dialect.RapidCode} Z{b.Number(U(Profile.CutHeight))}"));
        }
    }
}
