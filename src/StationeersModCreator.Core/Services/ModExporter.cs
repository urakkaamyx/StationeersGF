using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class ModExporter : IModExporter
{
    private readonly IProjectValidator _validator;
    private readonly IGameDataWriter _data;
    private readonly IArtworkRepository _artwork;
    private readonly AtomicFileWriter _writer;
    public ModExporter(IProjectValidator validator, IGameDataWriter data, IArtworkRepository artwork, AtomicFileWriter writer)
    {
        _validator = validator;
        _data = data;
        _artwork = artwork;
        _writer = writer;
    }

    public void Export(string path, ModProject project)
    {
        _validator.Validate(project, true);
        var files = _data.CreateFiles(project);
        AddMetadata(files, project);
        AddArtwork(files, project);
        var folder = Regex.Replace(project.Name.Trim(), "[^A-Za-z0-9_-]+", "_");
        _writer.Write(path, temp => WriteArchive(temp, folder, files));
    }

    private static void AddMetadata(Dictionary<string, byte[]> files, ModProject project)
    {
        var root = new XElement("ModMetadata", new XElement("Name", project.Name), new XElement("Author", project.Author), new XElement("Version", project.Version), new XElement("Description", project.Description));
        files.Add("About/About.xml", Encoding.UTF8.GetBytes(root.ToString()));
    }

    private void AddArtwork(Dictionary<string, byte[]> files, ModProject project)
    {
        var bytes = _artwork.ReadImage(project.PreviewAsset);
        if (bytes is null)
            throw new InvalidDataException("The selected preview image is unavailable.");
        files.Add("About/Preview.png", bytes);
        if (bytes.Length < 1_000_000)
            files.Add("About/thumb.png", bytes);
    }

    private static void WriteArchive(string path, string folder, Dictionary<string, byte[]> files)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var file in files)
        {
            var entry = zip.CreateEntry(folder + "/" + file.Key, CompressionLevel.Optimal);
            using var stream = entry.Open();
            stream.Write(file.Value);
        }
    }
}
