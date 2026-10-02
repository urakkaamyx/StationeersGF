using Avalonia.Media.Imaging;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class MachineCardViewModel : ObservableViewModel
{
    private bool _selected;
    private readonly IBitmapProvider _images;
    public MachineDefinition Definition { get; }
    public string Name => Definition.Name;
    public string Category => Definition.Category;
    public string CountLabel => Definition.RecipeCount + " recipes";
    public Bitmap? Image => _images.Get(Definition.Asset);
    public bool IsSelected { get => _selected; set => Set(ref _selected, value); }
    public RelayCommand SelectCommand { get; }

    public MachineCardViewModel(MachineDefinition definition, IBitmapProvider images, Action<MachineCardViewModel> select)
    {
        Definition = definition;
        _images = images;
        SelectCommand = new RelayCommand(() => select(this));
    }
}
