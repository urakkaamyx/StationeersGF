using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface ISettingsStore
{
    EditorSettings Load();
    void Save(EditorSettings settings);
}
