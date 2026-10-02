using System.Text.Json;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Core.Interfaces;
namespace StationeersModCreator.Core.Services;

public sealed class JsonInventoryCatalog : IInventoryCatalog
{
    private readonly Dictionary<string, InventoryPrefabDefinition> _lookup;
    public IReadOnlyList<InventoryPrefabDefinition> Prefabs { get; }
    public JsonInventoryCatalog(string path) { using var doc = JsonDocument.Parse(File.ReadAllText(path)); Prefabs = JsonSerializer.Deserialize<List<InventoryPrefabDefinition>>(doc.RootElement.GetProperty("prefabs").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!; _lookup = Prefabs.ToDictionary(x => x.Name); }
    public InventoryPrefabDefinition Find(string name) => _lookup.TryGetValue(name, out var p) ? p : throw new InvalidDataException("Inventory metadata is unavailable for " + name);
}
