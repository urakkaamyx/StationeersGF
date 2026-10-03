using Avalonia.Media.Imaging;
namespace StationeersModCreator.Desktop.ViewModels;
public sealed class SetupCardViewModel
{
    public string Label { get; }
    public string Title { get; }
    public string Description { get; }
    public Bitmap? Image { get; }
    public RelayCommand OpenCommand { get; }

    public SetupCardViewModel(string label, string title, string description, Bitmap? image, RelayCommand openCommand)
    {
        Label = label;
        Title = title;
        Description = description;
        Image = image;
        OpenCommand = openCommand;
    }
}
