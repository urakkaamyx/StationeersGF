using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.Services;
public sealed class SmokeTestFileDialogService : IFileDialogService
{
    private readonly string _directory;
    public SmokeTestFileDialogService(string directory) => _directory = directory;
    public Task<string?> OpenProjectAsync() => Task.FromResult<string?>(Path.Combine(_directory, "project.modforge.json"));
    public Task<string?> SaveFileAsync(string title, string name, string extension) => Task.FromResult<string?>(Path.Combine(_directory, extension == "zip" ? "mod.zip" : "project.modforge.json"));
}
