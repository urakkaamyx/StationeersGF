using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IProjectStore
{
    ModProject Load(string path);
    void Save(string path, ModProject project);
}
