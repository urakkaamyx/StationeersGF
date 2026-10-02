using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IProjectHistory
{
    bool CanUndo { get; }

    bool CanRedo { get; }

    void Capture(ModProject project);
    ModProject Undo(ModProject current);
    ModProject Redo(ModProject current);
    void Clear();
}
