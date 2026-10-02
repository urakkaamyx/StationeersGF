using Avalonia.Media.Imaging;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class RecipeCardViewModel : ObservableViewModel
{
    private bool _selected;
    private bool _staged;
    private readonly IBitmapProvider _images;
    public RecipeDefinition Definition { get; }
    public string Name => Definition.DisplayName;
    public string Prefab => Definition.Prefab;
    public string ProcessLabel => string.Join("  ·  ", Definition.Fields.Where(x => x.Group == "PROCESS").Select(x => x.Label + " " + x.Value));
    public string MaterialLabel => string.Join(" · ", Definition.Fields.Where(x => x.Group == "MATERIALS").Take(3).Select(x => x.Label + " " + x.Value));
    public Bitmap? Image => _images.Get(Definition.Asset);
    public IReadOnlyList<FieldEditorViewModel> Fields { get; }
    public bool IsSelected { get => _selected; set => Set(ref _selected, value); }

    public bool IsStaged
    {
        get => _staged;
        set
        {
            if (Set(ref _staged, value))
                Notify(nameof(StateLabel));
        }
    }

    public string StateLabel => IsStaged ? "OVERRIDE STAGED" : "BASE GAME";
    public RelayCommand SelectCommand { get; }

    public RecipeCardViewModel(RecipeDefinition definition, IBitmapProvider images, Action<RecipeCardViewModel> select)
    {
        Definition = definition;
        _images = images;
        Fields = definition.Fields.Select(x => new FieldEditorViewModel(x.Path, x.Label, x.Value, x.Group)).ToArray();
        SelectCommand = new RelayCommand(() => select(this));
    }
}
