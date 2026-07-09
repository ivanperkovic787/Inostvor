using PlasmaCAM.Core.Abstractions;
using PlasmaCAM.Core.Services;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Core.Tests;

public sealed class CompositeCommandTests
{
    private readonly List<string> _journal = [];

    [Fact]
    public void Execute_IzvrsavaNaredbeRedom()
    {
        var sut = new CompositeCommand("Grupa", [
            new TestCommand("A", _journal),
            new TestCommand("B", _journal),
        ]);

        sut.Execute();

        _journal.ShouldBe(["A:do", "B:do"]);
    }

    [Fact]
    public void Undo_PonistavaObrnutimRedoslijedom()
    {
        var sut = new CompositeCommand("Grupa", [
            new TestCommand("A", _journal),
            new TestCommand("B", _journal),
        ]);
        sut.Execute();

        sut.Undo();

        _journal.ShouldBe(["A:do", "B:do", "B:undo", "A:undo"]);
    }

    [Fact]
    public void Execute_IznimkaUsredIzvrsavanja_RollbackVecIzvrsenih_IPropagiraIznimku()
    {
        var sut = new CompositeCommand("Grupa", [
            new TestCommand("A", _journal),
            new TestCommand("B", _journal),
            new TestCommand("C", _journal, () => throw new InvalidOperationException("boom")),
            new TestCommand("D", _journal),
        ]);

        Should.Throw<InvalidOperationException>(sut.Execute);

        // A i B su izvršeni pa poništeni obrnutim redoslijedom; C je pukao prije zapisa; D nije ni pokrenut.
        _journal.ShouldBe(["A:do", "B:do", "B:undo", "A:undo"]);
    }

    [Fact]
    public void Konstruktor_PraznaLista_Baca()
    {
        Should.Throw<ArgumentException>(() => new CompositeCommand("Grupa", Array.Empty<IUndoableCommand>()));
    }

    [Fact]
    public void Konstruktor_PrazanOpis_Baca()
    {
        Should.Throw<ArgumentException>(() => new CompositeCommand("  ", [new TestCommand("A", _journal)]));
    }

    [Fact]
    public void Description_IzlozenaIzKonstruktora()
    {
        var sut = new CompositeCommand("Generate toolpaths", [new TestCommand("A", _journal)]);

        sut.Description.ShouldBe("Generate toolpaths");
    }
}
