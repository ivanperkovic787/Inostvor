using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Sdk.Post;

namespace Inostvor.ViewModels;

/// <summary>Editor biblioteke profila strojeva (naziv, proizvođač, radno područje, Z visine, probe, THC, proces, postprocesor, tehnologija).</summary>
public sealed partial class MachineProfileManagerViewModel : ObservableObject
{
    private readonly IMachineProfileRepository _repository;

    public MachineProfileManagerViewModel(IMachineProfileRepository repository, IPostProcessorCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(catalog);
        _repository = repository;
        PostProcessorIds = catalog.Plugins.Select(p => p.Id).ToList();
        Refresh();
    }

    public IReadOnlyList<string> PostProcessorIds { get; }

    public IReadOnlyList<CutProcess> Processes { get; } = Enum.GetValues<CutProcess>();

    [ObservableProperty]
    private IReadOnlyList<MachineProfile> _profiles = [];

    [ObservableProperty]
    private MachineProfile? _selected;

    public void Refresh() => Profiles = _repository.GetAll();

    [RelayCommand]
    private void New()
    {
        Selected = new MachineProfile
        {
            Name = "Novi stroj",
            PostProcessorId = PostProcessorIds.Count > 0 ? PostProcessorIds[0] : "inostvor.post.mach3",
        };
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected is null)
        {
            return;
        }

        _repository.Save(Selected);
        Refresh();
    }

    [RelayCommand]
    private void Delete()
    {
        if (Selected is null)
        {
            return;
        }

        _repository.Delete(Selected.Name);
        Selected = null;
        Refresh();
    }
}
