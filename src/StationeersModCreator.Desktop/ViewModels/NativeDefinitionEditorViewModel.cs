using System.Collections.ObjectModel;
using System.Xml.Linq;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Core.Services;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class NativeDefinitionEditorViewModel : ObservableViewModel
{
    private readonly INativeCatalogRepository _catalog;
    private readonly ICatalogRepository _prefabs;
    private readonly IBitmapProvider _images;
    private readonly IDraftService _draft;
    private readonly Action _changed;
    private readonly INativeForkService _fork;
    private XmlDefinitionEditor? _xml;
    private NativeDefinition? _source;
    private string _path = "";
    private string _id = "";
    private bool _pending;
    private string _status = "Select a definition card.";
    private string _itemId = "ItemIronFrames";
    private string _quantity = "1";
    private string _referenceId = "";
    private string _event = "NewWorld";
    private string _world = "Mars2";
    private string _property = "MiningYield";
    private string _propertyValue = "1";
    public ObservableCollection<NativeFieldEditorViewModel> Fields { get; } = [];
    public ObservableCollection<XmlNodeCardViewModel> Nodes { get; } = [];
    public NativeDefinition? Source => _source;
    public string Name => _source?.Id ?? "Definition inspector";
    public string Policy => _source?.Policy ?? "Select a definition.";
    public bool CanEdit => _source?.CanExport ?? false;
    public string Breadcrumb => _source is null ? "" : _source.Id + (_path.Length > 0 ? " / " + _path : "");
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool HasPendingEdits => _pending;

    public string ExportId
    {
        get => _id;
        set
        {
            if (Set(ref _id, value))
            {
                _pending = true;
                _changed();
            }
        }
    }

    public string ItemId { get => _itemId; set => Set(ref _itemId, value); }
    public string Quantity { get => _quantity; set => Set(ref _quantity, value); }
    public string ReferenceId { get => _referenceId; set => Set(ref _referenceId, value); }
    public string Event { get => _event; set => Set(ref _event, value); }
    public string WorldId { get => _world; set => Set(ref _world, value); }
    public string PropertyName { get => _property; set => Set(ref _property, value); }
    public string PropertyValue { get => _propertyValue; set => Set(ref _propertyValue, value); }
    public IReadOnlyList<string> ScalarProperties => _source is null ? [] : _catalog.Definitions.Where(x => x.Tag == _source.Tag).SelectMany(x => XElement.Parse(x.Xml).Elements()).Where(x => !x.HasElements && (x.Attribute("Value")is not null || !x.HasAttributes)).Select(x => x.Name.LocalName).Distinct().Order().ToList();
    public string[] Events { get; } = ["NewWorld", "NewPlayer", "NewPlayerKit", "RespawnPlayer", "RespawnPlayerKit"];
    public IReadOnlyList<string> ItemIds => _prefabs.Catalog.Prefabs.Select(x => x.Name).Order().ToList();
    public IReadOnlyList<string> SpawnIds => _catalog.Definitions.Where(x => x.Tag == "Spawn").Select(x => x.Id).Distinct().Order().ToList();
    public IReadOnlyList<string> WorldIds => _catalog.Definitions.Where(x => x.Tag == "World").Select(x => x.Id).Distinct().Order().ToList();
    public RelayCommand AddPropertyCommand { get; }
    public RelayCommand ForkReferenceCommand { get; }
    public RelayCommand RootCommand { get; }
    public RelayCommand ParentCommand { get; }
    public RelayCommand StageCommand { get; }
    public RelayCommand CloneCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand AddItemCommand { get; }
    public RelayCommand RemoveNodeCommand { get; }
    public RelayCommand DuplicateNodeCommand { get; }
    public RelayCommand AddReferenceCommand { get; }
    public RelayCommand FollowReferenceCommand { get; }
    public RelayCommand AssignWorldCommand { get; }

    public NativeDefinitionEditorViewModel(INativeCatalogRepository catalog, ICatalogRepository prefabs, IBitmapProvider images, IDraftService draft, INativeForkService fork, Action changed)
    {
        _catalog = catalog;
        _prefabs = prefabs;
        _images = images;
        _draft = draft;
        AddPropertyCommand = new(() => Run(AddProperty));
        _fork = fork;
        _changed = changed;
        ForkReferenceCommand = new(() => Run(ForkReference));
        RootCommand = new(() => OpenNode(""));
        ParentCommand = new(Parent);
        StageCommand = new(() => Run(Stage));
        CloneCommand = new(() => Run(Clone));
        RestoreCommand = new(() =>
        {
            if (_source is not null)
            {
                _draft.Project.PendingDefinition = null;
                Select(_source, true);
            }
        });
        AddItemCommand = new(() => Run(AddItem));
        RemoveNodeCommand = new(() => Run(RemoveNode));
        DuplicateNodeCommand = new(() => Run(DuplicateNode));
        AddReferenceCommand = new(() => Run(AddReference));
        FollowReferenceCommand = new(() => Run(FollowReference));
        AssignWorldCommand = new(() => Run(AssignWorld));
    }

    public void Select(NativeDefinition definition, bool discard = false)
    {
        if (_pending && !discard)
        {
            Status = "Stage or restore the current definition before selecting another.";
            return;
        }

        _source = definition;
        var pending = _draft.Project.PendingDefinition;
        var patch = pending?.SourceKey == definition.Key ? pending : _draft.Project.Definitions.LastOrDefault(x => x.SourceKey == definition.Key);
        _xml = new(patch?.Xml ?? definition.Xml);
        _id = patch?.ExportId ?? definition.Id;
        _pending = pending?.SourceKey == definition.Key;
        Notify(nameof(ScalarProperties));
        Notify(nameof(Source));
        Notify(nameof(Name));
        Notify(nameof(Policy));
        Notify(nameof(CanEdit));
        Notify(nameof(ExportId));
        OpenNode("");
        Status = definition.Policy;
    }

    public void OpenChange(NativeDefinitionChange change)
    {
        _source = _catalog.Find(change.SourceKey);
        _xml = new(change.Xml);
        _id = change.ExportId;
        _pending = false;
        foreach (var name in new[]
        {
            nameof(Source),
            nameof(Name),
            nameof(Policy),
            nameof(CanEdit),
            nameof(ExportId)
        }

        )
            Notify(name);
        OpenNode("");
    }

    public void OpenNode(string path)
    {
        if (_xml is null)
            return;
        _path = path;
        Fields.Clear();
        foreach (var field in _xml.Fields(path))
            Fields.Add(new(field, Edit));
        Nodes.Clear();
        foreach (var child in _xml.Node(path).Elements())
            Nodes.Add(new(child, _xml.ChildPath(path, child), _images, OpenNode));
        Notify(nameof(Breadcrumb));
    }

    private void Edit(string path, string value)
    {
        if (!CanEdit)
            return;
        _xml!.Set(path, value);
        _pending = true;
        _changed();
    }

    private void EnsureEditable()
    {
        if (!CanEdit || _xml is null || _source is null)
            throw new InvalidDataException("This definition is inspect-only until its loader policy is verified.");
    }

    private void Parent()
    {
        var i = _path.LastIndexOf('/');
        OpenNode(i < 0 ? "" : _path[..i]);
    }

    private void Clone()
    {
        EnsureEditable();
        var stem = "Forge." + _source!.Id;
        var id = stem;
        var count = 2;
        while (_catalog.Definitions.Any(x => x.Id == id) || _draft.Project.Definitions.Any(x => x.ExportId == id))
            id = stem + "." + count++;
        ExportId = id;
        Status = "Local copy ready. Stage it to add this definition without changing the source.";
    }

    private void Stage()
    {
        EnsureEditable();
        _xml!.SetId(ExportId);
        var addition = ExportId != _source!.Id;
        if (!addition && XNode.DeepEquals(XElement.Parse(_xml.Xml), XElement.Parse(_source.Xml)))
        {
            _draft.RemoveDefinition(ExportId);
            _pending = false;
            _changed();
            Status = "Source-equivalent definition removed from the mod.";
            return;
        }

        var change = new NativeDefinitionChange(_source.Key, _source.SourceHash, ExportId, _xml.Xml, addition);
        _draft.StageDefinition(change);
        _draft.Project.PendingDefinition = null;
        _pending = false;
        _changed();
        Status = (addition ? "New definition staged: " : "Exact definition replacement staged: ") + ExportId + ". No sibling records are included.";
    }

    private void AddProperty()
    {
        EnsureEditable();
        var template = _catalog.Definitions.Where(x => x.Tag == _source!.Tag).SelectMany(x => XElement.Parse(x.Xml).Elements(PropertyName)).FirstOrDefault(x => !x.HasElements && (x.Attribute("Value")is not null || !x.HasAttributes)) ?? throw new InvalidDataException("Choose an observed scalar property for this definition.");
        if (_xml!.Root.Element(PropertyName)is not null)
            throw new InvalidDataException("This property exists. Open its field instead.");
        var node = new XElement(template);
        if (node.Attribute("Value")is not null)
            node.SetAttributeValue("Value", PropertyValue);
        else
            node.Value = PropertyValue;
        _xml.Root.Add(node);
        _path = "";
        ChangedTree("Optional property added explicitly; other omitted fields retain native defaults.");
    }

    private void AddItem()
    {
        EnsureEditable();
        _prefabs.FindPrefab(ItemId);
        if (!int.TryParse(Quantity, out var q) || q < 1)
            throw new InvalidDataException("Use a positive whole item quantity.");
        var parent = _xml!.Node(_path);
        if (!new[]
        {
            "Spawn",
            "Item",
            "DynamicThing"
        }.Contains(parent.Name.LocalName))
            throw new InvalidDataException("Select a spawn package or item/container to add cargo.");
        _xml.Add(_path, new XElement("Item", new XAttribute("Id", ItemId), new XElement("Quantity", new XAttribute("Value", q))).ToString());
        ChangedTree("Cargo item added.");
    }

    private void RemoveNode()
    {
        EnsureEditable();
        _xml!.Remove(_path);
        var i = _path.LastIndexOf('/');
        _path = i < 0 ? "" : _path[..i];
        ChangedTree("Selected node removed.");
    }

    private void DuplicateNode()
    {
        EnsureEditable();
        if (_path.Length == 0)
            throw new InvalidDataException("Use Clone locally for an entire definition.");
        _xml!.Duplicate(_path);
        Parent();
        ChangedTree("Separate node duplicated; entries are not collapsed.");
    }

    private void AddReference()
    {
        EnsureEditable();
        if (!_catalog.Definitions.Any(x => x.Tag == "Spawn" && x.Id == ReferenceId) && !_draft.Project.Definitions.Any(x => x.ExportId == ReferenceId && XElement.Parse(x.Xml).Name == "Spawn"))
            throw new InvalidDataException("Choose an existing or staged spawn reference.");
        var node = new XElement("Spawn", new XAttribute("Id", ReferenceId));
        if (_xml!.Node(_path).Name == "StartCondition")
            node.SetAttributeValue("Event", Event);
        _xml.Add(_path, node.ToString());
        ChangedTree("Spawn reference added.");
    }

    private void ForkReference()
    {
        EnsureEditable();
        var changes = _fork.ForkReference(_source!, _xml!.Xml, _path, ExportId, _draft.Project);
        _draft.StageDefinitions(changes);
        _draft.Project.PendingDefinition = null;
        _pending = false;
        _changed();
        OpenChange(changes[0]);
        Status = "Local package added and only this parent's reference rewired. Both changes staged atomically.";
    }

    private void FollowReference()
    {
        if (_xml is null)
            return;
        var node = _xml.Node(_path);
        if (node.Name != "Spawn")
            throw new InvalidDataException("Open a Spawn binding/reference card first.");
        var id = (string? )node.Attribute("Id") ?? "";
        var change = _draft.Project.Definitions.FirstOrDefault(x => x.ExportId == id);
        if (_pending)
            throw new InvalidDataException("Stage or restore edits before following another definition.");
        if (change is not null)
            OpenChange(change);
        else
            Select(_catalog.Resolve("Spawn", id));
    }

    private void AssignWorld()
    {
        EnsureEditable();
        if (_xml!.Root.Name != "StartCondition")
            throw new InvalidDataException("World assignment is available on starting conditions.");
        if (!_catalog.Definitions.Any(x => x.Tag == "World" && x.Id == WorldId))
            throw new InvalidDataException("Choose an exact world ID.");
        _xml.Root.Element("WorldInject")?.Remove();
        _xml.Root.Add(new XElement("WorldInject", new XAttribute("Operator", "Any"), new XElement("World", new XAttribute("Id", WorldId))));
        ChangedTree("World injection set to " + WorldId + " using the game's exact-ID predicate.");
    }

    private void ChangedTree(string message)
    {
        _pending = true;
        _changed();
        OpenNode(_path);
        Status = message;
    }

    public void VerifyStaged()
    {
        if (_pending)
            throw new InvalidDataException("Stage or restore the native definition edits before exporting.");
    }

    public NativeDefinitionChange? CapturePending()
    {
        if (!_pending || _source is null || _xml is null)
            return null;
        _xml.SetId(ExportId);
        return new(_source.Key, _source.SourceHash, ExportId, _xml.Xml, ExportId != _source.Id);
    }

    public void ResetProject()
    {
        _pending = false;
        if (_draft.Project.PendingDefinition is { } pending)
        {
            _source = _catalog.Find(pending.SourceKey);
            Select(_source, true);
        }
        else if (_source is not null)
            Select(_source, true);
    }

    private void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
    }
}
