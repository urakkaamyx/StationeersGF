using System.Xml.Linq;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class XmlDefinitionEditor
{
    public XElement Root { get; private set; }

    public XmlDefinitionEditor(string xml) => Root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
    public XElement Node(string path)
    {
        var e = Root;
        if (path.Length == 0)
            return e;
        foreach (var part in path.Split('/'))
        {
            var i = part.LastIndexOf('[');
            var name = part[..i];
            var index = int.Parse(part[(i + 1)..^1]);
            e = e.Elements(name).ElementAt(index);
        }

        return e;
    }

    public string ChildPath(string parent, XElement child)
    {
        var i = child.ElementsBeforeSelf(child.Name).Count();
        return (parent.Length == 0 ? "" : parent + "/") + child.Name.LocalName + "[" + i + "]";
    }

    public IReadOnlyList<XmlField> Fields(string path)
    {
        var e = Node(path);
        var fields = new List<XmlField>();
        foreach (var a in e.Attributes().Where(x => !x.IsNamespaceDeclaration && !(path.Length == 0 && x.Name.LocalName == "Id")))
            fields.Add(new((path.Length == 0 ? "" : path + "/") + "@" + a.Name.LocalName, a.Name.LocalName, a.Value));
        if (!e.HasElements && !string.IsNullOrWhiteSpace(e.Value))
            fields.Add(new((path.Length == 0 ? "" : path + "/") + "#text", e.Name.LocalName, e.Value));
        foreach (var child in e.Elements().Where(x => !x.HasElements))
        {
            var childPath = ChildPath(path, child);
            foreach (var a in child.Attributes().Where(x => !x.IsNamespaceDeclaration))
                fields.Add(new(childPath + "/@" + a.Name.LocalName, child.Name.LocalName + " · " + a.Name.LocalName, a.Value));
            if (child.HasAttributes == false && !string.IsNullOrWhiteSpace(child.Value))
                fields.Add(new(childPath + "/#text", child.Name.LocalName, child.Value));
        }

        return fields;
    }

    public void Set(string field, string value)
    {
        var i = field.LastIndexOf('/');
        var path = i < 0 ? "" : field[..i];
        var key = i < 0 ? field : field[(i + 1)..];
        var node = Node(path);
        if (key == "#text")
            node.Value = value;
        else
            node.SetAttributeValue(key[1..], value);
    }

    public void Add(string parent, string xml) => Node(parent).Add(XElement.Parse(xml));
    public void Remove(string path)
    {
        if (path.Length == 0)
            throw new InvalidDataException("Cannot remove the definition root.");
        Node(path).Remove();
    }

    public void Duplicate(string path)
    {
        var n = Node(path);
        n.AddAfterSelf(new XElement(n));
    }

    public void SetId(string id) => Root.SetAttributeValue("Id", id);
    public string Xml => Root.ToString(SaveOptions.DisableFormatting);
}
