using System.Globalization;
using System.Xml.Linq;

namespace StationeersModCreator.Core.Services;
public sealed class NativeFieldTypeValidator
{
    public void Validate(string sourceXml, XElement edited)
    {
        var source = new XmlDefinitionEditor(sourceXml);
        var target = new XmlDefinitionEditor(edited.ToString());
        foreach (var original in source.Root.DescendantsAndSelf())
        {
            var path = PathOf(source, original);
            XElement node;
            try
            {
                node = target.Node(path);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            foreach (var field in source.Fields(path))
            {
                var tail = field.Path[(field.Path.LastIndexOf('/') + 1)..];
                if (tail == "@Id" || tail == "@Key" || tail == "@Path")
                    continue;
                var parent = field.Path.LastIndexOf('/');
                XElement actual;
                try
                {
                    actual = target.Node(parent < 0 ? "" : field.Path[..parent]);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }

                var value = tail == "#text" ? actual.Value : (string? )actual.Attribute(tail[1..]);
                if (value is null)
                    continue;
                if (bool.TryParse(field.Value, out _) && !bool.TryParse(value, out _))
                    throw new InvalidDataException(field.Label + " must be true or false.");
                if (double.TryParse(field.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) && (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number)))
                    throw new InvalidDataException(field.Label + " requires a finite number.");
            }
        }
    }

    private static string PathOf(XmlDefinitionEditor xml, XElement node)
    {
        if (node == xml.Root)
            return "";
        return xml.ChildPath(PathOf(xml, node.Parent!), node);
    }
}
