using Inostvor.Cam.Fitting;
using Inostvor.Cam.Generation;
using Inostvor.Cam.Leads;
using Inostvor.Cam.Offset;
using Inostvor.Cam.Simulation;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;
using static Inostvor.Cam.Tests.CamTestHelpers;

namespace Inostvor.Cam.Tests;

public sealed class SimulationEngineTests
{
    /// <summary>Linija (30,40)→(130,40): rapid 50 mm, pierce 0.5 s, rez 100 mm @ 6000 = 1 s. Ukupno 2 s.</summary>
    private static ToolpathProgram LineProgram()
    {
        var contours = Contours(("0", [L(30, 40, 130, 40)]));
        var tech = TechnologySettings.Default with { FeedRate = 6000, RapidRate = 6000, PierceTime = 0.5 };
        var generator = new ToolpathGenerator(
            new KerfOffsetService(), new ArcFitter(),
            new LeadGeneratorService([new LineLeadStrategy(), new ArcLeadStrategy()]),
            new OvercutService(),
            new CutOrderStrategyProvider([new DefaultCutOrderStrategy()]));
        return generator.Generate(contours, tech);
    }

    [Fact]
    public void VremenskaCrta_FazeUTocnimTrenucima()
    {
        var engine = new SimulationEngine(LineProgram());

        engine.TotalDuration.ShouldBe(2.0, 1e-9);

        var rapid = engine.StateAt(0.25); // pola rapida (50 mm @ 6000 = 0.5 s)
        rapid.Phase.ShouldBe(MachinePhase.Rapid);
        rapid.TorchOn.ShouldBeFalse();
        rapid.Position.AlmostEquals(new Point2(15, 20), 1e-9).ShouldBeTrue(); // lerp (0,0)→(30,40)

        var pierce = engine.StateAt(0.75);
        pierce.Phase.ShouldBe(MachinePhase.Piercing);
        pierce.TorchOn.ShouldBeTrue();
        pierce.Position.AlmostEquals(new Point2(30, 40), 1e-9).ShouldBeTrue();

        var cutting = engine.StateAt(1.5); // pola reza
        cutting.Phase.ShouldBe(MachinePhase.Cutting);
        cutting.MoveIndex.ShouldBe(0);
        cutting.MoveProgress.ShouldBe(0.5, 1e-9);
        cutting.Position.AlmostEquals(new Point2(80, 40), 1e-9).ShouldBeTrue();

        var done = engine.StateAt(99);
        done.Phase.ShouldBe(MachinePhase.Completed);
        done.TorchOn.ShouldBeFalse();
        done.Position.AlmostEquals(new Point2(130, 40), 1e-9).ShouldBeTrue();
    }

    [Fact]
    public void RazdvojenaVremena_TocnaUSvakomTrenutku()
    {
        var engine = new SimulationEngine(LineProgram());

        var mid = engine.StateAt(1.5);
        mid.Elapsed.Rapid.ShouldBe(0.5, 1e-9);
        mid.Elapsed.Pierce.ShouldBe(0.5, 1e-9);
        mid.Elapsed.Cut.ShouldBe(0.5, 1e-9);
        mid.Elapsed.Total.ShouldBe(1.5, 1e-9);

        engine.FinalBreakdown.Rapid.ShouldBe(0.5, 1e-9);
        engine.FinalBreakdown.Pierce.ShouldBe(0.5, 1e-9);
        engine.FinalBreakdown.Cut.ShouldBe(1.0, 1e-9);
    }

    [Fact]
    public void Breakdown_PoklapaSeSaStatistikomPrograma()
    {
        // Križna provjera dva neovisna izračuna (Statistics u M5, timeline u M6).
        var program = LineProgram();
        var engine = new SimulationEngine(program);

        engine.FinalBreakdown.Cut.ShouldBe(program.Statistics.CutTimeSeconds, 1e-9);
        engine.FinalBreakdown.Rapid.ShouldBe(program.Statistics.RapidTimeSeconds, 1e-9);
        engine.FinalBreakdown.Pierce.ShouldBe(program.Statistics.PierceTimeSeconds, 1e-9);
        engine.TotalDuration.ShouldBe(program.Statistics.TotalTimeSeconds, 1e-9);
    }

    [Fact]
    public void CistaFunkcija_IstoVrijeme_IdenticnoStanje()
    {
        var engine = new SimulationEngine(LineProgram());
        engine.StateAt(1.234).ShouldBe(engine.StateAt(1.234)); // record equality
    }

    [Fact]
    public void Sesija_AdvanceRespektiraBrzinu()
    {
        var session = new SimulationSession(LineProgram()) { SpeedMultiplier = 2.0 };
        session.Play();

        session.Advance(0.5); // realnih 0.5 s × 2 = 1.0 s simulacije

        session.CurrentTime.ShouldBe(1.0, 1e-9);
        session.Current.Phase.ShouldBe(MachinePhase.Cutting);
    }

    [Fact]
    public void Sesija_ZavrsetkomPrestajeIgrati()
    {
        var session = new SimulationSession(LineProgram());
        session.Play();
        session.Advance(10);

        session.IsCompleted.ShouldBeTrue();
        session.IsPlaying.ShouldBeFalse();
        session.CurrentTime.ShouldBe(session.TotalDuration, 1e-9);
    }

    [Fact]
    public void Checkpoint_SpremiINastavi_IdenticnoStanje_BezPonovnogIzracuna()
    {
        var program = LineProgram(); // program se NE regenerira za nastavak
        var a = new SimulationSession(program) { SpeedMultiplier = 2.0 };
        a.Seek(1.37);
        var checkpoint = a.Save();

        var b = new SimulationSession(program);
        b.Restore(checkpoint);

        b.CurrentTime.ShouldBe(1.37, 1e-9);
        b.SpeedMultiplier.ShouldBe(2.0);
        b.Current.ShouldBe(a.Current);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1859:Use concrete types when possible",
        Justification = "Namjerno kroz sučelje: test dokazuje da UI može ovisiti o IMachineStateSource (put prema Live Monitoru).")]
    public void Sesija_JeIzvorStanjaStroja()
    {
        // Digital twin sučelje: UI ovisi o IMachineStateSource, ne o sesiji.
        IMachineStateSource source = new SimulationSession(LineProgram());
        var notified = 0;
        source.StateChanged += (_, _) => notified++;

        ((SimulationSession)source).Seek(1.0);

        source.Current.Phase.ShouldBe(MachinePhase.Cutting);
        notified.ShouldBe(1);
    }

    [Fact]
    public void PrazanProgram_ZavrsenoStanje_BezPada()
    {
        var engine = new SimulationEngine(ToolpathProgram.Empty);
        engine.TotalDuration.ShouldBe(0.0);
        engine.StateAt(5).Phase.ShouldBe(MachinePhase.Completed);
    }
}
