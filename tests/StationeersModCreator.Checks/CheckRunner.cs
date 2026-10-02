using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Core.Services;

namespace StationeersModCreator.Checks;
public sealed class CheckRunner
{
    private readonly JsonCatalogRepository _catalog;
    private readonly ZipArtworkRepository _artwork;
    private readonly ProjectValidator _validator;
    private readonly string _temporary = Path.Combine(Path.GetTempPath(), "ModForgeChecks-" + Guid.NewGuid().ToString("N"));
    private int _passed;
    public CheckRunner(string content)
    {
        _catalog = new(Path.Combine(content, "catalog.json"));
        _artwork = new(Path.Combine(content, "artwork.zip"));
        _validator = new(_catalog, new NumericValueValidator());
    }

    public void Run()
    {
        Directory.CreateDirectory(_temporary);
        try
        {
            CheckRecipeExport();
            CheckSamePrefabAcrossMachines();
            CheckAttributeExport();
            CheckAtomicStaging();
            CheckInvalidInputs();
            CheckNestedConstraints();
            CheckProjectRoundTrip();
            CheckSettingsRoundTrip();
            CheckArtworkAndSafeArchivePaths();
            Console.WriteLine($"PASS: {_passed} end-to-end core checks.");
        }
        finally
        {
            Directory.Delete(_temporary, true);
        }
    }

    private ModProject Project() => new()
    {
        Name = "Forge & Test",
        Author = "QA <Pilot>",
        CatalogFingerprint = _catalog.Fingerprint
    };
    private RecipeDefinition Recipe(string section, string prefab) => _catalog.Catalog.Recipes.Single(x => x.Section == section && x.Prefab == prefab);
    private ModExporter Exporter() => new(_validator, new NativeGameDataWriter(_catalog), _artwork, new AtomicFileWriter());
    private void Pass(string name)
    {
        _passed++;
        Console.WriteLine("PASS: " + name);
    }

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

