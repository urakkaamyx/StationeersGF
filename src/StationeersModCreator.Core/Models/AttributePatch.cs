namespace StationeersModCreator.Core.Models;
public sealed record AttributePatch(string PrefabName, string SourceHash, Dictionary<string, string> Values);
