using System.Xml.Linq;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Core.Interfaces;
namespace StationeersModCreator.Core.Services;

public sealed class InventoryPredicateEvaluator
{
    private readonly string[] _difficulties;
    public InventoryPredicateEvaluator(INativeCatalogRepository catalog) { _difficulties = catalog.Definitions.Where(x => x.Tag == "DifficultySetting").Select(x => x.Id).Distinct().ToArray(); }
    public bool Applies(XElement node, InventoryContext context, bool characterParent) { foreach (var condition in node.Elements().Where(x => x.Name == "Species" || x.Name == "Difficulty")) { if (condition.HasElements) throw new InvalidDataException("Nested inventory predicates require additional evaluation support."); if (condition.Name == "Species" && (!characterParent || (string?)condition.Attribute("Id") != context.Species)) return false; if (condition.Name == "Difficulty") { var a = Array.IndexOf(_difficulties, context.Difficulty); var b = Array.IndexOf(_difficulties, (string?)condition.Attribute("Id")); if (a < 0 || b < 0) throw new InvalidDataException("Unknown difficulty predicate."); var valid = ((string?)condition.Attribute("Compare") ?? "EqualOrGreater") switch { "Less" => a < b, "EqualOrLess" => a <= b, "Equal" => a == b, "EqualOrGreater" or "Unassigned" => a >= b, "Greater" => a > b, _ => throw new InvalidDataException("Unknown difficulty comparison.") }; if (!valid) return false; } } return true; }
}