        throw new InvalidOperationException("Invalid input was accepted.");
    }

    private void CheckRecipeExport()
    {
        var r = Recipe("AutolatheRecipes", "ItemIronFrames");
        var original = r.OriginalXml;
        var p = Project();
        p.Recipes.Add(new(r.Id, r.SourceHash, new() { ["Iron"] = "5" }));
        var path = Path.Combine(_temporary, "recipe.zip");
        Exporter().Export(path, p);
        using var zip = ZipFile.OpenRead(path);
        var entry = zip.Entries.Single(x => x.FullName.EndsWith("GameData/autolathe.xml"));
        using var stream = entry.Open();
        var xml = XDocument.Load(stream);
        Require(xml.Descendants("Iron").Single().Value == "5", "Iron override missing.");
        Require(xml.Descendants("Energy").Single().Value == "200", "Unchanged energy was lost.");
        Require(xml.Descendants("RecipeData").Count() == 1, "Export contains unrelated recipes.");
        Require(r.OriginalXml == original, "Catalog source was changed.");
        using var metadata = zip.Entries.Single(x => x.FullName.EndsWith("About/About.xml")).Open();
        Require(XDocument.Load(metadata).Root!.Element("Author")!.Value == p.Author, "XML escaping failed.");
        Pass("native recipe ZIP, metadata escaping, and source preservation");
    }

    private void CheckSamePrefabAcrossMachines()
    {
        var p = Project();
        foreach (var section in new[]
        {
            "AutolatheRecipes",
            "TerraformingManufactoryRecipes"
        }

        )
        {
            var r = Recipe(section, "ItemIronFrames");
            p.Recipes.Add(new(r.Id, r.SourceHash, new() { ["Iron"] = section == "AutolatheRecipes" ? "5" : "6" }));
        }

        _validator.Validate(p, true);
        var files = new NativeGameDataWriter(_catalog).CreateFiles(p);
        Require(files.Count == 2, "Machine-specific variants were collapsed.");
        Pass("same item retains distinct machine recipes");
    }

    private void CheckAttributeExport()
    {
        var prefab = _catalog.FindPrefab("ItemIronOre");
        var p = Project();
        p.Attributes.Add(new(prefab.Name, prefab.SourceHash, new() { ["MaxQuantity"] = "1000", ["SurfaceAreaScale"] = "2" }));
        _validator.Validate(p, true);
        var xml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(new NativeGameDataWriter(_catalog).CreateFiles(p)["GameData/prefabsettings.xml"]));
        var mod = xml.Descendants("ThingModData").Single();
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        Require(mod.Attribute(xsi + "type")!.Value == "StackableModData", "Incorrect mod subtype.");
        Require(mod.Element("MaxQuantity")!.Value == "1000", "Attribute override missing.");
        Pass("native StackableModData with inherited attributes");
    }

    private void CheckAtomicStaging()
    {
        var draft = new DraftService(_catalog.Fingerprint, _validator);
        var r = Recipe("AutolatheRecipes", "ItemIronFrames");
        draft.StageRecipe(r, new() { ["Iron"] = "5" });
        Reject(() => draft.StageRecipe(r, new() { ["Iron"] = "-1" }));
        Require(draft.Project.Recipes.Single().Values["Iron"] == "5", "Failed stage replaced valid draft.");
        draft.StageRecipe(r, new() { ["Iron"] = "4" });
        Require(draft.ChangeCount == 0, "Restoring original value did not remove override.");
        Pass("invalid edits preserve the previous draft");
    }

    private void CheckInvalidInputs()
    {
        var r = Recipe("AutolatheRecipes", "ItemIronFrames");
        foreach (var value in new[]
        {
            "NaN",
            "Infinity",
            "-1",
            "1,5"
        }

        )
        {
            var p = Project();
            p.Recipes.Add(new(r.Id, r.SourceHash, new() { ["Iron"] = value }));
            Reject(() => _validator.Validate(p, true));
        }

        var wrong = Project();
        wrong.CatalogFingerprint = "wrong";
        Reject(() => _validator.Validate(wrong, false));
        var unknown = Project();
        unknown.Recipes.Add(new(r.Id, r.SourceHash, new() { ["UnknownField"] = "1" }));
        Reject(() => _validator.Validate(unknown, true));
        var duplicate = Project();
        var patch = new RecipePatch(r.Id, r.SourceHash, new() { ["Iron"] = "5" });
        duplicate.Recipes.AddRange([patch, patch]);
        Reject(() => _validator.Validate(duplicate, true));
        var qty = Project();
        var ore = _catalog.FindPrefab("ItemIronOre");
        qty.Attributes.Add(new(ore.Name, ore.SourceHash, new() { ["MaxQuantity"] = "1.5" }));
        Reject(() => _validator.Validate(qty, true));
        Pass("nonfinite/negative numbers, unknown fields, snapshots, duplicates and fractional stacks rejected");
    }

    private void CheckNestedConstraints()
    {
        var r = Recipe("FurnaceRecipes", "ItemSolidFuel");
        var p = Project();
        p.Recipes.Add(new(r.Id, r.SourceHash, new() { ["Temperature/Start"] = "1000" }));
        _validator.Validate(p, true);
        var xml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(new NativeGameDataWriter(_catalog).CreateFiles(p).Values.Single()));
        Require(xml.Descendants("RequiredMix").Single().Attribute("Rule")!.Value == "Pure", "Mixture constraint was lost.");
        Require(xml.Descendants("Volatiles").Single().Value == "1", "Nested ingredient was lost.");
        var invalid = Project();
        invalid.Recipes.Add(new(r.Id, r.SourceHash, new() { ["Temperature/Start"] = "100000" }));
        Reject(() => _validator.Validate(invalid, true));
        Pass("nested furnace constraints preserved; inverted ranges rejected");
    }

    private void CheckProjectRoundTrip()
    {
        var p = Project();
        var r = Recipe("AutolatheRecipes", "ItemIronFrames");
        p.Recipes.Add(new(r.Id, r.SourceHash, new() { ["Iron"] = "5" }));
        var store = new JsonProjectStore(new AtomicFileWriter());
        var path = Path.Combine(_temporary, "project.modforge.json");
        store.Save(path, p);
        var loaded = store.Load(path);
        _validator.Validate(loaded, true);
        Require(loaded.Recipes.Single().Values["Iron"] == "5" && loaded.Name == p.Name, "Project round trip failed.");
        Pass("editable project save/reopen");
    }

    private void CheckSettingsRoundTrip()
    {
        var store = new JsonSettingsStore(Path.Combine(_temporary, "settings.json"), new AtomicFileWriter());
        store.Save(new() { Accent = "Violet", FontScale = 1.1, HighContrast = true, CompactCards = true });
        var s = store.Load();
        Require(s.Accent == "Violet" && s.HighContrast && s.CompactCards && s.FontScale == 1.1, "Settings were not persisted.");
        Pass("appearance settings persist");
    }

    private void CheckArtworkAndSafeArchivePaths()
    {
        var p = Project();
        p.Name = "../Unsafe <Mod>";
        var r = Recipe("AutolatheRecipes", "ItemIronFrames");
        p.Recipes.Add(new(r.Id, r.SourceHash, new() { ["Iron"] = "5" }));
        var path = Path.Combine(_temporary, "art.zip");
        Exporter().Export(path, p);
        using var zip = ZipFile.OpenRead(path);
        Require(zip.Entries.All(x => !x.FullName.Contains("..") && !Path.IsPathRooted(x.FullName)), "Unsafe archive entry.");
        Require(zip.Entries.Any(x => x.FullName.EndsWith("About/Preview.png")), "Mod preview omitted.");
        Require(_artwork.Names.Count > 1500, "Game art catalog missing.");
        Pass("real game preview artwork and safe archive paths");
    }
}
