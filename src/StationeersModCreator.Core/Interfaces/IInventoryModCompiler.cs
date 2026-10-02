using StationeersModCreator.Core.Models;
namespace StationeersModCreator.Core.Interfaces;
public interface IInventoryModCompiler { IReadOnlyList<NativeDefinitionChange> Compile(PendingInventoryDraft draft,ModProject project); }
