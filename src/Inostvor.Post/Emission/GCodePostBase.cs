using System.Globalization;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Inostvor.Sdk.Post;

namespace Inostvor.Post.Emission;

/// <summary>
/// Zajednička emisijska jezgra: sve što je FORMAT dolazi iz deklarativnog
/// GCodeDialecta; sve što je KONTROLERSKA SEKVENCA je virtualni hook
/// (EmitPierceSequence, EmitCutEnd…) koji konkretni postprocesor smije
/// nadjačati. Program se emitira TOČNO onim redoslijedom kojim je došao —
/// postprocesor nikad ne mijenja redoslijed, leadove, kerf ni geometriju.
///
/// NAPOMENA O DETERMINIZMU: ugrađeni dijalekti namjerno NE koriste {DATE}
/// placeholder — isti program mora dati bajt-identičan izlaz (golden testovi).
/// </summary>
public class GCodePostBase : IPostProcessor
{
    public GCodePostBase(GCodeDialect dialect, MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(dialect);
        ArgumentNullException.ThrowIfNull(profile);
        Dialect = dialect;
        Profile = profile;
    }

    protected GCodeDialect Dialect { get; }

    protected MachineProfile Profile { get; }

    private string? _lastMotionCode;
    private double? _lastFeed;

    public PostResult Generate(ToolpathProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        var builder = new GCodeBuilder(Dialect);
        var warnings = new List<string>();
        _lastMotionCode = null;
        _lastFeed = null;

        EmitProgramStart(builder, program);

        for (var i = 0; i < program.Sequences.Count; i++)
        {
            var sequence = program.Sequences[i];
            builder.BlankLine();
            builder.Comment(FormattableString.Invariant(
                $"Sekvenca {i + 1}/{program.Sequences.Count} - kontura #{sequence.SourceContourId}"));

            EmitRapidToPierce(builder, sequence);
            EmitPierceSequence(builder, program, sequence);

            foreach (var move in sequence.Moves)
            {
                EmitMove(builder, move);
            }

            EmitCutEnd(builder);
        }

        EmitProgramEnd(builder, program);

        return new PostResult(builder.ToString(), Dialect.FileExtension, warnings);
    }

    /// <summary>Zaglavlje (deklarativno, sa supstitucijom) + modovi (G21/G20, G90).</summary>
    protected virtual void EmitProgramStart(GCodeBuilder b, ToolpathProgram program)
    {
        foreach (var line in Dialect.HeaderLines)
        {
            EmitTemplateLine(b, Substitute(line, program));
        }

        var units = Dialect.Units == UnitsMode.Millimeters ? "G21" : "G20";
        b.Line(units + " G90");
    }

    /// <summary>
    /// Redak zaglavlja/podnožja koji je KOMENTAR mora poštovati EmitComments — inače
    /// bi kontroler koji ne podnosi komentare dobio neispravan G-kod. Redak se
    /// prepoznaje kao komentar ako počinje oznakom komentara iz dijalekta.
    /// </summary>
    private void EmitTemplateLine(GCodeBuilder b, string line)
    {
        var isComment = Dialect.CommentStart.Length > 0
            && line.TrimStart().StartsWith(Dialect.CommentStart, StringComparison.Ordinal);

        if (!isComment)
        {
            b.Line(line);
            return;
        }

        if (!Dialect.EmitComments)
        {
            return; // komentari isključeni — redak se preskače
        }

        b.Line(line);
    }

    protected virtual void EmitRapidToPierce(GCodeBuilder b, CutSequence sequence)
    {
        b.Line(FormattableString.Invariant($"{Dialect.RapidCode} Z{b.Number(U(Profile.SafeZ))}"));
        b.Line(FormattableString.Invariant(
            $"{Dialect.RapidCode} X{b.Number(U(sequence.PiercePoint.X))} Y{b.Number(U(sequence.PiercePoint.Y))}"));
        _lastMotionCode = Dialect.RapidCode;
    }

