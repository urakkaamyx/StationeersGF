using System.Xml.Linq;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Core.Interfaces;
namespace StationeersModCreator.Core.Services;

public sealed class InventoryModCompiler : IInventoryModCompiler
{
    private readonly INativeCatalogRepository _catalog; private readonly IInventoryLayoutService _layout; private readonly IInventoryResolver _resolver;
    public InventoryModCompiler(INativeCatalogRepository catalog, IInventoryLayoutService layout, IInventoryResolver resolver) { _catalog = catalog; _layout = layout; _resolver = resolver; }
    public IReadOnlyList<NativeDefinitionChange> Compile(PendingInventoryDraft draft, ModProject project)
    {
        var source = _catalog.Find(draft.SourceKey); if (source.SourceHash != draft.SourceHash || source.Tag != "StartCondition") throw new InvalidDataException("Inventory source mismatch."); if (draft.ExportStartId == source.Id || !draft.ExportStartId.StartsWith("Forge.", StringComparison.Ordinal) || _catalog.Definitions.Any(x => x.Id == draft.ExportStartId)) throw new InvalidDataException("Use a new Forge-prefixed start ID to keep the source kit unchanged."); var check = _layout.Layout(draft.Xml, draft.Context.Species); if (check.Diagnostics.Count > 0) throw new InvalidDataException(string.Join("\n", check.Diagnostics)); var kitBase = _catalog.Resolve("Spawn", "DefaultNewPlayer"); var kitId = draft.ExportStartId + "." + draft.Context.Event + "." + draft.Context.Species + "." + draft.Context.Difficulty; var kit = XElement.Parse(draft.Xml); kit.SetAttributeValue("Id", kitId);
        var startChange = project.Definitions.FirstOrDefault(x => x.ExportId == draft.ExportStartId); var start = XElement.Parse(startChange?.Xml ?? source.Xml); start.SetAttributeValue("Id", draft.ExportStartId);
        var stem = draft.ExportStartId + "." + draft.Context.Event + ".Router"; var routerId = stem; var ordinal = 2; while (project.Definitions.Any(x => x.ExportId == routerId)) routerId = stem + "." + ordinal++;
        var bindings = start.Elements("Spawn").Where(x => (string?)x.Attribute("Event") == draft.Context.Event).ToList(); if (bindings.Count == 0) bindings.Add(new XElement("Spawn", new XAttribute("Id", draft.Context.Event == "NewPlayerKit" ? "DefaultNewPlayer" : "DefaultRespawnPlayer")));
        var router = new XElement("Spawn", new XAttribute("Id", routerId)); var changes = new List<NativeDefinitionChange> { new(kitBase.Key, kitBase.SourceHash, kitId, kit.ToString(), true) };
        foreach (var species in new[] { "Human", "Zrilian", "Robot" }) foreach (var diff in _catalog.Definitions.Where(x => x.Tag == "DifficultySetting").Select(x => x.Id).Distinct())
        {
            if (species == draft.Context.Species && diff == draft.Context.Difficulty) router.Add(Binding(kitId, species, diff));
            else foreach (var original in bindings) { var branch = new XElement("Spawn", new XElement(original)); branch.Add(new XElement("Species", new XAttribute("Id", species)), new XElement("Difficulty", new XAttribute("Id", diff), new XAttribute("Compare", "Equal"))); branch.Element("Spawn")!.Attribute("Event")?.Remove(); router.Add(branch); }
        }
        foreach (var binding in start.Elements("Spawn").Where(x => (string?)x.Attribute("Event") == draft.Context.Event).ToList()) binding.Remove(); start.Add(new XElement("Spawn", new XAttribute("Id", routerId), new XAttribute("Event", draft.Context.Event))); changes.Add(new(kitBase.Key, kitBase.SourceHash, routerId, router.ToString(), true)); changes.Add(new(source.Key, source.SourceHash, draft.ExportStartId, start.ToString(), true)); return changes;
    }
    private static XElement Binding(string id, string species, string difficulty) => new("Spawn", new XAttribute("Id", id), new XElement("Species", new XAttribute("Id", species)), new XElement("Difficulty", new XAttribute("Id", difficulty), new XAttribute("Compare", "Equal")));
}
