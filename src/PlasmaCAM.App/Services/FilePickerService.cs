using Windows.Storage.Pickers;
using PlasmaCAM.Core.Abstractions;

namespace PlasmaCAM.App.Services;

/// <summary>
/// WinUI 3 implementacija dijaloga za odabir datoteke. Unpackaged aplikacije
/// MORAJU inicijalizirati picker HWND-om vlasničkog prozora (WinRT.Interop) —
/// handle se dohvaća lijeno jer prozor ne postoji u trenutku DI registracije.
/// </summary>
public sealed class FilePickerService : IFilePickerService
{
    private readonly Func<nint> _windowHandleProvider;

    public FilePickerService(Func<nint> windowHandleProvider)
    {
        ArgumentNullException.ThrowIfNull(windowHandleProvider);
        _windowHandleProvider = windowHandleProvider;
    }

    public async Task<string?> PickOpenFileAsync(IReadOnlyList<string> extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandleProvider());

        foreach (var extension in extensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
