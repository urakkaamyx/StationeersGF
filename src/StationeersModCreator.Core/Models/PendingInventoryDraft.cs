namespace StationeersModCreator.Core.Models;
public sealed record PendingInventoryDraft(InventoryContext Context,string ExportStartId,string SourceKey,string SourceHash,string Xml);
