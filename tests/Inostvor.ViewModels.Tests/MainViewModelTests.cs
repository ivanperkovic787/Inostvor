using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Import;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Core.Model.Validation;
using Inostvor.Sdk.Post;
using Shouldly;
using Xunit;

namespace Inostvor.ViewModels.Tests;

public sealed class MainViewModelTests
{
    private readonly IUndoService _undo = Substitute.For<IUndoService>();
    private readonly IDxfImporter _importer = Substitute.For<IDxfImporter>();
    private readonly IFilePickerService _picker = Substitute.For<IFilePickerService>();
    private readonly IGeometryPipeline _pipeline = Substitute.For<IGeometryPipeline>();
    private readonly IToolpathGenerator _toolpath = Substitute.For<IToolpathGenerator>();
    private readonly IPostProcessorCatalog _catalog = Substitute.For<IPostProcessorCatalog>();
    private readonly IFileSaveService _fileSave = Substitute.For<IFileSaveService>();
    private readonly IProjectStore _store = Substitute.For<IProjectStore>();
    private readonly IAutoSaveService _autoSave = Substitute.For<IAutoSaveService>();
    private readonly IFileHashService _fileHash = Substitute.For<IFileHashService>();

    private ProjectViewModel _project = null!;

    private MainViewModel Create()
    {
        _pipeline.Process(Arg.Any<IReadOnlyList<ImportedEntity>>(), Arg.Any<Core.Model.Geometry.ContourBuildSettings>())
            .Returns(new GeometryPipelineResult([], [], new ValidationReport([])));
        _toolpath.Generate(Arg.Any<IReadOnlyList<Core.Model.Geometry.Contour>>(), Arg.Any<TechnologySettings>())
            .Returns(ToolpathProgram.Empty);
        _fileHash.HashFile(Arg.Any<string>()).Returns("deadbeef");
        _project = new ProjectViewModel(_store, _autoSave, _fileHash);
        return new MainViewModel(_undo, _importer, _picker, _pipeline, _toolpath, _catalog, _fileSave, _project, NullLogger<MainViewModel>.Instance);
    }

    [Fact]
    public async Task OpenDxf_KorisnikOdustao_ImporterSeNePoziva()
    {
        _picker.PickOpenFileAsync(Arg.Any<IReadOnlyList<string>>()).Returns(Task.FromResult<string?>(null));
        var vm = Create();

        await vm.OpenDxfCommand.ExecuteAsync(null);

        _importer.DidNotReceive().Import(Arg.Any<string>());
        vm.LastImport.ShouldBeNull();
        vm.StatusText.ShouldBe("Spremno");
    }

    [Fact]
    public async Task OpenDxf_ImportNeuspjesan_LastImportOstajeNull()
    {
        _picker.PickOpenFileAsync(Arg.Any<IReadOnlyList<string>>()).Returns(Task.FromResult<string?>("C:\\test\\bad.dxf"));
        _importer.Name.Returns("test");
        _importer.Import("C:\\test\\bad.dxf").Returns(ImportResult.Fail("pokvarena datoteka"));
        var vm = Create();

        await vm.OpenDxfCommand.ExecuteAsync(null);

        vm.LastImport.ShouldBeNull();
        vm.LastPipeline.ShouldBeNull();
        _pipeline.DidNotReceive().Process(Arg.Any<IReadOnlyList<ImportedEntity>>(), Arg.Any<Core.Model.Geometry.ContourBuildSettings>());
        vm.StatusText.ShouldContain("neuspješan");

        // REGRESIJA: neuspješan import NE SMIJE ostaviti fantomski izvor u projektu
        // (ranije se AddDxf zvao PRIJE importa, pa se pokvarena datoteka spremala u .ino).
        _project.DxfSources.ShouldBeEmpty();
        _project.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public async Task OpenDxf_Uspjeh_LastImportPostavljen_StatusSazetak()
    {
        var ok = ImportResult.Ok(
            entities:
            [
                new ImportedEntity(
                    [new Kernel.Primitives.LineSeg(new Kernel.Primitives.Point2(0, 0), new Kernel.Primitives.Point2(10, 0))],
                    "0", "LINE", "A1"),
            ],
            warnings: [new ImportWarning(ImportWarningCodes.UnitlessAssumedMm, "test upozorenje")],
            sourceUnits: "Millimeters",
            unitScaleToMm: 1.0,
            layers: ["0"]);

        _picker.PickOpenFileAsync(Arg.Any<IReadOnlyList<string>>()).Returns(Task.FromResult<string?>("C:\\test\\dobar.dxf"));
        _importer.Import("C:\\test\\dobar.dxf").Returns(ok);
        var vm = Create();

        await vm.OpenDxfCommand.ExecuteAsync(null);

        vm.LastImport.ShouldBeSameAs(ok);
        vm.LastPipeline.ShouldNotBeNull();
        vm.LastToolpath.ShouldNotBeNull();
        _pipeline.Received(1).Process(ok.Entities, Arg.Any<Core.Model.Geometry.ContourBuildSettings>());
        vm.StatusText.ShouldContain("dobar.dxf");
        vm.StatusText.ShouldContain("kontura");

        // Uspješan import dodaje izvor u projekt, s hashom kroz IFileHashService seam.
        _project.DxfSources.ShouldHaveSingleItem();
        _project.DxfSources[0].FileName.ShouldBe("dobar.dxf");
        _project.DxfSources[0].Sha256.ShouldBe("deadbeef");
        _project.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void ReportRecoveryFailure_ProgutaIznimku_PostaviStatus()
    {
        // REGRESIJA: aplikacija se rušila na startu jer je oporavak autosavea (async
        // void event handler) puštao iznimku iz OpenProjectAsync. ReportRecoveryFailure
        // MORA proći bez bacanja — inače proces tvrdo pada prije prikaza prozora.
        var vm = Create();

        Should.NotThrow(() => vm.ReportRecoveryFailure(new InvalidOperationException("oštećen autosave")));

        vm.StatusText.ShouldContain("Oporavak nije uspio");
    }

    [Fact]
    public async Task OpenProjectAsync_StoreBaci_IznimkaSePropagira()
    {
        // Dokumentira zašto pozivatelj (recovery) MORA imati try/catch: OpenProjectAsync
        // namjerno propagira grešku učitavanja umjesto da je tiho proguta.
        _store.LoadAsync(Arg.Any<string>())
            .Returns(Task.FromException<Core.Model.Project.LoadedProject>(new IOException("nečitljiv .ino")));
        var vm = Create();

        await Should.ThrowAsync<IOException>(() => vm.OpenProjectAsync("C:\\test\\autosave.ino"));
    }
}
