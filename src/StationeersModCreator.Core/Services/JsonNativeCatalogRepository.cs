using System.Text.Json;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class JsonNativeCatalogRepository : INativeCatalogRepository
{
    public IReadOnlyList<NativeDefinition> Definitions { get; }

    public JsonNativeCatalogRepository(string path)
    {
        Definitions = JsonSerializer.Deserialize<List<NativeDefinition>>(File.ReadAllBytes(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Native catalog is empty.");
    }

    public NativeDefinition Find(string key) => Definitions.SingleOrDefault(x => x.Key == key) ?? throw new InvalidDataException("Unknown definition: " + key);
    public NativeDefinition Resolve(string tag, string id)
    {
        var matches = Definitions.Where(x => x.Tag == tag && x.Id == id).ToList();
        return matches.Count == 1 ? matches[0] : throw new InvalidDataException("Definition reference is missing or ambiguous: " + tag + " " + id);
    }
}
