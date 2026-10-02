namespace StationeersModCreator.Core.Models;
public sealed record InventoryLayout(IReadOnlyList<InventoryPlacement> Placements,IReadOnlyList<string> Diagnostics);
