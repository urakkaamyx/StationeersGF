using System.Globalization;
using System.Xml.Linq;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class NativeDefinitionValidator : INativeDefinitionValidator
{
    private readonly INativeCatalogRepository _catalog;
    private readonly ICatalogRepository _prefabs;
    public NativeDefinitionValidator(INativeCatalogRepository catalog, ICatalogRepository prefabs)
    {
        _catalog = catalog;
        _prefabs = prefabs;
    }

    public void ValidatePending(NativeDefinitionChange change)
    {
        var source = _catalog.Find(change.SourceKey);
        if (source.SourceHash != change.SourceHash)
            throw new InvalidDataException("Pending native source fingerprint mismatch.");
        try
        {
            if (XElement.Parse(change.Xml).Name.LocalName != source.Tag)
                throw new InvalidDataException("Pending definition type mismatch.");
        }
        catch (System.Xml.XmlException e)
        {
            throw new InvalidDataException("Invalid pending XML.", e);
        }
    }

    public void Validate(ModProject project)
    {
        var ids = new HashSet<string>();
        foreach (var change in project.Definitions)
        {
            var source = _catalog.Find(change.SourceKey);
            if (!source.CanExport)
                throw new InvalidDataException(source.Policy);
            if (source.SourceHash != change.SourceHash)
                throw new InvalidDataException("Native source fingerprint mismatch.");
            XElement element;
            try
            {
                element = XElement.Parse(change.Xml);
            }
            catch (Exception e)
            {
                throw new InvalidDataException("Invalid definition XML.", e);
            }

            if (element.Name.LocalName != source.Tag || element.Attribute("Id")?.Value != change.ExportId)
                throw new InvalidDataException("Definition identity changed unexpectedly.");
            if (!ids.Add(change.ExportId))
                throw new InvalidDataException("Duplicate native definition ID: " + change.ExportId);
            if (string.IsNullOrWhiteSpace(change.ExportId) || change.ExportId.Length > 100)
                throw new InvalidDataException("Enter a definition ID of 1–100 characters.");
            if (change.IsAddition && _catalog.Definitions.Any(x => x.Id == change.ExportId))
                throw new InvalidDataException("New ID already exists in the source catalog.");
            if (!change.IsAddition && change.ExportId != source.Id)
                throw new InvalidDataException("An override must retain the source ID.");
            new NativeFieldTypeValidator().Validate(source.Xml, element);
            ValidateScalarTypes(source, element);
            ValidateShape(element);
            ValidateNumbers(element);
            ValidatePrefabs(element);
        }

        ValidateReferences(project);
        ValidateResourceReferences(project);
    }

    private void ValidateScalarTypes(NativeDefinition source, XElement root)
    {
        foreach (var child in root.Elements().Where(x => !x.HasElements))
        {
            var samples = _catalog.Definitions.Where(x => x.Tag == source.Tag).SelectMany(x => XElement.Parse(x.Xml).Elements(child.Name)).Where(x => !x.HasElements).ToList();
            var value = (string? )child.Attribute("Value") ?? child.Value;
            var observed = samples.Select(x => (string? )x.Attribute("Value") ?? x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (observed.Count > 0 && observed.All(x => bool.TryParse(x, out _)) && !bool.TryParse(value, out _))
                throw new InvalidDataException(child.Name + " must be true or false.");
        }
    }

    private static void ValidateShape(XElement root)
    {
        if (root.Name == "World" && (root.Element("GlobalAtmosphere")is null || root.Element("PreviewScene")is null))
            throw new InvalidDataException("A world replacement must retain its atmosphere and preview scene.");
        if (root.Name == "WeatherEvent" && (new[]
        {
            "CoolDown",
            "StartDelay",
            "Duration",
            "SolarRatio",
            "WindStrength"
        }.Any(name => root.Element(name)is null) || (root.Element("TemperatureOffset")is null && root.Element("TemperatureOffsetCurve")is null)))
            throw new InvalidDataException("Weather requires cooldown, start delay, duration, temperature offset, solar ratio and wind strength.");
        if (root.Name == "OreVein" && (root.Attribute("Type")is null || root.Attribute("Type")!.Value == "None"))
            throw new InvalidDataException("Ore vein requires its type.");
        if (root.Name == "StartCondition" && !root.Elements("Spawn").Any())
            throw new InvalidDataException("A start condition requires at least one event binding.");
        if (root.Name == "StartCondition")
            foreach (var s in root.Elements("Spawn"))
                if (!new[]
                {
                    "NewWorld",
                    "NewPlayer",
                    "NewPlayerKit",
                    "RespawnPlayer",
                    "RespawnPlayerKit"
                }.Contains((string? )s.Attribute("Event")))
                    throw new InvalidDataException("Choose one of the five supported spawn events.");
        if (root.Name == "Spawn" && !root.Elements().Any(x => new[] { "Item", "DynamicThing", "Spawn", "Structure", "WorldAtmosphere", "PositionList" }.Contains(x.Name.LocalName)))
            throw new InvalidDataException("Spawn package has no contents.");
    }

    private static void ValidateNumbers(XElement root)
    {
        foreach (var node in root.DescendantsAndSelf())
            foreach (var attr in node.Attributes())
            {
                var key = attr.Name.LocalName;
                if (key is "Moles" or "Litres" or "Celsius" or "Seconds" or "SlotIndex" || (key == "Value" && node.Name.LocalName is "Quantity" or "Percent") || (key == "Value" && root.Name == "DifficultySetting" && node.Name.LocalName is not ("Name" or "Description")))
                {
                    if (bool.TryParse(attr.Value, out _) && key == "Value" && root.Name == "DifficultySetting")
                        continue;
                    if (!double.TryParse(attr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n))
                        throw new InvalidDataException("Enter a finite number for " + node.Name + " " + key);
                    if (key == "Celsius" && n < -273.15)
                        throw new InvalidDataException("Temperature cannot be below absolute zero.");
                    if (key != "Celsius" && n < 0)
                        throw new InvalidDataException("Negative " + key + " is invalid.");
                    if (key == "SlotIndex" && n != Math.Truncate(n))
                        throw new InvalidDataException("Slot index must be a whole number.");
                    if (node.Name == "Quantity" && (n < 1 || n != Math.Truncate(n)))
                        throw new InvalidDataException("Item quantity must be a positive whole number.");
                    if (node.Name == "Percent" && n > 100)
                        throw new InvalidDataException("Fill percentage cannot exceed 100.");
                }
            }
    }

    private void ValidatePrefabs(XElement root)
    {
        foreach (var item in root.Descendants().Where(x => x.Name == "Item" || x.Name == "DynamicThing"))
        {
            var id = (string? )item.Attribute("Id");
            if (id is null || !_prefabs.Catalog.Prefabs.Any(x => x.Name == id))
                throw new InvalidDataException("Unknown prefab: " + id);
            var quantity = (string? )item.Element("Quantity")?.Attribute("Value");
            var max = _prefabs.Catalog.Prefabs.First(x => x.Name == id).Attributes.FirstOrDefault(x => x.Name == "MaxQuantity");
            if (quantity is not null && max is not null && decimal.Parse(quantity, CultureInfo.InvariantCulture) > decimal.Parse(max.Value, CultureInfo.InvariantCulture))
                throw new InvalidDataException("Quantity exceeds the source stack maximum for " + id + ". Use another stack.");
        }
    }

    private void ValidateReferences(ModProject project)
    {
        var effective = _catalog.Definitions.Where(x => x.Tag is "Spawn" or "StartCondition").GroupBy(x => x.Id).ToDictionary(g => g.Key, g => XElement.Parse(g.First().Xml));
        foreach (var c in project.Definitions)
        {
            var e = XElement.Parse(c.Xml);
            if (e.Name == "Spawn" || e.Name == "StartCondition")
                effective[c.ExportId] = e;
        }

        foreach (var c in project.Definitions)
        {
            var e = XElement.Parse(c.Xml);
            if (e.Name == "Spawn" || e.Name == "StartCondition")
                Visit(c.ExportId, effective, []);
            foreach (var world in e.Descendants("World"))
            {
                if (!_catalog.Definitions.Any(x => x.Tag == "World" && x.Id == (string? )world.Attribute("Id")) && !XElement.Parse(_catalog.Find(c.SourceKey).Xml).Descendants("World").Any(x => (string? )x.Attribute("Id") == (string? )world.Attribute("Id")))
                    throw new InvalidDataException("Unknown exact world ID: " + world.Attribute("Id"));
            }

            foreach (var species in e.Descendants("Species"))
                if (!new[]
                {
                    "Human",
                    "Zrilian",
                    "Robot"
                }.Contains((string? )species.Attribute("Id")))
                    throw new InvalidDataException("Unknown species predicate.");
            foreach (var diff in e.Descendants("Difficulty"))
                if (!_catalog.Definitions.Any(x => x.Tag == "DifficultySetting" && x.Id == (string? )diff.Attribute("Id")))
                    throw new InvalidDataException("Unknown difficulty predicate.");
        }
    }

    private void ValidateResourceReferences(ModProject project)
    {
        var known = _catalog.Definitions.SelectMany(d => XElement.Parse(d.Xml).DescendantsAndSelf().Attributes("Path").Select(x => x.Value)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var change in project.Definitions)
            foreach (var path in XElement.Parse(change.Xml).DescendantsAndSelf().Attributes("Path"))
            {
                if (!known.Contains(path.Value))
                    throw new InvalidDataException("Unverified resource path: " + path.Value + ". Keep an existing game resource reference.");
            }
    }

    private static void Visit(string id, Dictionary<string, XElement> definitions, HashSet<string> path)
    {
        if (!definitions.TryGetValue(id, out var element))
            throw new InvalidDataException("Unresolved spawn reference: " + id);
        if (!path.Add(id))
            throw new InvalidDataException("Spawn reference cycle at " + id);
        foreach (var child in element.Descendants("Spawn"))
        {
            var target = (string? )child.Attribute("Id");
            if (target is not null && !child.Elements().Any(x => x.Name == "Item" || x.Name == "DynamicThing" || x.Name == "Spawn"))
                Visit(target, definitions, new HashSet<string>(path));
        }
    }
}
