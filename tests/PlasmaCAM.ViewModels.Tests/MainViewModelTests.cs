using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PlasmaCAM.Core.Abstractions;
using PlasmaCAM.Core.Model.Import;
using Shouldly;
using Xunit;

namespace PlasmaCAM.ViewModels.Tests;

public sealed class MainViewModelTests
{
    private readonly IUndoService _undo = Substitute.For<IUndoService>();
    private readonly IDxfImporter _importer = Substitute.For<IDxfImporter>();
    private readonly IFilePickerService _picker = Substitute.For<IFilePickerService>();

    private MainViewModel Create() => new(_undo, _importer, _picker, NullLogger<MainViewModel>.Instance);

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
        vm.StatusText.ShouldContain("dobar.dxf");
        vm.StatusText.ShouldContain("1 entiteta");
    }
}
