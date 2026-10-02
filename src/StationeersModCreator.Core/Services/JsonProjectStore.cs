using System.Text.Json;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class JsonProjectStore : IProjectStore
{
    private readonly AtomicFileWriter _writer;
    public JsonProjectStore(AtomicFileWriter writer) => _writer = writer;
    public ModProject Load(string path) => JsonSerializer.Deserialize<ModProject>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid mod project.");
    public void Save(string path, ModProject project) => _writer.Write(path, temporary => File.WriteAllText(temporary, JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true })));
}
