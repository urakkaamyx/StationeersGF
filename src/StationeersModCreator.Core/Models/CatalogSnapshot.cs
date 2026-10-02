namespace StationeersModCreator.Core.Models;
public sealed class CatalogSnapshot
{
    public string Snapshot { get; init; } = "";
    public string UnityVersion { get; init; } = "";
    public List<MachineDefinition> Machines { get; init; } = [];
    public List<RecipeDefinition> Recipes { get; init; } = [];
    public List<PrefabDefinition> Prefabs { get; init; } = [];
}
