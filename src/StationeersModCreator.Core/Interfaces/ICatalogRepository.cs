using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface ICatalogRepository
{
    CatalogSnapshot Catalog { get; }

    string Fingerprint { get; }

    RecipeDefinition FindRecipe(string id);
    PrefabDefinition FindPrefab(string name);
}
