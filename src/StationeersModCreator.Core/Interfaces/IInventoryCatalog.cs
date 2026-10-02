using StationeersModCreator.Core.Models;
namespace StationeersModCreator.Core.Interfaces;
public interface IInventoryCatalog { IReadOnlyList<InventoryPrefabDefinition> Prefabs{get;} InventoryPrefabDefinition Find(string name); }
