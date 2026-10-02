using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface INativeDefinitionValidator
{
    void Validate(ModProject project);
    void ValidatePending(NativeDefinitionChange change);
    void ValidatePendingInventory(PendingInventoryDraft draft);
}
