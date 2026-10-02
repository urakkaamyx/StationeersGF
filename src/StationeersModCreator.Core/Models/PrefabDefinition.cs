namespace StationeersModCreator.Core.Models;
public sealed record PrefabDefinition(string Name, string DisplayName, string ClassName, string Asset, string SourcePath, string SourceHash, long PathId, List<AttributeDefinition> Attributes);
