using Avalonia.Media.Imaging;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class ArtworkCardViewModel
{
    private readonly IBitmapProvider _images;
    public string Asset { get; }
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Asset);
    public Bitmap? Image => _images.Get(Asset);
    public RelayCommand SelectCommand { get; }

    public ArtworkCardViewModel(string asset, IBitmapProvider images, Action<string> select)
    {
        Asset = asset;
        _images = images;
        SelectCommand = new RelayCommand(() => select(Asset));
    }
}
