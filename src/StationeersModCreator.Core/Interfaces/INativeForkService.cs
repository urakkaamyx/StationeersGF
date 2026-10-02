using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface INativeForkService
{
    IReadOnlyList<NativeDefinitionChange> ForkReference(NativeDefinition parent, string parentXml, string referencePath, string parentExportId, ModProject project);
}
