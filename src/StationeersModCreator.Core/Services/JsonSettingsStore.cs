using System.Text.Json;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private readonly AtomicFileWriter _writer;
    public JsonSettingsStore(string path, AtomicFileWriter writer)
    {
        _path = path;
        _writer = writer;
    }

    public EditorSettings Load()
    {
        if (!File.Exists(_path))
            return new EditorSettings();
        try
        {
            return JsonSerializer.Deserialize<EditorSettings>(File.ReadAllText(_path)) ?? new EditorSettings();
        }
        catch (JsonException)
        {
            return new EditorSettings();
        }
    }

    public void Save(EditorSettings settings) => _writer.Write(_path, temporary => File.WriteAllText(temporary, JsonSerializer.Serialize(settings)));
}
