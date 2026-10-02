using System.Text.Json;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class DraftService : IDraftService
{
    private readonly IProjectValidator _validator;
    private readonly IProjectHistory _history;
    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    public void Undo() => Project = _history.Undo(Project);
    public void Redo() => Project = _history.Redo(Project);
    public ModProject Project { get; private set; }
    public int ChangeCount => Project.Recipes.Count + Project.Attributes.Count + Project.Definitions.Count;

    public DraftService(string fingerprint, IProjectValidator validator, IProjectHistory? history = null)
    {
        _validator = validator;
        _history = history ?? new ProjectHistory();
        Project = new ModProject
        {
            CatalogFingerprint = fingerprint
        };
    }

    public void Replace(ModProject project)
    {
        Project = project;
        _history.Clear();
    }

    public void StageRecipe(RecipeDefinition recipe, Dictionary<string, string> values)
    {
        var candidate = CloneProject();
        candidate.Recipes.RemoveAll(x => x.RecipeId == recipe.Id);
        candidate.PendingRecipes.RemoveAll(x => x.RecipeId == recipe.Id);
        var changes = values.Where(x => recipe.Fields.Single(f => f.Path == x.Key).Value != x.Value).ToDictionary();
        if (changes.Count > 0)
            candidate.Recipes.Add(new RecipePatch(recipe.Id, recipe.SourceHash, changes));
        _validator.Validate(candidate, false);
        _history.Capture(Project);
        Project = candidate;
    }

    public void StageAttributes(PrefabDefinition prefab, Dictionary<string, string> values)
    {
        var candidate = CloneProject();
        candidate.Attributes.RemoveAll(x => x.PrefabName == prefab.Name);
        candidate.PendingAttributes.RemoveAll(x => x.PrefabName == prefab.Name);
        var changes = values.Where(x => prefab.Attributes.Single(f => f.Name == x.Key).Value != x.Value).ToDictionary();
        if (changes.Count > 0)
            candidate.Attributes.Add(new AttributePatch(prefab.Name, prefab.SourceHash, changes));
        _validator.Validate(candidate, false);
        _history.Capture(Project);
        Project = candidate;
    }

    public void StageDefinition(NativeDefinitionChange change) => StageDefinitions([change]);
    public void StageDefinitions(IReadOnlyList<NativeDefinitionChange> changes)
    {
        var candidate = CloneProject();
        foreach (var change in changes)
        {
            candidate.Definitions.RemoveAll(x => x.ExportId == change.ExportId);
            candidate.Definitions.Add(change);
        }

        _validator.Validate(candidate, false);
        _history.Capture(Project);
        candidate.PendingDefinition = null;
        Project = candidate;
    }

    public void RemoveDefinition(string exportId)
    {
        _history.Capture(Project);
        Project.Definitions.RemoveAll(x => x.ExportId == exportId);
    }

    private ModProject CloneProject() => JsonSerializer.Deserialize<ModProject>(JsonSerializer.Serialize(Project))!;
    public void RemoveRecipe(string recipeId)
    {
        _history.Capture(Project);
        Project.Recipes.RemoveAll(x => x.RecipeId == recipeId);
        Project.PendingRecipes.RemoveAll(x => x.RecipeId == recipeId);
    }

    public void RemoveAttributes(string prefabName)
    {
        _history.Capture(Project);
        Project.Attributes.RemoveAll(x => x.PrefabName == prefabName);
        Project.PendingAttributes.RemoveAll(x => x.PrefabName == prefabName);
    }
}
