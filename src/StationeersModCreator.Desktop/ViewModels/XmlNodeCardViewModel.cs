using Avalonia.Media.Imaging;
using System.Xml.Linq;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class XmlNodeCardViewModel
{
    public string Path { get; }
    public string Name { get; }
    public string Tag { get; }
    public string Detail { get; }
    public Bitmap? Image { get; }
    public RelayCommand OpenCommand { get; }

    public XmlNodeCardViewModel(XElement node, string path, IBitmapProvider images, Action<string> open)
    {
        Path = path;
        Tag = node.Name.LocalName;
        Name = (string? )node.Attribute("Id") ?? (string? )node.Attribute("Type") ?? Tag;
        Detail = string.Join(" · ", node.Attributes().Where(x => x.Name != "Id").Select(x => x.Name + ": " + x.Value));
        var quantity = (string? )node.Element("Quantity")?.Attribute("Value");
        if (quantity is not null)
            Detail = "Quantity " + quantity;
        Image = node.Name.LocalName is "Item" or "DynamicThing" ? images.Get((string? )node.Attribute("Id") + ".png") : null;
        OpenCommand = new(() => open(path));
    }
}
