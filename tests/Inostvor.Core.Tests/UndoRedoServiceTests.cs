using Inostvor.Core.Services;
using Shouldly;
using Xunit;

namespace Inostvor.Core.Tests;

public sealed class UndoRedoServiceTests
{
    private readonly List<string> _journal = [];

    [Fact]
    public void Do_IzvrsavaNaredbu_IPuniUndoPovijest()
    {
        var sut = new UndoRedoService();

        sut.Do(new TestCommand("A", _journal));

        _journal.ShouldBe(["A:do"]);
        sut.CanUndo.ShouldBeTrue();
        sut.CanRedo.ShouldBeFalse();
        sut.UndoDescription.ShouldBe("A");
    }

    [Fact]
    public void Do_NullNaredba_BacaArgumentNull()
    {
        var sut = new UndoRedoService();

        Should.Throw<ArgumentNullException>(() => sut.Do(null!));
    }

    [Fact]
    public void Do_KadExecuteBaciIznimku_PovijestOstajeNetaknuta()
    {
        var sut = new UndoRedoService();

        Should.Throw<InvalidOperationException>(() =>
            sut.Do(new TestCommand("X", _journal, () => throw new InvalidOperationException("boom"))));

        sut.CanUndo.ShouldBeFalse();
        sut.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void Undo_PonistavaZadnjuNaredbu_IPrebacujeJeURedo()
    {
        var sut = new UndoRedoService();
        sut.Do(new TestCommand("A", _journal));
        sut.Do(new TestCommand("B", _journal));

        sut.Undo();

        _journal.ShouldBe(["A:do", "B:do", "B:undo"]);
        sut.CanRedo.ShouldBeTrue();
        sut.RedoDescription.ShouldBe("B");
        sut.UndoDescription.ShouldBe("A");
    }

    [Fact]
    public void Redo_PonovnoIzvrsavaPonistenuNaredbu()
    {
        var sut = new UndoRedoService();
        sut.Do(new TestCommand("A", _journal));
        sut.Undo();

        sut.Redo();

        _journal.ShouldBe(["A:do", "A:undo", "A:do"]);
        sut.CanUndo.ShouldBeTrue();
        sut.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void Do_NakonUndoa_BriseRedoPovijest()
    {
        var sut = new UndoRedoService();
        sut.Do(new TestCommand("A", _journal));
        sut.Undo();

        sut.Do(new TestCommand("B", _journal));

        sut.CanRedo.ShouldBeFalse();
        sut.RedoDescription.ShouldBeNull();
    }

    [Fact]
    public void Undo_PraznaPovijest_BacaInvalidOperation()
    {
        var sut = new UndoRedoService();

        Should.Throw<InvalidOperationException>(sut.Undo);
    }

    [Fact]
    public void Redo_PraznaPovijest_BacaInvalidOperation()
    {
        var sut = new UndoRedoService();

        Should.Throw<InvalidOperationException>(sut.Redo);
    }

    [Fact]
    public void Do_IznadKapaciteta_IzbacujeNajstarijuNaredbu()
    {
        var sut = new UndoRedoService(capacity: 2);
        sut.Do(new TestCommand("A", _journal));
        sut.Do(new TestCommand("B", _journal));
        sut.Do(new TestCommand("C", _journal));

        sut.Undo(); // C
        sut.Undo(); // B

        sut.CanUndo.ShouldBeFalse(); // A je izbačen iz povijesti
        _journal.ShouldBe(["A:do", "B:do", "C:do", "C:undo", "B:undo"]);
    }

    [Fact]
    public void Konstruktor_KapacitetManjiOdJedan_Baca()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new UndoRedoService(capacity: 0));
    }

    [Fact]
    public void StateChanged_OkidaSeNaSvakuPromjenu()
    {
        var sut = new UndoRedoService();
        var raised = 0;
        sut.StateChanged += (_, _) => raised++;

        sut.Do(new TestCommand("A", _journal));
        sut.Undo();
        sut.Redo();
        sut.Clear();

        raised.ShouldBe(4);
    }

    [Fact]
    public void Clear_PrazniObjePovijesti()
    {
        var sut = new UndoRedoService();
        sut.Do(new TestCommand("A", _journal));
        sut.Do(new TestCommand("B", _journal));
        sut.Undo();

        sut.Clear();

        sut.CanUndo.ShouldBeFalse();
        sut.CanRedo.ShouldBeFalse();
        sut.UndoDescription.ShouldBeNull();
        sut.RedoDescription.ShouldBeNull();
    }
}
