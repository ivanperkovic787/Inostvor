using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PlasmaCAM.Core.Abstractions;
using PlasmaCAM.Core.Model.Import;
using PlasmaCAM.Core.Model.Validation;
using Shouldly;
using Xunit;

namespace PlasmaCAM.ViewModels.Tests;

public sealed class MainViewModelTests
{
    private readonly IUndoService _undo = Substitute.For<IUndoService>();
    private readonly IDxfImporter _importer = Substitute.For<IDxfImporter>();
    private readonly IFilePickerService _picker = Substitute.For<IFilePickerService>();
    private readonly IGeometryPipeline _pipeline = Substitute.For<IGeometryPipeline>();

    private MainViewModel Create()
    {
        _pipeline.Process(Arg.Any<IReadOnlyList<ImportedEntity>>(), Arg.Any<Core.Model.Geometry.ContourBuildSettings>())
            .Returns(new GeometryPipelineResult([], [], new ValidationReport([])));
        return new MainViewModel(_undo, _importer, _picker, _pipeline, NullLogger<MainViewModel>.Instance);
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
        _pipeline.Received(1).Process(ok.Entities, Arg.Any<Core.Model.Geometry.ContourBuildSettings>());
        vm.StatusText.ShouldContain("dobar.dxf");
        vm.StatusText.ShouldContain("kontura");
    }
}
