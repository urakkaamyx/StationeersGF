using System.Text.Json;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class DraftService : IDraftService
{
    private readonly IProjectValidator _validator;
    public ModProject Project { get; private set; }
    public int ChangeCount => Project.Recipes.Count + Project.Attributes.Count;

    public DraftService(string fingerprint, IProjectValidator validator)
    {
        _validator = validator;
        Project = new ModProject
        {
            CatalogFingerprint = fingerprint
        };
    }

    public void Replace(ModProject project) => Project = project;
    public void StageRecipe(RecipeDefinition recipe, Dictionary<string, string> values)
    {
        var candidate = CloneProject();
        candidate.Recipes.RemoveAll(x => x.RecipeId == recipe.Id);
        var changes = values.Where(x => recipe.Fields.Single(f => f.Path == x.Key).Value != x.Value).ToDictionary();
        if (changes.Count > 0)
            candidate.Recipes.Add(new RecipePatch(recipe.Id, recipe.SourceHash, changes));
        _validator.Validate(candidate, false);
        Project = candidate;
    }

    public void StageAttributes(PrefabDefinition prefab, Dictionary<string, string> values)
    {
        var candidate = CloneProject();
        candidate.Attributes.RemoveAll(x => x.PrefabName == prefab.Name);
        var changes = values.Where(x => prefab.Attributes.Single(f => f.Name == x.Key).Value != x.Value).ToDictionary();
        if (changes.Count > 0)
            candidate.Attributes.Add(new AttributePatch(prefab.Name, prefab.SourceHash, changes));
        _validator.Validate(candidate, false);
        Project = candidate;
    }

    private ModProject CloneProject() => JsonSerializer.Deserialize<ModProject>(JsonSerializer.Serialize(Project))!;
    public void RemoveRecipe(string recipeId) => Project.Recipes.RemoveAll(x => x.RecipeId == recipeId);
    public void RemoveAttributes(string prefabName) => Project.Attributes.RemoveAll(x => x.PrefabName == prefabName);
}
