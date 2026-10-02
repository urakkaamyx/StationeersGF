namespace StationeersModCreator.Desktop.Interfaces;
public interface IFileDialogService
{
    Task<string?> OpenProjectAsync();
    Task<string?> SaveFileAsync(string title, string name, string extension);
}
