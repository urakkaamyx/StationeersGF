using Avalonia.Media.Imaging;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class PrefabCardViewModel : ObservableViewModel
{
    private readonly IBitmapProvider _images;
    public PrefabDefinition Definition { get; }
    public string Name => Definition.DisplayName;
    public string Subtitle => Definition.Attributes.Count + " editable attributes";
    public Bitmap? Image => _images.Get(Definition.Asset);
    public IReadOnlyList<FieldEditorViewModel> Fields { get; }
    public RelayCommand SelectCommand { get; }

    public PrefabCardViewModel(PrefabDefinition definition, IBitmapProvider images, Action<PrefabCardViewModel> select)
    {
        Definition = definition;
        _images = images;
        Fields = definition.Attributes.Select(x => new FieldEditorViewModel(x.Name, x.Name, x.Value, "ATTRIBUTES")).ToArray();
        SelectCommand = new RelayCommand(() => select(this));
    }
}
