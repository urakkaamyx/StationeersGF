namespace StationeersModCreator.Core.Models;
public sealed class EditorSettings
{
    public string Accent { get; set; } = "Cyan";
    public double FontScale { get; set; } = 1;
    public bool CompactCards { get; set; }
    public bool HighContrast { get; set; }
}
