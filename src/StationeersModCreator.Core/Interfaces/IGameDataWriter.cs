using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IGameDataWriter
{
    Dictionary<string, byte[]> CreateFiles(ModProject project);
}
