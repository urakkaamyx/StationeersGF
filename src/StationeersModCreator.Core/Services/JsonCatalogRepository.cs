using System.Security.Cryptography;
using System.Text.Json;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class JsonCatalogRepository : ICatalogRepository
{
    private readonly Dictionary<string, RecipeDefinition> _recipes;
    private readonly Dictionary<string, PrefabDefinition> _prefabs;
    public CatalogSnapshot Catalog { get; }
    public string Fingerprint { get; }

    public JsonCatalogRepository(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Catalog = JsonSerializer.Deserialize<CatalogSnapshot>(bytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Catalog is empty.");
        Fingerprint = Convert.ToHexString(SHA256.HashData(bytes));
        _recipes = Catalog.Recipes.ToDictionary(x => x.Id);
        _prefabs = Catalog.Prefabs.ToDictionary(x => x.Name);
    }

    public RecipeDefinition FindRecipe(string id) => _recipes.TryGetValue(id, out var value) ? value : throw new InvalidDataException("Unknown recipe: " + id);
    public PrefabDefinition FindPrefab(string name) => _prefabs.TryGetValue(name, out var value) ? value : throw new InvalidDataException("Unknown prefab: " + name);
}
