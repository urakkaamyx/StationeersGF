using Avalonia.Controls;
using StationeersModCreator.Core.Services;
using StationeersModCreator.Desktop.ViewModels;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.Services;
public static class CompositionRoot
{
    public static MainViewModel Create(Window window, IFileDialogService? dialogs = null, string? settingsFile = null)
    {
        var content = Path.Combine(AppContext.BaseDirectory, "Content");
        var catalog = new JsonCatalogRepository(Path.Combine(content, "catalog.json"));
        var artwork = new ZipArtworkRepository(Path.Combine(content, "artwork.zip"));
        var files = new AtomicFileWriter();
        var validator = new ProjectValidator(catalog, new NumericValueValidator());
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StationeersModForge", "settings.json");
        return new MainViewModel(catalog, artwork, new GameBitmapProvider(artwork), new DraftService(catalog.Fingerprint, validator), new JsonProjectStore(files), new JsonSettingsStore(settingsFile ?? settingsPath, files), new ThemeService(), new ModExporter(validator, new NativeGameDataWriter(catalog), artwork, files), validator, dialogs ?? new AvaloniaFileDialogService(() => window));
    }
}
