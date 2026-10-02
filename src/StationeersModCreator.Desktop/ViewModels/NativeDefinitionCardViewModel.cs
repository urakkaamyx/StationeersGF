using Avalonia.Media.Imaging;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class NativeDefinitionCardViewModel
{
    public NativeDefinition Definition { get; }
    public string Name => Definition.Id;
    public string Category => Definition.Category;
    public Bitmap? Image { get; }
    public string State => Definition.CanExport ? "NATIVE MOD DEFINITION" : "INSPECT SOURCE";
    public RelayCommand SelectCommand { get; }

    public NativeDefinitionCardViewModel(NativeDefinition definition, IBitmapProvider images, Action<NativeDefinition> select)
    {
        Definition = definition;
        Image = images.Get(definition.Asset);
        SelectCommand = new(() => select(definition));
    }
}
