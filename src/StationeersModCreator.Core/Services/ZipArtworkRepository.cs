using System.IO.Compression;
using StationeersModCreator.Core.Interfaces;

namespace StationeersModCreator.Core.Services;
public sealed class ZipArtworkRepository : IArtworkRepository
{
    private readonly string _path;
    public IReadOnlyList<string> Names { get; }

    public ZipArtworkRepository(string path)
    {
        _path = path;
        using var zip = ZipFile.OpenRead(path);
        Names = zip.Entries.Where(x => x.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).Select(x => x.FullName).Order().ToArray();
    }

    public byte[]? ReadImage(string name)
    {
        using var zip = ZipFile.OpenRead(_path);
        var entry = zip.GetEntry(name);
        if (entry is null)
            return null;
        using var stream = entry.Open();
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }
}
