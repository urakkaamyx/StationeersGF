using StationeersModCreator.Core.Models;
namespace StationeersModCreator.Core.Interfaces;
public interface IInventoryLayoutService { IReadOnlyList<InventorySlotDefinition> Slots(string prefab,string species); InventoryLayout Layout(string xml,string species); bool Fits(InventorySlotDefinition slot,string prefab); }
