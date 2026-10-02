using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface INativeCatalogRepository
{
    IReadOnlyList<NativeDefinition> Definitions { get; }

    NativeDefinition Find(string key);
    NativeDefinition Resolve(string tag, string id);
}
