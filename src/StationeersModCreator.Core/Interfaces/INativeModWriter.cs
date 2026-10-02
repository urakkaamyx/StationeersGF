using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface INativeModWriter
{
    byte[] Write(ModProject project);
}
