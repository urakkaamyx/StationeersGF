using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class ProjectValidator : IProjectValidator
{
    private readonly ICatalogRepository _catalog;
    private readonly NumericValueValidator _numbers;
    public ProjectValidator(ICatalogRepository catalog, NumericValueValidator numbers)
    {
        _catalog = catalog;
        _numbers = numbers;
    }

    public void Validate(ModProject project, bool requireChanges)
    {
        ValidateMetadata(project);
        ValidateUniqueChanges(project);
        foreach (var patch in project.Recipes)
            ValidateRecipe(patch);
        foreach (var patch in project.Attributes)
            ValidateAttributes(patch);
        if (requireChanges && project.Recipes.Count + project.Attributes.Count == 0)
            throw new InvalidDataException("Stage at least one change before exporting.");
    }

    private void ValidateMetadata(ModProject p)
    {
        if (p.FormatVersion != 1)
            throw new InvalidDataException("Unsupported project version.");
        if (p.CatalogFingerprint != _catalog.Fingerprint)
            throw new InvalidDataException("This project was created from a different game-data snapshot.");
        if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 80)
            throw new InvalidDataException("Mod name must contain 1–80 characters.");
        if (string.IsNullOrWhiteSpace(p.Author))
            throw new InvalidDataException("Enter the mod author.");
        if (!Version.TryParse(p.Version, out _))
            throw new InvalidDataException("Use a numeric mod version such as 1.0.0.");
    }

    private static void ValidateUniqueChanges(ModProject p)
    {
        if (p.Recipes.Select(x => x.RecipeId).Distinct().Count() != p.Recipes.Count || p.Attributes.Select(x => x.PrefabName).Distinct().Count() != p.Attributes.Count)
            throw new InvalidDataException("Project contains duplicate overrides.");
    }

    private void ValidateRecipe(RecipePatch patch)
    {
        var recipe = _catalog.FindRecipe(patch.RecipeId);
        if (patch.SourceHash != recipe.SourceHash)
            throw new InvalidDataException("Recipe source fingerprint mismatch.");
        foreach (var change in patch.Values)
        {
            if (!recipe.Fields.Any(x => x.Path == change.Key))
                throw new InvalidDataException("Unknown recipe field: " + change.Key);
            _numbers.Parse(change.Key, change.Value);
        }

        ValidateRanges(recipe, patch);
    }

    private void ValidateRanges(RecipeDefinition recipe, RecipePatch patch)
    {
        var values = recipe.Fields.ToDictionary(x => x.Path, x => patch.Values.GetValueOrDefault(x.Path, x.Value));
        foreach (var start in values.Keys.Where(x => x.EndsWith("/Start", StringComparison.Ordinal)))
        {
            var stop = start[..^5] + "Stop";
            if (values.TryGetValue(stop, out var value) && _numbers.Parse(start, values[start]) > _numbers.Parse(stop, value))
                throw new InvalidDataException(start + " cannot exceed " + stop + ".");
        }
    }

    private void ValidateAttributes(AttributePatch patch)
    {
        var prefab = _catalog.FindPrefab(patch.PrefabName);
        if (patch.SourceHash != prefab.SourceHash)
            throw new InvalidDataException("Prefab source fingerprint mismatch.");
        foreach (var change in patch.Values)
        {
            if (!prefab.Attributes.Any(x => x.Name == change.Key))
                throw new InvalidDataException("Unsupported attribute: " + change.Key);
            _numbers.ValidateQuantity(change.Key, change.Value);
        }
    }
}
