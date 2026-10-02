using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IModExporter
{
    void Export(string path, ModProject project);
}
