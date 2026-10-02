namespace StationeersModCreator.Core.Models;
public sealed record InventoryPrefabDefinition(string Name,long PathId,string ClassName,int PrefabHash,int SlotType,string SlotTypeName,decimal? MaxQuantity,IReadOnlyList<InventorySlotDefinition> Slots);
