using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class ExportPlanner : IExportPlanner
{
    private readonly ICatalogRepository _recipes;
    private readonly INativeCatalogRepository _native;
    public ExportPlanner(ICatalogRepository recipes, INativeCatalogRepository native)
    {
        _recipes = recipes;
        _native = native;
    }

    public IReadOnlyList<ExportPlanEntry> Plan(ModProject project)
    {
        var rows = new List<ExportPlanEntry>();
        foreach (var p in project.Recipes)
        {
            var r = _recipes.FindRecipe(p.RecipeId);
            rows.Add(new(r.Prefab, r.Section, "Replace exact recipe", p.Values.Count + " explicit field changes; recipe loader requires this complete record. No other recipes included."));
        }

        foreach (var p in project.Attributes)
            rows.Add(new(p.PrefabName, "Prefab attributes", "Override fields", p.Values.Count + " explicit properties only; other attributes inherited."));
        foreach (var p in project.Definitions)
        {
            var d = _native.Find(p.SourceKey);
            rows.Add(new(p.ExportId, d.Category, p.IsAddition ? "Add definition" : "Replace exact definition", d.Policy + " Only this record is emitted; sibling definitions are excluded."));
        }

        return rows;
    }
}
