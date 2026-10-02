using System.Xml.Linq;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class NativeForkService : INativeForkService
{
    private readonly INativeCatalogRepository _catalog;
    public NativeForkService(INativeCatalogRepository catalog) => _catalog = catalog;
    public IReadOnlyList<NativeDefinitionChange> ForkReference(NativeDefinition parent, string parentXml, string referencePath, string parentExportId, ModProject project)
    {
        var xml = new XmlDefinitionEditor(parentXml);
        var node = xml.Node(referencePath);
        if (node.Name != "Spawn")
            throw new InvalidDataException("Open a Spawn reference card to fork its target.");
        var source = _catalog.Resolve("Spawn", (string? )node.Attribute("Id") ?? "");
        var stem = "Forge." + source.Id;
        var id = stem;
        var count = 2;
        while (_catalog.Definitions.Any(x => x.Id == id) || project.Definitions.Any(x => x.ExportId == id))
            id = stem + "." + count++;
        var child = XElement.Parse(source.Xml);
        child.SetAttributeValue("Id", id);
        node.SetAttributeValue("Id", id);
        xml.SetId(parentExportId);
        return[new(source.Key, source.SourceHash, id, child.ToString(SaveOptions.DisableFormatting), true), new(parent.Key, parent.SourceHash, parentExportId, xml.Xml, parentExportId != parent.Id)];
    }
}
