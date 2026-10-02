using Avalonia.Media.Imaging;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.Services;
public sealed class GameBitmapProvider : IBitmapProvider
{
    private readonly IArtworkRepository _artwork;
    private readonly Dictionary<string, Bitmap?> _cache = [];
    public GameBitmapProvider(IArtworkRepository artwork) => _artwork = artwork;
    public Bitmap? Get(string name)
    {
        if (_cache.TryGetValue(name, out var image))
            return image;
        var bytes = _artwork.ReadImage(name) ?? _artwork.ReadImage("planets/StatMars.png");
        var bitmap = bytes is null ? null : new Bitmap(new MemoryStream(bytes));
        _cache[name] = bitmap;
        return bitmap;
    }
}
