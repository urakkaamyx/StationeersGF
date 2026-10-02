using System.IO.Compression;
using System.Xml.Linq;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Core.Services;

namespace StationeersModCreator.Checks;
public sealed class NativeCheckRunner
{
    private readonly JsonCatalogRepository _catalog;
    private readonly JsonNativeCatalogRepository _native;
    private readonly ProjectValidator _validator;
    private readonly string _content;
    public NativeCheckRunner(string content)
    {
        _content = content;
        _catalog = new(Path.Combine(content, "catalog.json"));
        _native = new(Path.Combine(content, "native-catalog.json"));
        _validator = new(_catalog, new NumericValueValidator(), new NativeDefinitionValidator(_native, _catalog));
    }

    private ModProject Project() => new()
    {
        CatalogFingerprint = _catalog.Fingerprint
    };
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException("Invalid definition accepted.");
    }

    private NativeDefinitionChange Change(string tag, string id, string? replacement = null, Action<XElement>? edit = null)
    {
        var source = _native.Resolve(tag, id);
        var xml = XElement.Parse(source.Xml);
        xml.SetAttributeValue("Id", replacement ?? id);
        edit?.Invoke(xml);
        return new(source.Key, source.SourceHash, replacement ?? id, xml.ToString(), replacement is not null);
    }

    public void Run()
    {
        CheckMinimalExport();
        CheckNewStart();
        CheckAtomicFork();
        CheckCycleAndMissing();
        CheckInvalidValues();
        CheckUnknownPolicy();
        CheckWorldReplacement();
        CheckProjectPersistence();
        Console.WriteLine("PASS: 8 native mod compiler checks.");
    }

    private void CheckMinimalExport()
    {
        var p = Project();
        p.Definitions.Add(Change("DifficultySetting", "Normal", edit: x => x.Element("RespawnStressTime")!.SetAttributeValue("Value", "900")));
        _validator.Validate(p, true);
        var xml = XElement.Parse(System.Text.Encoding.UTF8.GetString(new NativeModWriter(_native).Write(p)));
        Require(xml.Descendants("DifficultySetting").Count() == 1, "Unrelated difficulties copied.");
        Require(xml.Descendants("HungerRate").Single().Attribute("Value")!.Value == "1.0", "Required replacement context lost.");
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".zip");
        try
        {
            new ModExporter(_validator, new NativeGameDataWriter(_catalog, new NativeModWriter(_native)), new ZipArtworkRepository(Path.Combine(_content, "artwork.zip")), new AtomicFileWriter()).Export(path, p);
            using var z = ZipFile.OpenRead(path);
            Require(!z.Entries.Any(e => e.FullName.EndsWith(".json") || e.FullName.Contains("catalog")), "Project/catalog copied into mod.");
            Require(z.Entries.Count(e => e.FullName.Contains("GameData/")) == 1, "Unexpected native mod files.");
        }
        finally
        {
            File.Delete(path);
        }

        Console.WriteLine("PASS: exact difficulty replacement only; no source siblings, catalog or editable project in mod.");
    }

    private void CheckNewStart()
    {
        var p = Project();
        p.Definitions.Add(Change("StartCondition", "DefaultStart", "Forge.Expedition", x =>
        {
            x.Element("WorldInject")?.Remove();
            x.Add(new XElement("WorldInject", new XAttribute("Operator", "Any"), new XElement("World", new XAttribute("Id", "Mars2"))));
        }));
        _validator.Validate(p, true);
        var x = XElement.Parse(p.Definitions[0].Xml);
        Require(x.Elements("Spawn").Select(e => (string? )e.Attribute("Event")).Distinct().Count() == 5, "Spawn events conflated.");
        Require(x.Descendants("World").Single().Attribute("Id")!.Value == "Mars2", "World identity lost.");
        Console.WriteLine("PASS: new starting condition retains all five events and exact world injection.");
    }

    private void CheckAtomicFork()
    {
        var source = _native.Resolve("Spawn", "DefaultLander");
        var editor = new XmlDefinitionEditor(source.Xml);
        var node = editor.Root.Descendants("Spawn").First(x => (string? )x.Attribute("Id") == "ConstructionSupplies");
        var parent = editor.ChildPath("", editor.Root.Element("DynamicThing")!);
        var path = editor.ChildPath(parent, node);
        var p = Project();
        var changes = new NativeForkService(_native).ForkReference(source, source.Xml, path, source.Id, p);
        var draft = new DraftService(_catalog.Fingerprint, _validator);
        draft.StageDefinitions(changes);
        Require(draft.Project.Definitions.Count == 2, "Fork not atomic.");
        Require(XElement.Parse(changes[1].Xml).Descendants("Spawn").Any(x => (string? )x.Attribute("Id") == changes[0].ExportId), "Parent reference not rewired.");
        Require(source.Xml.Contains("Id=\"ConstructionSupplies\""), "Original package altered.");
        var bad = changes[0] with
        {
            Xml = changes[0].Xml.Replace("Value=\"30\"", "Value=\"-1\"")
        };
        Reject(() => draft.StageDefinition(bad));
        Require(draft.Project.Definitions[0].Xml == changes[0].Xml, "Failed edit changed draft.");
        Console.WriteLine("PASS: local crate fork rewires only selected parent and failed staging remains transactional.");
    }

    private void CheckCycleAndMissing()
    {
        var p = Project();
        p.Definitions.Add(Change("Spawn", "DefaultHuman", "Forge.Cycle", x => x.Add(new XElement("Spawn", new XAttribute("Id", "Forge.Cycle")))));
        Reject(() => _validator.Validate(p, true));
        p.Definitions.Clear();
        p.Definitions.Add(Change("StartCondition", "DefaultStart", "Forge.Missing", x => x.Elements("Spawn").First().SetAttributeValue("Id", "NoSuchSpawn")));
        Reject(() => _validator.Validate(p, true));
        Console.WriteLine("PASS: reachable cycles and missing spawn references rejected.");
    }

    private void CheckInvalidValues()
    {
        foreach (var value in new[]
        {
            "-1",
            "NaN",
            "Infinity",
            "true",
            "1.5"
        }

        )
        {
            var p = Project();
            p.Definitions.Add(Change("Spawn", "ConstructionSupplies", edit: x => x.Descendants("Quantity").First().SetAttributeValue("Value", value)));
            Reject(() => _validator.Validate(p, true));
        }

        var collision = Project();
        collision.Definitions.Add(Change("Spawn", "ConstructionSupplies", "DefaultLander"));
        Reject(() => _validator.Validate(collision, true));
        var weather = Project();
        weather.Definitions.Add(Change("WeatherEvent", "MarsDustStorm", edit: x => x.Element("Duration")!.Remove()));
        Reject(() => _validator.Validate(weather, true));
        var optionalBoolean = Project();
        optionalBoolean.Definitions.Add(Change("DifficultySetting", "Normal", edit: x =>
        {
            x.Element("Sanitation")?.Remove();
            x.Add(new XElement("Sanitation", new XAttribute("Value", "1")));
        }));
        Reject(() => _validator.Validate(optionalBoolean, true));
        Console.WriteLine("PASS: invalid quantities, missing weather fields, boolean types and new-definition ID collisions rejected.");
    }

    private void CheckWorldReplacement()
    {
        var p = Project();
        p.Definitions.Add(Change("World", "Mars2", edit: x => x.Element("Gravity")!.Value = "-2.5"));
        _validator.Validate(p, true);
        var x = XElement.Parse(System.Text.Encoding.UTF8.GetString(new NativeModWriter(_native).Write(p)));
        Require(x.Descendants("World").Count() == 1, "Unrelated worlds copied.");
        Require(x.Descendants("Gravity").Single().Value == "-2.5", "Signed world setting corrupted.");
        var invalid = p.Definitions[0] with
        {
            Xml = p.Definitions[0].Xml.Replace("-2.5", "NaN")
        };
        p.Definitions[0] = invalid;
        Reject(() => _validator.Validate(p, true));
        Console.WriteLine("PASS: exact world replacement preserves base resource references and validates signed numeric settings.");
    }

    private void CheckUnknownPolicy()
    {
        var d = _native.Definitions.First(x => !x.CanExport);
        var p = Project();
        p.Definitions.Add(new(d.Key, d.SourceHash, d.Id, d.Xml, false));
        Reject(() => _validator.Validate(p, true));
        Console.WriteLine("PASS: unverified native domains remain blocked for export.");
    }

    private void CheckProjectPersistence()
    {
        var p = Project();
        p.Definitions.Add(Change("Spawn", "ConstructionSupplies", "Forge.Supplies"));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new JsonProjectStore(new AtomicFileWriter());
            store.Save(path, p);
            var restored = store.Load(path);
            _validator.Validate(restored, true);
            Require(restored.Definitions.Single().Xml == p.Definitions.Single().Xml, "Native changes lost during reopen.");
        }
        finally
        {
            File.Delete(path);
        }

        Console.WriteLine("PASS: format-2 native definition changes save and reopen.");
    }
}
