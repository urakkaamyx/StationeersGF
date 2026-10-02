namespace StationeersModCreator.Core.Models;
public sealed record RecipePatch(string RecipeId, string SourceHash, Dictionary<string, string> Values);
