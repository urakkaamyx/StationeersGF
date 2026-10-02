namespace StationeersModCreator.Core.Models;
public sealed record RecipeDefinition(string Id, string Prefab, string DisplayName, string Section, string SourceFile, string SourceHash, string SourcePath, string OriginalXml, string Asset, List<NumericFieldDefinition> Fields);
