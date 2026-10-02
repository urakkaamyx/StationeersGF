using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IDraftService
{
    ModProject Project { get; }

    int ChangeCount { get; }

    void Replace(ModProject project);
    void StageRecipe(RecipeDefinition recipe, Dictionary<string, string> values);
    void StageAttributes(PrefabDefinition prefab, Dictionary<string, string> values);
    void RemoveRecipe(string recipeId);
    void RemoveAttributes(string prefabName);
}
