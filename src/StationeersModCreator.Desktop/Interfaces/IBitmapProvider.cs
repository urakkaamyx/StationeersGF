using Avalonia.Media.Imaging;

namespace StationeersModCreator.Desktop.Interfaces;
public interface IBitmapProvider
{
    Bitmap? Get(string name);
}