    /// <summary>Kontrolerska sekvenca probijanja — hook za nadjačavanje (EC300, laser M-kodovi…).</summary>
    protected virtual void EmitPierceSequence(GCodeBuilder b, ToolpathProgram program, CutSequence sequence)
    {
        if (Profile.ProbeMacro.Length > 0)
        {
            b.Line(Profile.ProbeMacro);
        }

        b.Line(FormattableString.Invariant($"{Dialect.RapidCode} Z{b.Number(U(Profile.PierceHeight))}"));
        b.Line(Dialect.TorchOnCode);
        b.Line(FormattableString.Invariant($"G04 P{b.Number(program.Technology.PierceTime)}"));
        b.Line(FormattableString.Invariant($"{Dialect.RapidCode} Z{b.Number(U(Profile.CutHeight))}"));
        _lastMotionCode = Dialect.RapidCode;
    }

    /// <summary>Kraj reza — hook (gašenje luka/vretena/lasera).</summary>
    protected virtual void EmitCutEnd(GCodeBuilder b)
    {
        b.Line(Dialect.TorchOffCode);
    }

    protected virtual void EmitProgramEnd(GCodeBuilder b, ToolpathProgram program)
    {
        b.BlankLine();
        b.Line(FormattableString.Invariant($"{Dialect.RapidCode} Z{b.Number(U(Profile.SafeZ))}"));
        foreach (var line in Dialect.FooterLines)
        {
            EmitTemplateLine(b, Substitute(line, program));
        }
    }

    private void EmitMove(GCodeBuilder b, CutMove move)
    {
        switch (move.Geometry)
        {
            case LineSeg line:
            {
                var code = Dialect.LinearCode;
                var emitCode = !Dialect.ModalMotionCodes || _lastMotionCode != code;
                var words = new List<string>();
                if (emitCode)
                {
                    words.Add(code);
                }

                words.Add("X" + b.Number(U(line.EndPoint.X)));
                words.Add("Y" + b.Number(U(line.EndPoint.Y)));
                AppendFeed(b, words, move.FeedRate);
                b.Line(string.Join(' ', words));
                _lastMotionCode = code;
                break;
            }

            case ArcSeg arc:
            {
                // Lukovi UVIJEK nose motion kod (univerzalno sigurno) + I/J relativno na start.
                var code = arc.IsCcw ? Dialect.ArcCcwCode : Dialect.ArcCwCode;
                var words = new List<string>
                {
                    code,
                    "X" + b.Number(U(arc.EndPoint.X)),
                    "Y" + b.Number(U(arc.EndPoint.Y)),
                    "I" + b.Number(U(arc.Center.X - arc.StartPoint.X)),
                    "J" + b.Number(U(arc.Center.Y - arc.StartPoint.Y)),
                };
                AppendFeed(b, words, move.FeedRate);
                b.Line(string.Join(' ', words));
                _lastMotionCode = code;
                break;
            }
        }
    }

    private void AppendFeed(GCodeBuilder b, List<string> words, double feedMmMin)
    {
        var feed = U(feedMmMin);
        if (!Dialect.EmitFeedOnlyOnChange || _lastFeed != feed)
        {
            words.Add("F" + b.Number(feed));
            _lastFeed = feed;
        }
    }

    /// <summary>Pretvorba jedinica: mm → dijalekt (inch: /25.4).</summary>
    protected double U(double millimeters)
        => Dialect.Units == UnitsMode.Inches ? millimeters / 25.4 : millimeters;

    private string Substitute(string template, ToolpathProgram program)
        => template
            .Replace("{POST}", Dialect.Name, StringComparison.Ordinal)
            .Replace("{MACHINE}", Profile.Name, StringComparison.Ordinal)
            .Replace("{UNITS}", Dialect.Units == UnitsMode.Millimeters ? "mm" : "inch", StringComparison.Ordinal)
            .Replace("{SEQUENCES}", program.Sequences.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{CUTLENGTH}", program.Statistics.CutLength.ToString("0.#", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{TOTALTIME}", program.Statistics.TotalTimeSeconds.ToString("0.#", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
