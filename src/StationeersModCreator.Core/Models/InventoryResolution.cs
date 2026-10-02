namespace StationeersModCreator.Core.Models;
public sealed record InventoryResolution(string Xml,IReadOnlyList<string> Sources,IReadOnlyList<string> Diagnostics);
