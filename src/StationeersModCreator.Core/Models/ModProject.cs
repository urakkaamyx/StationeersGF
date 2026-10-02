namespace StationeersModCreator.Core.Models;
public sealed class ModProject
{
    public int FormatVersion { get; init; } = 1;
    public string CatalogFingerprint { get; set; } = "";
    public string Name { get; set; } = "My Stationeers Mod";
    public string Author { get; set; } = "Player";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "";
    public string PreviewAsset { get; set; } = "planets/StatMars.png";
    public List<RecipePatch> Recipes { get; set; } = [];
    public List<AttributePatch> Attributes { get; set; } = [];
}
