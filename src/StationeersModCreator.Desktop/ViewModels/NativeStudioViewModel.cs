using System.Collections.ObjectModel;
using System.Xml.Linq;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class NativeStudioViewModel : ObservableViewModel
{
    private readonly INativeCatalogRepository _catalog;
    private readonly IBitmapProvider _images;
    private string _category = "Starting conditions";
    private string _search = "";
    private int _offset;
    public NativeDefinitionEditorViewModel Editor { get; }
    public ObservableCollection<NativeDefinitionCardViewModel> Definitions { get; } = [];
    public IReadOnlyList<string> Categories { get; }

    public string Category
    {
        get => _category;
        set
        {
            if (Set(ref _category, value))
            {
                _offset = 0;
                Refresh();
            }
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (Set(ref _search, value))
            {
                _offset = 0;
                Refresh();
            }
        }
    }

    public string CountLabel => Definitions.Count + " cards · page " + (_offset / 24 + 1);
    public RelayCommand WorldsCommand { get; }
    public RelayCommand StartsCommand { get; }
    public RelayCommand RespawnCommand { get; }
    public RelayCommand LandersCommand { get; }
    public RelayCommand LoadoutsCommand { get; }
    public RelayCommand DifficultyCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand PreviousCommand { get; }

    public NativeStudioViewModel(INativeCatalogRepository catalog, IBitmapProvider images, NativeDefinitionEditorViewModel editor)
    {
        _catalog = catalog;
        _images = images;
        Editor = editor;
        Categories = catalog.Definitions.Select(x => x.Category).Distinct().Order().ToList();
        WorldsCommand = new(() => OpenCategory("Worlds"));
        StartsCommand = new(() => OpenCategory("Starting conditions"));
        DifficultyCommand = new(() => OpenCategory("Difficulty"));
        LandersCommand = new(() => FilterSpawns("Lander"));
        RespawnCommand = new(() => FilterSpawns("Respawn"));
        LoadoutsCommand = new(() => FilterSpawns("Human"));
        NextCommand = new(() =>
        {
            _offset += 24;
            Refresh();
        });
        PreviousCommand = new(() =>
        {
            _offset = Math.Max(0, _offset - 24);
            Refresh();
        });
        Refresh();
    }

    private void FilterSpawns(string text)
    {
        _category = "Spawn packages";
        _search = text;
        _offset = 0;
        Notify(nameof(Category));
        Notify(nameof(Search));
        Refresh();
    }

    private void OpenCategory(string category)
    {
        _category = category;
        _search = "";
        _offset = 0;
        Notify(nameof(Category));
        Notify(nameof(Search));
        Refresh();
    }

    private void Refresh()
    {
        var matches = _catalog.Definitions.Where(x => x.Category == Category && x.Id.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (_offset >= matches.Count)
            _offset = Math.Max(0, ((matches.Count - 1) / 24) * 24);
        Definitions.Clear();
        foreach (var item in matches.Skip(_offset).Take(24))
            Definitions.Add(new(item, _images, x => Editor.Select(x)));
        Notify(nameof(CountLabel));
    }
}
