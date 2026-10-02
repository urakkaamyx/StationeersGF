using System.Text;
using System.Xml.Linq;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class NativeModWriter : INativeModWriter
{
    private readonly INativeCatalogRepository _catalog;
    public NativeModWriter(INativeCatalogRepository catalog) => _catalog = catalog;
    public byte[] Write(ModProject project)
    {
        var root = new XElement("GameData");
        foreach (var change in project.Definitions)
        {
            var source = _catalog.Find(change.SourceKey);
            var element = XElement.Parse(change.Xml, LoadOptions.PreserveWhitespace);
            if (source.Wrapper.Length == 0)
                root.Add(element);
            else
            {
                var wrapper = root.Element(source.Wrapper);
                if (wrapper is null)
                {
                    wrapper = new XElement(source.Wrapper);
                    root.Add(wrapper);
                }

                wrapper.Add(element);
            }
        }

        return Encoding.UTF8.GetBytes(new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString());
    }
}
