using System.Text;
using System.Xml.Linq;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class NativeGameDataWriter : IGameDataWriter
{
    private readonly ICatalogRepository _catalog;
    public NativeGameDataWriter(ICatalogRepository catalog) => _catalog = catalog;
    public Dictionary<string, byte[]> CreateFiles(ModProject project)
    {
        var files = CreateRecipes(project);
        if (project.Attributes.Count > 0)
            files.Add("GameData/prefabsettings.xml", Encode(CreateAttributes(project)));
        return files;
    }

    private Dictionary<string, byte[]> CreateRecipes(ModProject project)
    {
        var files = new Dictionary<string, byte[]>();
        foreach (var group in project.Recipes.GroupBy(x => _catalog.FindRecipe(x.RecipeId).SourceFile))
        {
            var root = new XElement("GameData");
            foreach (var section in group.GroupBy(x => _catalog.FindRecipe(x.RecipeId).Section))
                root.Add(new XElement(section.Key, section.Select(CreateRecipe)));
            files.Add("GameData/" + group.Key, Encode(root));
        }

        return files;
    }

    private XElement CreateRecipe(RecipePatch patch)
    {
        var definition = _catalog.FindRecipe(patch.RecipeId);
        var record = XElement.Parse(definition.OriginalXml, LoadOptions.PreserveWhitespace);
        foreach (var change in patch.Values)
        {
            var element = record.Element("Recipe")!;
            foreach (var part in change.Key.Split('/'))
                element = element.Elements(part).Single();
            element.Value = change.Value;
        }

        return record;
    }

    private XElement CreateAttributes(ModProject project)
    {
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        var mods = new XElement("ThingMods");
        foreach (var patch in project.Attributes)
        {
            var definition = _catalog.FindPrefab(patch.PrefabName);
            var type = patch.Values.Keys.Any(k => definition.Attributes.Single(x => x.Name == k).ModType == "StackableModData") ? "StackableModData" : "ThingModData";
            mods.Add(new XElement("ThingModData", new XAttribute(xsi + "type", type), new XElement("PrefabName", patch.PrefabName), patch.Values.Select(x => new XElement(x.Key, x.Value))));
        }

        return new XElement("GameData", new XAttribute(XNamespace.Xmlns + "xsi", xsi), mods);
    }

    private static byte[] Encode(XElement root) => Encoding.UTF8.GetBytes(new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString());
}
