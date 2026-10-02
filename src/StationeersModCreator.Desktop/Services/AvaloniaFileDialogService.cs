using Avalonia.Controls;
using Avalonia.Platform.Storage;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.Services;
public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Func<TopLevel> _window;
    public AvaloniaFileDialogService(Func<TopLevel> window) => _window = window;
    public async Task<string?> OpenProjectAsync()
    {
        var files = await _window().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Open Mod Forge project", AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("Mod Forge project") { Patterns = ["*.modforge.json"] }] });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> SaveFileAsync(string title, string name, string extension)
    {
        var file = await _window().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = title, SuggestedFileName = name, DefaultExtension = extension, ShowOverwritePrompt = true });
        return file?.TryGetLocalPath();
    }
}
