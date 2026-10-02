using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IDraftService
{
    ModProject Project { get; }

    int ChangeCount { get; }

    bool CanUndo { get; }

    bool CanRedo { get; }

    void Undo();
    void Redo();
    void Replace(ModProject project);
    void StageRecipe(RecipeDefinition recipe, Dictionary<string, string> values);
    void StageAttributes(PrefabDefinition prefab, Dictionary<string, string> values);
    void StageDefinitions(IReadOnlyList<NativeDefinitionChange> changes);
    void StageDefinition(NativeDefinitionChange change);
    void RemoveDefinition(string exportId);
    void RemoveRecipe(string recipeId);
    void RemoveAttributes(string prefabName);
}
