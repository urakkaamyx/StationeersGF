namespace StationeersModCreator.Core.Models;
public sealed record InventorySlotDefinition(int Index,string Key,int StringHash,int Type,string TypeName,int[] SpecificPrefabHashes,bool IsLocked,bool IsInteractable,bool IsSwappable,long LocationPathId,string AttachmentName);
