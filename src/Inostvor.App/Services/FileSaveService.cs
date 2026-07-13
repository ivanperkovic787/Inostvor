using Windows.Storage;
using Windows.Storage.Pickers;
using Inostvor.Core.Abstractions;

namespace Inostvor.App.Services;

/// <summary>WinUI 3 FileSavePicker (unpackaged: HWND inicijalizacija kao kod open pickera).</summary>
public sealed class FileSaveService : IFileSaveService
{
    private readonly Func<nint> _windowHandleProvider;

    public FileSaveService(Func<nint> windowHandleProvider)
    {
        ArgumentNullException.ThrowIfNull(windowHandleProvider);
        _windowHandleProvider = windowHandleProvider;
    }

    public async Task<string?> SaveTextAsync(string suggestedFileName, string extension, string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var file = await PickAsync(suggestedFileName, extension);
        if (file is null)
        {
            return null;
        }

        await FileIO.WriteTextAsync(file, content);
        return file.Path;
    }

    public async Task<string?> PickSavePathAsync(string suggestedFileName, string extension)
    {
        var file = await PickAsync(suggestedFileName, extension);
        return file?.Path;
    }

    private async Task<StorageFile?> PickAsync(string suggestedFileName, string extension)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
        };
        picker.FileTypeChoices.Add(
            extension == ".ino" ? "Inostvor projekt" : "G-kod",
            new List<string> { extension });
        WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandleProvider());

        return await picker.PickSaveFileAsync();
    }
}
