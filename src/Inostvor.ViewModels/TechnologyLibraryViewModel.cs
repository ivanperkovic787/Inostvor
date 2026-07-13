using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Library;

namespace Inostvor.ViewModels;

/// <summary>Biblioteka tehnologija (Steel 2 mm, …) — primjenjiva na više projekata.</summary>
public sealed partial class TechnologyLibraryViewModel : ObservableObject
{
    private readonly ITechnologyRepository _repository;

    public TechnologyLibraryViewModel(ITechnologyRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        Refresh();
    }

    [ObservableProperty]
    private IReadOnlyList<TechnologyEntry> _entries = [];

    [ObservableProperty]
    private TechnologyEntry? _selected;

    public void Refresh() => Entries = _repository.GetAll();

    [RelayCommand]
    private void New() => Selected = new TechnologyEntry { Name = "Nova tehnologija" };

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

        _repository.Delete(Selected.Id);
        Selected = null;
        Refresh();
    }
}
