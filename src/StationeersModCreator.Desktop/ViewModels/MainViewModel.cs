using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Media.Imaging;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class MainViewModel : ObservableViewModel
{
    private readonly ICatalogRepository _catalog;
    private readonly IArtworkRepository _artwork;
    private readonly IBitmapProvider _images;
    private readonly IDraftService _draft;
    private readonly IProjectStore _projects;
    private readonly ISettingsStore _settingsStore;
    private readonly IThemeService _theme;
    private readonly IModExporter _exporter;
    private readonly IProjectValidator _validator;
    private readonly IFileDialogService _dialogs;
    private readonly List<RecipeCardViewModel> _allRecipes;
    private readonly List<PrefabCardViewModel> _allPrefabs;
    private readonly EditorSettings _settings;
    private string _page = "Recipes";
    private string _search = "";
    private string _status = "Ready. Select a recipe to create an override.";
    private bool _settingsOpen;
    private int _offset;
    private MachineCardViewModel? _machine;
    private RecipeCardViewModel? _recipe;
    private PrefabCardViewModel? _prefab;
    private bool _dirty;
    private readonly INativeCatalogRepository _nativeCatalog;
    public NativeStudioViewModel NativeStudio { get; }
    public RelayCommand NativeStudioCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }
    public bool IsNativeStudio => _page == "Native";
    public ObservableCollection<ExportPlanEntry> ExportPlan { get; } = [];

    private readonly IExportPlanner _planner;
    public ObservableCollection<MachineCardViewModel> Machines { get; } = [];
    public ObservableCollection<RecipeCardViewModel> Recipes { get; } = [];
    public ObservableCollection<PrefabCardViewModel> Prefabs { get; } = [];
    public ObservableCollection<ArtworkCardViewModel> Artwork { get; } = [];
    public ObservableCollection<DraftChangeViewModel> Changes { get; } = [];
    public string[] Accents { get; } = ["Cyan", "Amber", "Violet", "Rose"];

    public string Search
    {
        get => _search;
        set
        {
            if (Set(ref _search, value))
            {
                _offset = 0;
                RefreshVisibleCards();
            }
        }
    }

    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool IsRecipes => _page == "Recipes";
    public bool IsAttributes => _page == "Attributes";
    public bool IsArtwork => _page == "Artwork";
    public bool IsWorkspace => _page == "Workspace";
    public string PageTitle => _page switch
    {
        "Native" => "World & content studio",
        "Attributes" => "Prefab attributes",
        "Artwork" => "Asset library",
        "Workspace" => "Your mod workspace",
        _ => "Recipe laboratory"
    };
    public string PageDescription => _page switch
    {
        "Native" => "Starts, equipment, respawn, landers and native game definitions.",
        "Attributes" => "Tune the supported native attributes of real game prefabs.",
        "Artwork" => "Choose game artwork for your mod's preview image.",
        "Workspace" => "Review your overrides, edit metadata, then export.",
        _ => "Select a production system. Pick an item. Make it yours."
    };
    public bool SettingsOpen { get => _settingsOpen; set => Set(ref _settingsOpen, value); }
    public RecipeCardViewModel? SelectedRecipe { get => _recipe; private set => Set(ref _recipe, value); }
    public PrefabCardViewModel? SelectedPrefab { get => _prefab; private set => Set(ref _prefab, value); }
    public string MachineName => _machine?.Name ?? "";
    public Bitmap? MachineImage => _machine?.Image;
    public string MachineCategory => _machine?.Category ?? "";
    public string CatalogLabel => _catalog.Catalog.Recipes.Count + " recipes  /  " + _catalog.Catalog.Prefabs.Count + " prefabs";
    public string SnapshotLabel => "UNITY " + _catalog.Catalog.UnityVersion + "  •  " + _catalog.Catalog.Snapshot;
    public string ResultLabel => IsRecipes ? Recipes.Count + " recipes in this system" : IsAttributes ? Prefabs.Count + " / " + _allPrefabs.Count + " supported prefabs" : Artwork.Count + " artwork cards";
    public string PageLabel => "PAGE " + (_offset / 36 + 1);
    public double CardWidth => CompactCards ? 155 : 185;
    public double FontSize => 14 * Math.Clamp(_settings.FontScale, .85, 1.3);

    public string Accent
    {
        get => _settings.Accent;
        set
        {
            _settings.Accent = value;
            Notify();
            ApplySettings();
        }
    }

    public bool HighContrast
    {
        get => _settings.HighContrast;
        set
        {
            _settings.HighContrast = value;
            Notify();
            ApplySettings();
        }
    }

    public bool CompactCards
    {
        get => _settings.CompactCards;
        set
        {
            _settings.CompactCards = value;
            Notify();
            Notify(nameof(CardWidth));
            ApplySettings();
        }
    }

    public double FontScale
    {
        get => _settings.FontScale;
        set
        {
            _settings.FontScale = value;
            Notify();
            Notify(nameof(FontSize));
            ApplySettings();
        }
    }

    public string ModName
    {
        get => _draft.Project.Name;
        set
        {
            _draft.Project.Name = value;
            Notify();
            MarkDirty();
        }
    }

    public string Author
    {
        get => _draft.Project.Author;
        set
        {
            _draft.Project.Author = value;
            Notify();
            MarkDirty();
        }
    }

    public string Version
    {
        get => _draft.Project.Version;
        set
        {
            _draft.Project.Version = value;
            Notify();
            MarkDirty();
        }
    }

    public string Description
    {
        get => _draft.Project.Description;
        set
        {
            _draft.Project.Description = value;
            Notify();
            MarkDirty();
        }
    }

    public Bitmap? PreviewImage => _images.Get(_draft.Project.PreviewAsset);
    public string PreviewName => _draft.Project.PreviewAsset;
    public string DraftLabel => _draft.ChangeCount + (_draft.ChangeCount == 1 ? " staged override" : " staged overrides");
    public bool HasUnsavedProject => _dirty;
    public string SaveState => _dirty ? "UNSAVED PROJECT" : "PROJECT READY";
    public RelayCommand RecipesCommand { get; }
    public RelayCommand AttributesCommand { get; }
    public RelayCommand ArtworkCommand { get; }
    public RelayCommand WorkspaceCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public RelayCommand CloseSettingsCommand { get; }
    public RelayCommand StageRecipeCommand { get; }
    public RelayCommand ResetRecipeCommand { get; }
    public RelayCommand StageAttributesCommand { get; }
    public RelayCommand ResetAttributesCommand { get; }
    public RelayCommand NextPageCommand { get; }
    public RelayCommand PreviousPageCommand { get; }
    public RelayCommand NewProjectCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand OpenCommand { get; }
    public AsyncRelayCommand ExportCommand { get; }

    public MainViewModel(ICatalogRepository catalog, IArtworkRepository artwork, IBitmapProvider images, IDraftService draft, IProjectStore projects, ISettingsStore settingsStore, IThemeService theme, IModExporter exporter, IProjectValidator validator, IFileDialogService dialogs, INativeCatalogRepository nativeCatalog, IExportPlanner planner, INativeForkService fork)
    {
        _catalog = catalog;
        _artwork = artwork;
        _images = images;
        _draft = draft;
        _projects = projects;
        _settingsStore = settingsStore;
        _theme = theme;
        _exporter = exporter;
        _validator = validator;
        _dialogs = dialogs;
        _settings = settingsStore.Load();
        _nativeCatalog = nativeCatalog;
        _planner = planner;
        var editor = new NativeDefinitionEditorViewModel(nativeCatalog, catalog, images, draft, fork, () =>
        {
            RefreshChanges();
            MarkDirty();
        });
        NativeStudio = new NativeStudioViewModel(nativeCatalog, images, editor);
        NativeStudioCommand = new(() => Navigate("Native"));
        UndoCommand = new(() => RunAction(Undo));
        RedoCommand = new(() => RunAction(Redo));
        _allRecipes = catalog.Catalog.Recipes.Select(x => new RecipeCardViewModel(x, images, SelectRecipe)).ToList();
        _allPrefabs = catalog.Catalog.Prefabs.Where(x => x.Attributes.Count > 0).Select(x => new PrefabCardViewModel(x, images, SelectPrefab)).ToList();
        foreach (var definition in catalog.Catalog.Machines)
            Machines.Add(new MachineCardViewModel(definition, images, SelectMachine));
        RecipesCommand = new(() => Navigate("Recipes"));
        AttributesCommand = new(() => Navigate("Attributes"));
        ArtworkCommand = new(() => Navigate("Artwork"));
        WorkspaceCommand = new(() => Navigate("Workspace"));
        SettingsCommand = new(() => SettingsOpen = true);
        CloseSettingsCommand = new(() => SettingsOpen = false);
        StageRecipeCommand = new(() => RunAction(StageRecipe));
        ResetRecipeCommand = new(ResetRecipe);
        StageAttributesCommand = new(() => RunAction(StageAttributes));
        ResetAttributesCommand = new(ResetAttributes);
        NextPageCommand = new(NextPage);
        PreviousPageCommand = new(PreviousPage);
        NewProjectCommand = new(() => RunAction(NewProject));
        SaveCommand = new(() => RunAsync(SaveProjectAsync));
        OpenCommand = new(() => RunAsync(OpenProjectAsync));
        ExportCommand = new(() => RunAsync(ExportProjectAsync));
        SubscribeToFieldEdits();
        _theme.Apply(_settings);
        SelectMachine(Machines[0]);
        SelectPrefab(_allPrefabs.First(x => x.Definition.Name == "ItemIronOre"));
        RefreshChanges();
    }

    private void SubscribeToFieldEdits()
    {
        foreach (var field in _allRecipes.SelectMany(x => x.Fields).Concat(_allPrefabs.SelectMany(x => x.Fields)))
            field.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FieldEditorViewModel.Value))
                    MarkDirty();
            };
    }

    private void VerifyAllEditsStaged()
    {
        NativeStudio.Editor.VerifyStaged();
        foreach (var card in _allRecipes)
        {
            var patch = _draft.Project.Recipes.SingleOrDefault(x => x.RecipeId == card.Definition.Id);
            if (card.Fields.Any(x => x.Value != (patch?.Values.GetValueOrDefault(x.Path, x.Original) ?? x.Original)))
                throw new InvalidDataException("Stage or restore the pending recipe edits before exporting.");
        }

        foreach (var card in _allPrefabs)
        {
            var patch = _draft.Project.Attributes.SingleOrDefault(x => x.PrefabName == card.Definition.Name);
            if (card.Fields.Any(x => x.Value != (patch?.Values.GetValueOrDefault(x.Path, x.Original) ?? x.Original)))
                throw new InvalidDataException("Stage or restore the pending prefab edits before exporting.");
        }
    }

    private void Navigate(string page)
    {
        Status = page == "Native" ? "Select a definition card; stage changes to include them in the mod." : "Ready. Select a card to edit your mod.";
        _page = page;
        _offset = 0;
        _search = "";
        foreach (var name in new[]
        {
            nameof(IsNativeStudio),
            nameof(IsRecipes),
            nameof(IsAttributes),
            nameof(IsArtwork),
            nameof(IsWorkspace),
            nameof(PageTitle),
            nameof(PageDescription),
            nameof(Search)
        }

        )
            Notify(name);
        RefreshVisibleCards();
    }

    private void SelectMachine(MachineCardViewModel machine)
    {
        foreach (var item in Machines)
            item.IsSelected = item == machine;
        _machine = machine;
        _search = "";
        Notify(nameof(Search));
        Notify(nameof(MachineName));
        Notify(nameof(MachineImage));
        Notify(nameof(MachineCategory));
        RefreshVisibleCards();
        SelectRecipe(Recipes.First());
    }

    private void SelectRecipe(RecipeCardViewModel recipe)
    {
        if (SelectedRecipe is not null)
            SelectedRecipe.IsSelected = false;
        SelectedRecipe = recipe;
        recipe.IsSelected = true;
    }

    private void SelectPrefab(PrefabCardViewModel prefab) => SelectedPrefab = prefab;
    private void RefreshVisibleCards()
    {
        Recipes.Clear();
        foreach (var card in _allRecipes.Where(x => x.Definition.Section == _machine?.Definition.Id && Matches(x.Name, x.Prefab)))
            Recipes.Add(card);
        Prefabs.Clear();
        foreach (var card in _allPrefabs.Where(x => Matches(x.Name, x.Definition.Name)).Skip(_offset).Take(36))
            Prefabs.Add(card);
        Artwork.Clear();
        foreach (var name in _artwork.Names.Where(x => Matches(x, x)).Skip(_offset).Take(36))
            Artwork.Add(new ArtworkCardViewModel(name, _images, SelectPreview));
        Notify(nameof(ResultLabel));
        Notify(nameof(PageLabel));
    }

    private bool Matches(string name, string id) => name.Contains(Search, StringComparison.OrdinalIgnoreCase) || id.Contains(Search, StringComparison.OrdinalIgnoreCase);
    private void NextPage()
    {
        var total = IsArtwork ? _artwork.Names.Count(x => Matches(x, x)) : _allPrefabs.Count(x => Matches(x.Name, x.Definition.Name));
        if (_offset + 36 >= total)
            return;
        _offset += 36;
        RefreshVisibleCards();
    }

    private void PreviousPage()
    {
        _offset = Math.Max(0, _offset - 36);
        RefreshVisibleCards();
    }

    private void StageRecipe()
    {
        var card = SelectedRecipe ?? throw new InvalidDataException("Select a recipe first.");
        _draft.StageRecipe(card.Definition, card.Fields.ToDictionary(x => x.Path, x => x.Value));
        card.IsStaged = _draft.Project.Recipes.Any(x => x.RecipeId == card.Definition.Id);
        RefreshChanges();
        MarkDirty();
        Status = card.Name + " override staged. Export from the workspace.";
    }

    private void ResetRecipe()
    {
        if (SelectedRecipe is null)
            return;
        foreach (var field in SelectedRecipe.Fields)
            field.Reset();
        _draft.RemoveRecipe(SelectedRecipe.Definition.Id);
        SelectedRecipe.IsStaged = false;
        RefreshChanges();
        MarkDirty();
        Status = "Recipe restored to its source values.";
    }

    private void StageAttributes()
    {
        var card = SelectedPrefab ?? throw new InvalidDataException("Select a prefab first.");
        _draft.StageAttributes(card.Definition, card.Fields.ToDictionary(x => x.Path, x => x.Value));
        RefreshChanges();
        MarkDirty();
        Status = card.Name + " attributes staged.";
    }

    private void ResetAttributes()
    {
        if (SelectedPrefab is null)
            return;
        foreach (var field in SelectedPrefab.Fields)
            field.Reset();
        _draft.RemoveAttributes(SelectedPrefab.Definition.Name);
        RefreshChanges();
        MarkDirty();
        Status = "Prefab attributes restored.";
    }

    private void SelectPreview(string name)
    {
        _draft.Project.PreviewAsset = name;
        Notify(nameof(PreviewImage));
        Notify(nameof(PreviewName));
        MarkDirty();
        Status = "Mod preview set to " + name;
    }

    private void RefreshChanges()
    {
        Changes.Clear();
        foreach (var patch in _draft.Project.Recipes)
        {
            var recipe = _catalog.FindRecipe(patch.RecipeId);
            Changes.Add(new DraftChangeViewModel(recipe.DisplayName, recipe.Section, string.Join("  ·  ", patch.Values.Select(x => x.Key + ": " + recipe.Fields.Single(f => f.Path == x.Key).Value + " → " + x.Value)), new RelayCommand(() => RemoveRecipe(patch.RecipeId))));
        }

        foreach (var patch in _draft.Project.Attributes)
        {
            var prefab = _catalog.FindPrefab(patch.PrefabName);
            Changes.Add(new DraftChangeViewModel(prefab.DisplayName, "Prefab attributes", string.Join("  ·  ", patch.Values.Select(x => x.Key + ": " + prefab.Attributes.Single(f => f.Name == x.Key).Value + " → " + x.Value)), new RelayCommand(() => RemoveAttributes(patch.PrefabName))));
        }

        foreach (var patch in _draft.Project.Definitions)
        {
            var definition = _nativeCatalog.Find(patch.SourceKey);
            Changes.Add(new DraftChangeViewModel(patch.ExportId, definition.Category, patch.IsAddition ? "Add this definition only" : "Replace this exact definition only — required by native loader", new RelayCommand(() =>
            {
                _draft.RemoveDefinition(patch.ExportId);
                RefreshChanges();
                NativeStudio.Editor.ResetProject();
                MarkDirty();
            })));
        }

        ExportPlan.Clear();
        foreach (var entry in _planner.Plan(_draft.Project))
            ExportPlan.Add(entry);
        Notify(nameof(DraftLabel));
    }

    private void RemoveRecipe(string id)
    {
        _draft.RemoveRecipe(id);
        var card = _allRecipes.Single(x => x.Definition.Id == id);
        card.IsStaged = false;
        foreach (var field in card.Fields)
            field.Reset();
        RefreshChanges();
        MarkDirty();
    }

    private void RemoveAttributes(string name)
    {
        _draft.RemoveAttributes(name);
        var card = _allPrefabs.Single(x => x.Definition.Name == name);
        foreach (var field in card.Fields)
            field.Reset();
        RefreshChanges();
        MarkDirty();
    }

    private void ApplySettings()
    {
        _theme.Apply(_settings);
        RunAction(() => _settingsStore.Save(_settings));
    }

    private void Undo()
    {
        VerifyAllEditsStaged();
        if (!_draft.CanUndo)
        {
            Status = "Nothing to undo.";
            return;
        }

        _draft.Undo();
        RestoreRecipeFields();
        RestoreAttributeFields();
        NativeStudio.Editor.ResetProject();
        RefreshChanges();
        MarkDirty();
        Status = "Previous staged edit restored.";
    }

    private void Redo()
    {
        VerifyAllEditsStaged();
        if (!_draft.CanRedo)
        {
            Status = "Nothing to redo.";
            return;
        }

        _draft.Redo();
        RestoreRecipeFields();
        RestoreAttributeFields();
        NativeStudio.Editor.ResetProject();
        RefreshChanges();
        MarkDirty();
        Status = "Staged edit reapplied.";
    }

    private ModProject CaptureProject()
    {
        var snapshot = JsonSerializer.Deserialize<ModProject>(JsonSerializer.Serialize(_draft.Project))!;
        snapshot.FormatVersion = 2;
        snapshot.PendingRecipes.Clear();
        foreach (var card in _allRecipes)
        {
            var patch = snapshot.Recipes.FirstOrDefault(x => x.RecipeId == card.Definition.Id);
            var values = card.Fields.Where(x => x.Value != (patch?.Values.GetValueOrDefault(x.Path, x.Original) ?? x.Original)).ToDictionary(x => x.Path, x => x.Value);
            if (values.Count > 0)
                snapshot.PendingRecipes.Add(new(card.Definition.Id, card.Definition.SourceHash, values));
        }

        snapshot.PendingAttributes.Clear();
        foreach (var card in _allPrefabs)
        {
            var patch = snapshot.Attributes.FirstOrDefault(x => x.PrefabName == card.Definition.Name);
            var values = card.Fields.Where(x => x.Value != (patch?.Values.GetValueOrDefault(x.Path, x.Original) ?? x.Original)).ToDictionary(x => x.Path, x => x.Value);
            if (values.Count > 0)
                snapshot.PendingAttributes.Add(new(card.Definition.Name, card.Definition.SourceHash, values));
        }

        snapshot.PendingDefinition = NativeStudio.Editor.CapturePending();
        return snapshot;
    }

    private void MarkDirty()
    {
        _dirty = true;
        Notify(nameof(SaveState));
    }

    private void NewProject()
    {
        if (_dirty)
            throw new InvalidDataException("Save this project before starting a new one.");
        SetProject(new ModProject { CatalogFingerprint = _catalog.Fingerprint, Author = "Player" });
        Status = "New mod project created.";
    }

    private void SetProject(ModProject project)
    {
        _draft.Replace(project);
        NativeStudio.Editor.ResetProject();
        RestoreRecipeFields();
        RestoreAttributeFields();
        RefreshChanges();
        _dirty = false;
        foreach (var name in new[]
        {
            nameof(ModName),
            nameof(Author),
            nameof(Version),
            nameof(Description),
            nameof(PreviewImage),
            nameof(PreviewName),
            nameof(SaveState)
        }

        )
            Notify(name);
    }

    private void RestoreRecipeFields()
    {
        foreach (var card in _allRecipes)
        {
            var patch = _draft.Project.Recipes.SingleOrDefault(x => x.RecipeId == card.Definition.Id);
            foreach (var field in card.Fields)
                field.Value = _draft.Project.PendingRecipes.FirstOrDefault(x => x.RecipeId == card.Definition.Id)?.Values.GetValueOrDefault(field.Path, patch?.Values.GetValueOrDefault(field.Path, field.Original) ?? field.Original) ?? patch?.Values.GetValueOrDefault(field.Path, field.Original) ?? field.Original;
            card.IsStaged = patch is not null;
        }
    }

    private void RestoreAttributeFields()
    {
        foreach (var card in _allPrefabs)
        {
            var patch = _draft.Project.Attributes.SingleOrDefault(x => x.PrefabName == card.Definition.Name);
            foreach (var field in card.Fields)
                field.Value = _draft.Project.PendingAttributes.FirstOrDefault(x => x.PrefabName == card.Definition.Name)?.Values.GetValueOrDefault(field.Path, patch?.Values.GetValueOrDefault(field.Path, field.Original) ?? field.Original) ?? patch?.Values.GetValueOrDefault(field.Path, field.Original) ?? field.Original;
        }
    }

    private async Task SaveProjectAsync()
    {
        var snapshot = CaptureProject();
        _validator.Validate(snapshot, false);
        var path = await _dialogs.SaveFileAsync("Save mod project", ModName + ".modforge.json", "json");
        if (path is null)
            return;
        _projects.Save(path, snapshot);
        _dirty = false;
        Notify(nameof(SaveState));
        Status = "Project saved: " + path;
    }

    private async Task OpenProjectAsync()
    {
        if (_dirty)
            throw new InvalidDataException("Save this project before opening another one.");
        var path = await _dialogs.OpenProjectAsync();
        if (path is null)
            return;
        var project = _projects.Load(path);
        _validator.Validate(project, false);
        SetProject(project);
        Status = "Project opened: " + path;
    }

    private async Task ExportProjectAsync()
    {
        VerifyAllEditsStaged();
        _validator.Validate(_draft.Project, true);
        var path = await _dialogs.SaveFileAsync("Export native Stationeers mod", ModName + ".zip", "zip");
        if (path is null)
            return;
        _exporter.Export(path, _draft.Project);
        Status = "Mod exported: " + path + ". Set its load order and verify it in game.";
    }

    private void RunAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }
}
